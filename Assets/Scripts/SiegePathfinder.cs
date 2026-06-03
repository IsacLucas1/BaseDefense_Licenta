/*using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(InamiciAI))]
public class SiegePathfinder : NetworkBehaviour
{
    [Header("Setări Recalculare")]
    [Tooltip("La câte secunde își regândește ruta? (pt. performanță)")]
    public float intervalRecalculare = 0.5f;
    private float urmatoareaRecalculare = 0f;

    public bool AsediazaZidCurent { get; private set; } = false;
    public Transform ZidDeSpart { get; private set; } = null;

    private NavMeshAgent agent;
    private InamiciAI inamicAI;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        inamicAI = GetComponent<InamiciAI>();
    }

    public void CalculeazaSiNavigheaza(Vector3 destinatieFinala)
    {
        if (!IsServer)
        {
            return;
        } 

        if (Time.time >= urmatoareaRecalculare)
        {
            urmatoareaRecalculare = Time.time + intervalRecalculare;
            EvalueazaCeaMaiBunaRuta(destinatieFinala);
        }
    }

    private void EvalueazaCeaMaiBunaRuta(Vector3 destinatie)
    {
        AsediazaZidCurent = false;
        ZidDeSpart = null;

        agent.stoppingDistance = inamicAI.razaAtac * 0.5f;
        
        //Cea mai buna ruta ocolitoare (fara ziduri)
        NavMeshPath pathOcolire = new NavMeshPath();
        agent.CalculatePath(destinatie, pathOcolire);
        
        float timpOcolire = Mathf.Infinity;
        bool poateOcoli = false;
        
        //Acceptam si rute care se opresc langa cristal
        if (pathOcolire.status == NavMeshPathStatus.PathComplete)
        {
            poateOcoli = true;
        }
        else if (pathOcolire.status == NavMeshPathStatus.PathPartial && pathOcolire.corners.Length > 0)
        {
            Vector3 ultimulPunct = pathOcolire.corners[pathOcolire.corners.Length - 1];
            //Daca ultimul punct este indeajuns de aproape de cristal => ruta ocolitoare este curata
            if (Vector3.Distance(ultimulPunct, destinatie) <= inamicAI.razaAtac + 5f)
            {
                poateOcoli = true;
            }
        }
        
        if (poateOcoli)
        {
            float distantaOcolire = CalculLungimeRuta(pathOcolire);
            timpOcolire = distantaOcolire / agent.speed;
        }
        
        Vector3 origineRaycast = transform.position;
        
        // Dacă drumul e blocat complet (ex: inel interior închis), NavMesh-ul ne duce până la el.
        // Tragem linia direct din punctul de blocaj, NU din inamic! Asta evită lovirea zidurilor laterale pe drum.
        if (!poateOcoli && pathOcolire.corners.Length > 0)
        {
            origineRaycast = pathOcolire.corners[pathOcolire.corners.Length - 1];
        }

        //Cautam zid pe ruta directa catre cristal 
        Vector3 directieSpreBaza = (destinatie - origineRaycast).normalized;
        float distantaLiniara = Vector3.Distance(origineRaycast, destinatie);
        
        // Tragem o linie din inamic cpre cristal
        if (Physics.Raycast(origineRaycast + Vector3.up, directieSpreBaza, out RaycastHit hit, distantaLiniara))
        {
            Zid zid = hit.collider.GetComponent<Zid>();
            
            if (zid != null && zid.viata.Value > 0)
            {
                Collider zidCol = zid.GetComponent<Collider>();
                Vector3 punctSuprafataZid = zidCol.ClosestPoint(transform.position);
                
                //Timp spargere
                float distantaPanaLaZid = Vector3.Distance(transform.position, punctSuprafataZid);
                float distantaZidCristal = Vector3.Distance(punctSuprafataZid, destinatie);
                
                int numarLovituri = Mathf.CeilToInt((float)zid.viata.Value / inamicAI.damageAtac);
                float timpSpargere = numarLovituri * inamicAI.cooldownAtac;

                float timpPrinZid = (distantaPanaLaZid / agent.speed) + timpSpargere + (distantaZidCristal / agent.speed);

                //Decizia
                if (!poateOcoli || timpPrinZid < timpOcolire)
                {
                    AsediazaZidCurent = true;
                    ZidDeSpart = zid.transform;
                    
                    if (distantaPanaLaZid > inamicAI.razaAtac)
                    {
                        agent.SetDestination(punctSuprafataZid); 
                    }
                    else
                    {
                        agent.ResetPath(); 
                    }
                    return;
                }
            }
        }
        agent.SetDestination(destinatie);
    }
    
    private float CalculLungimeRuta(NavMeshPath path)
    {
        if (path.corners.Length < 2)
        {
            return 0f;
        }
        
        float lungime = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            lungime += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return lungime;
    }
}*/ 

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
        if (Time.time < asteaptaNavMeshPanaLa)
        {
            AsediazaZidCurent = false;
            ZidDeSpart = null;
            agent.SetDestination(destinatie);
            return;
        }

        if (AsediazaZidCurent && ZidDeSpart != null)
        {
            Zid zidCurent = ZidDeSpart.GetComponent<Zid>();
            if (zidCurent != null && zidCurent.viata.Value > 0)
            {
                NavigheazaLaZid(zidCurent);
                return;
            }

            AsediazaZidCurent = false;
            ZidDeSpart = null;
            asteaptaNavMeshPanaLa = Time.time + timpAsteptareDupaZidSpart;
            numarZiduriSparte++;
            agent.SetDestination(destinatie);
            return;
        }

        AsediazaZidCurent = false;
        ZidDeSpart = null;
        agent.stoppingDistance = inamicAI.razaAtac * 0.8f;

        NavMeshPath pathOcolire = new NavMeshPath();
        bool poateOcoli = agent.CalculatePath(destinatie, pathOcolire) && pathOcolire.status == NavMeshPathStatus.PathComplete;
        float costOcolire = poateOcoli ? CalculCostDeplasare(pathOcolire) : Mathf.Infinity;

        if (numarMaximZiduriSparte > 0 && numarZiduriSparte >= numarMaximZiduriSparte)
        {
            agent.SetDestination(destinatie);
            return;
        }

        Zid celMaiBunZid = null;
        Vector3 celMaiBunPunctAtac = Vector3.zero;
        float celMaiBunCostPrinZid = Mathf.Infinity;

        Zid[] ziduri = FindObjectsByType<Zid>(FindObjectsSortMode.None);
        foreach (Zid zid in ziduri)
        {
            if (zid == null || zid.viata.Value <= 0)
            {
                continue;
            }

            Collider zidCol = zid.GetComponent<Collider>();
            if (zidCol == null)
            {
                continue;
            }

            Vector3 punctSuprafataZid = zidCol.ClosestPoint(transform.position);
            if (!NavMesh.SamplePosition(punctSuprafataZid, out NavMeshHit navHitInainteZid, razaCautarePunctAtacZid, NavMesh.AllAreas))
            {
                continue;
            }

            NavMeshPath pathPanaLaZid = new NavMeshPath();
            if (!agent.CalculatePath(navHitInainteZid.position, pathPanaLaZid) || pathPanaLaZid.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            int damagePeLovitura = Mathf.Max(1, inamicAI.damageAtac);
            int numarLovituri = Mathf.CeilToInt((float)zid.viata.Value / damagePeLovitura);
            float timpSpargere = numarLovituri * inamicAI.cooldownAtac;
            float costDupaZid = CalculeazaCostDupaZid(punctSuprafataZid, destinatie, !poateOcoli);
            if (float.IsInfinity(costDupaZid))
            {
                continue;
            }

            float costPrinZid = CalculCostDeplasare(pathPanaLaZid) + timpSpargere + costDupaZid;

            if (costPrinZid < celMaiBunCostPrinZid)
            {
                celMaiBunCostPrinZid = costPrinZid;
                celMaiBunZid = zid;
                celMaiBunPunctAtac = navHitInainteZid.position;
            }
        }

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

        agent.SetDestination(destinatie);
    }

    private void NavigheazaLaZid(Zid zid)
    {
        Collider zidCol = zid.GetComponent<Collider>();
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
