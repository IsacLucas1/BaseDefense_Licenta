using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(InamiciAI))]
public class SiegePathfinder : NetworkBehaviour
{
    [Header("Setari Recalculare")]
    public float intervalRecalculare = 0.5f;
    
    [Tooltip("Cat de departe cauta NavMesh langa suprafata zidului.")]
    public float razaCautarePunctAtacZid = 3f;

    [Tooltip("Cat asteapta dupa ce zidul ajunge la 0 viata, ca NavMeshObstacle carving sa actualizeze gaura.")]
    public float timpAsteptareDupaZidSpart = 1f;

    [Tooltip("Sparge zidul doar daca este cu atatea secunde mai ieftin decat ocolirea.")]
    public float avantajMinimSpargere = 0.75f;

    [Tooltip("Limita de siguranta. Seteaza 0 sau mai putin pentru nelimitat.")]
    public int numarMaximZiduriSparte = 0;

    public bool AsediazaZidCurent { get; private set; } = false;
    public Transform ZidDeSpart { get; private set; } = null;

    private float urmatoareaRecalculare = 0f;
    private float asteaptaNavMeshPanaLa = 0f;
    private int numarZiduriSparte = 0;
    private NavMeshAgent agent;
    private InamiciAI inamicAI;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        inamicAI = GetComponent<InamiciAI>();
    }

    public void CalculeazaSiNavigheaza(Vector3 destinatieFinala)
    {
        if (!IsServer) return; 

        if (Time.time >= urmatoareaRecalculare)
        {
            urmatoareaRecalculare = Time.time + intervalRecalculare;
            EvalueazaCeaMaiBunaRuta(destinatieFinala);
        }
    }

    private void EvalueazaCeaMaiBunaRuta(Vector3 destinatie)
    {
        // Daca inca asteapta sa se actualizeze NavMesh dupa ce a spart un zid, nu face nimic
        if (Time.time < asteaptaNavMeshPanaLa)
        {
            AsediazaZidCurent = false;
            ZidDeSpart = null;
            agent.SetDestination(destinatie);
            return;
        }

        // Daca este deja in mijlocul unui asediu (sparge un zid)
        if (AsediazaZidCurent && ZidDeSpart != null)
        {
            Zid zidCurent = ZidDeSpart.GetComponent<Zid>();
            // Daca zidul mai are viata, continua sa navigheze catre el
            if (zidCurent != null && zidCurent.viata.Value > 0)
            {
                NavigheazaLaZid(zidCurent);
                return;
            }

            // Daca zidul a fost spart, reseteaza starea si asteapta sa se actualizeze NavMesh
            AsediazaZidCurent = false;
            ZidDeSpart = null;
            asteaptaNavMeshPanaLa = Time.time + timpAsteptareDupaZidSpart;
            numarZiduriSparte++;
            agent.SetDestination(destinatie);
            return;
        }

        // Reseteaza starea ca fiind default, nu mai asediaza niciun zid
        AsediazaZidCurent = false;
        ZidDeSpart = null;
        agent.stoppingDistance = inamicAI.razaAtac * 0.8f;

        // Calculeaza costul de ocolire a zidurilor
        NavMeshPath pathOcolire = new NavMeshPath();
        bool poateOcoli = agent.CalculatePath(destinatie, pathOcolire) && pathOcolire.status == NavMeshPathStatus.PathComplete;
        float costOcolire = poateOcoli ? CalculCostDeplasare(pathOcolire) : Mathf.Infinity;

        // Daca a spart deja prea multe ziduri, nu mai sparge altele
        if (numarMaximZiduriSparte > 0 && numarZiduriSparte >= numarMaximZiduriSparte)
        {
            agent.SetDestination(destinatie);
            return;
        }

        Zid celMaiBunZid = null;
        Vector3 celMaiBunPunctAtac = Vector3.zero;
        float celMaiBunCostPrinZid = Mathf.Infinity;

        // Cauta toate zidurile din scena si evalueaza daca merita sa le sparga
        Zid[] ziduri = FindObjectsByType<Zid>(FindObjectsSortMode.None);
        foreach (Zid zid in ziduri)
        {
            if (zid == null || zid.viata.Value <= 0)
            {
                continue;
            }

            Collider zidCol = zid.GetComponentInChildren<Collider>();
            if (zidCol == null)
            {
                continue;
            }

            // Calculeaza punctul de pe suprafata zidului cel mai apropiat de inamic
            Vector3 punctSuprafataZid = zidCol.ClosestPoint(transform.position);
            // Verifica daca acel punct are sub el o pozitie valida pe NavMesh, pentru a putea naviga catre el
            if (!NavMesh.SamplePosition(punctSuprafataZid, out NavMeshHit navHitInainteZid, razaCautarePunctAtacZid, NavMesh.AllAreas))
            {
                continue;
            }

            NavMeshPath pathPanaLaZid = new NavMeshPath();
            if (!agent.CalculatePath(navHitInainteZid.position, pathPanaLaZid) || pathPanaLaZid.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            // Calculeaza timpul necesar pentru a sparge zidul, in functie de damage-ul per lovitura si viata zidului
            int damagePeLovitura = Mathf.Max(1, inamicAI.damageAtac);
            int numarLovituri = Mathf.CeilToInt((float)zid.viata.Value / damagePeLovitura);
            float timpSpargere = numarLovituri * inamicAI.cooldownAtac;
            // Calculeaza costul de a merge dupa zid catre destinatie
            float costDupaZid = CalculeazaCostDupaZid(punctSuprafataZid, destinatie, !poateOcoli);
            if (float.IsInfinity(costDupaZid))
            {
                continue;
            }

            // Calculeaza costul total de a merge pana la zid, a-l sparge si a merge dupa el
            float costPrinZid = CalculCostDeplasare(pathPanaLaZid) + timpSpargere + costDupaZid;

            // Daca acest cost este mai mic decat cel mai bun cost gasit pana acum, actualizeaza cel mai bun zid si punct de atac
            if (costPrinZid < celMaiBunCostPrinZid)
            {
                celMaiBunCostPrinZid = costPrinZid;
                celMaiBunZid = zid;
                celMaiBunPunctAtac = navHitInainteZid.position;
            }
        }

        // Daca cel mai bun zid gasit are un cost total mai mic decat costul de ocolire, incepe sa-l asedieze
        if (celMaiBunZid != null && celMaiBunCostPrinZid + avantajMinimSpargere < costOcolire)
        {
            AsediazaZidCurent = true;
            ZidDeSpart = celMaiBunZid.transform;

            if (Vector3.Distance(transform.position, celMaiBunPunctAtac) > inamicAI.razaAtac)
            {
                agent.SetDestination(celMaiBunPunctAtac);
            }
            else
            {
                agent.ResetPath();
            }
            return;
        }

        // Daca nu exista ziduri mai bune de spart, navigheaza direct catre destinatie
        agent.SetDestination(destinatie);
    }

    private void NavigheazaLaZid(Zid zid)
    {
        Collider zidCol = zid.GetComponentInChildren<Collider>();
        if (zidCol == null)
        {
            return;
        }

        Vector3 punctSuprafataZid = zidCol.ClosestPoint(transform.position);
        if (!NavMesh.SamplePosition(punctSuprafataZid, out NavMeshHit navHit, razaCautarePunctAtacZid, NavMesh.AllAreas))
        {
            return;
        }

        if (Vector3.Distance(transform.position, navHit.position) > inamicAI.razaAtac)
        {
            agent.SetDestination(navHit.position);
        }
        else
        {
            agent.ResetPath();
        }
    }

    private float CalculeazaCostDupaZid(Vector3 punctSuprafataZid, Vector3 destinatie, bool permiteEstimareCandNuExistaRuta)
    {
        Vector3 directieDupaZid = (destinatie - punctSuprafataZid).normalized;
        Vector3 punctDupaZid = punctSuprafataZid + directieDupaZid * (razaCautarePunctAtacZid + 0.5f);

        if (NavMesh.SamplePosition(punctDupaZid, out NavMeshHit navHitDupaZid, razaCautarePunctAtacZid, NavMesh.AllAreas))
        {
            NavMeshPath pathDupaZid = new NavMeshPath();
            if (NavMesh.CalculatePath(navHitDupaZid.position, destinatie, NavMesh.AllAreas, pathDupaZid) &&
                pathDupaZid.status == NavMeshPathStatus.PathComplete)
            {
                return CalculCostDeplasare(pathDupaZid);
            }
        }

        if (permiteEstimareCandNuExistaRuta)
        {
            return Vector3.Distance(punctSuprafataZid, destinatie) / Mathf.Max(0.1f, agent.speed);
        }

        return Mathf.Infinity;
    }

    private float CalculCostDeplasare(NavMeshPath path)
    {
        return CalculLungimeRuta(path) / Mathf.Max(0.1f, agent.speed);
    }
    
    private float CalculLungimeRuta(NavMeshPath path)
    {
        if (path.corners.Length < 2) return 0f;
        
        float lungime = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            lungime += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return lungime;
    }
}