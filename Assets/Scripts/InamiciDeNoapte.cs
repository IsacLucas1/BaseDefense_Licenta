using Unity.Netcode;
using UnityEngine;

public class InamiciDeNoapte : InamiciAI
{
    [Header("Setari Noapte")]
    public Transform tintaBaza;
    
    private bool aAjunsLaBaza = false;
    
    protected override void Update()
    {
        if (!IsServer || isDead.Value) return;

        // Daca a ajuns deja la baza, IGNORA cautarea de jucatori si executa doar atacul pe baza
        if (aAjunsLaBaza)
        {
            ComportamentFaraTinta();
        }
        else
        {
            base.Update();
        }
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
    
    
    protected override void ComportamentFaraTinta()
    {
        if (tintaBaza == null)
        {
            return;
        }

        float distantaPanaLaBaza = Vector3.Distance(transform.position, tintaBaza.position);
        
        if (distantaPanaLaBaza <= razaAtac +2f)
        {
            aAjunsLaBaza = true; 
            tinta = null; 
            
            agent.isStopped = true;
            
            Vector3 directieprivire = new Vector3(tintaBaza.position.x, transform.position.y, tintaBaza.position.z);
            transform.LookAt(directieprivire);
            
            if (Time.time >= nextAttackTime)
            {
                Health healthBaza = tintaBaza.GetComponent<Health>();
                if (healthBaza != null)
                {
                    healthBaza.TakeDamage(damageAtac);
                    Debug.Log("Inamicul a lovit baza!");
                }
                
                nextAttackTime = Time.time + cooldownAtac;
            }
        }
        else
        {
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }

            if (Vector3.Distance(agent.destination, tintaBaza.position) > 1f)
            {
                agent.SetDestination(tintaBaza.position);
            }
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