using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class InamiciDeNoapte : InamiciAI
{
    [Header("Setari Noapte")]
    public Transform tintaBaza;

    private SiegePathfinder siegePathfinder;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        siegePathfinder = GetComponent<SiegePathfinder>(); 
    }
    
    protected override bool VerificaLimitaUrmarire(Vector3 pozitieJucator)
    {
        if (tintaBaza == null)
        {
            return true;
        }

        float distantaPanaLaBaza = Vector3.Distance(transform.position, tintaBaza.position);
        float distantaPanaLaJucator = Vector3.Distance(transform.position, pozitieJucator);
        
        if (distantaPanaLaBaza < distantaPanaLaJucator)
        {
            return false;
        }
        
        return true;
    }

    protected override bool PoateUrmariJucator(BasePlayer player)
    {
        if (player == null || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return false;
        }

        NavMeshPath pathSpreJucator = new NavMeshPath();
        return agent.CalculatePath(player.transform.position, pathSpreJucator) && pathSpreJucator.status == NavMeshPathStatus.PathComplete;
    }
    
    protected override void GasesteJucator()
    {
        if (tintaBaza == null)
        {
            return;
        }
        Collider cristalCol = tintaBaza.GetComponent<Collider>();
        Vector3 punctSuprafataCristal = cristalCol != null ? cristalCol.ClosestPoint(transform.position) : tintaBaza.position;
        float distantaPanaLaBaza = Vector3.Distance(transform.position, punctSuprafataCristal);
        
        if (distantaPanaLaBaza <= razaAtac + 0.5f)
        {
            tinta = null;
            return; 
        }
        
        if (siegePathfinder != null && siegePathfinder.AsediazaZidCurent && siegePathfinder.ZidDeSpart != null)
        {
            Zid scriptZid = siegePathfinder.ZidDeSpart.GetComponent<Zid>();
            if (scriptZid != null && scriptZid.viata.Value > 0)
            {
                //Distanta pana la suprafata zidului
                Collider zidCol = siegePathfinder.ZidDeSpart.GetComponentInChildren<Collider>();
                Vector3 punctSuprafata = zidCol.ClosestPoint(transform.position);
                float distantaLaSuprafata = Vector3.Distance(transform.position, punctSuprafata);

                if (distantaLaSuprafata <= razaAtac + 0.5f) 
                {
                    if (Time.time < tauntEndTime && tauntTarget != null)
                    {
                        tinta = tauntTarget; 
                    }
                    else
                    {
                        tinta = null; 
                    }
                    return; 
                }
            }
        }
        
        base.GasesteJucator();
    }
    
    protected override void ComportamentFaraTinta()
    {
        if (tintaBaza == null)
        {
            return;
        }

        Collider cristalCol = tintaBaza.GetComponent<Collider>();
        Vector3 punctSuprafataCristal = cristalCol != null ? cristalCol.ClosestPoint(transform.position) : tintaBaza.position;
        float distantaPanaLaBaza = Vector3.Distance(transform.position, punctSuprafataCristal);
        
        if (distantaPanaLaBaza <= razaAtac + 0.5f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            
            Vector3 directieprivire = new Vector3(tintaBaza.position.x, transform.position.y, tintaBaza.position.z);
            transform.LookAt(directieprivire);
            
            if (Time.time >= nextAttackTime)
            {
                tinta = tintaBaza;
                Ataca();
                tinta = null;
                nextAttackTime = Time.time + cooldownAtac;
            }

            return;
        }
        
        if (siegePathfinder == null)
        {
            agent.isStopped = false;
            agent.SetDestination(tintaBaza.position);
            return;
        }

        // Gaseste cel mai apropiat punct VALID pe NavMesh langa cristal
        Vector3 destinatie = tintaBaza.position;
        if (NavMesh.SamplePosition(tintaBaza.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            destinatie = navHit.position;
        }
        siegePathfinder.CalculeazaSiNavigheaza(destinatie);

        if (siegePathfinder.AsediazaZidCurent && siegePathfinder.ZidDeSpart != null)
        {
            Zid scriptZid = siegePathfinder.ZidDeSpart.GetComponent<Zid>();
            if (scriptZid != null && scriptZid.viata.Value > 0)
            {
                Collider zidCol = siegePathfinder.ZidDeSpart.GetComponentInChildren<Collider>();
                Vector3 punctSuprafata = zidCol.ClosestPoint(transform.position);
                float distantaPanaLaZid = Vector3.Distance(transform.position, punctSuprafata);

                if (distantaPanaLaZid <= razaAtac + 0.5f)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;

                    Vector3 directieZid = new Vector3(siegePathfinder.ZidDeSpart.position.x, transform.position.y, siegePathfinder.ZidDeSpart.position.z);
                    transform.LookAt(directieZid);

                    if (Time.time >= nextAttackTime)
                    {
                        tinta = siegePathfinder.ZidDeSpart;
                        Ataca();
                        tinta = null;
                        nextAttackTime = Time.time + cooldownAtac;
                    }
                }
                else
                {
                    agent.isStopped = false;
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }
        else
        {
            agent.isStopped = false;
        }
    }

    public override void Moarte(BasePlayer killer)
    {
        base.Moarte(killer); 
        CampReward loot = GetComponent<CampReward>();
        if (loot != null)
        {
            loot.OferaRecompensa(killer);
        }
        if (GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
