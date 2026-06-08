using UnityEngine;
using Unity.Netcode;

public class InamiciAsediu : InamiciAI
{
    [Header("Setari Asediu")] 
    public Transform checkpoint;
    public Transform tintaCristal;
    public Poarta poarta;
    
    private bool aAjunsLaCheckpoint = false;
    private bool aTrecutDePoarta = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer && health != null)
        {
            health.maxHealth.Value = 200; 
            health.currentHealth.Value = 200;
            damageAtac = 30; 
        }
    }
    
    protected override void GasesteJucator()
    {
        if (!aAjunsLaCheckpoint && checkpoint != null)
        {
            tinta = checkpoint;
        }
        else if (!aTrecutDePoarta && poarta != null && !poarta.EsteAccesibilFizic() && poarta.viata.Value > 0)
        {
            tinta = poarta.transform;
        }
        else
        {
            tinta = tintaCristal;
        }
    }

    protected override void UrmaresteSiAtacaJucator()
    {
        if (tinta == null)
        {
            return;
        }
        
        if (!aAjunsLaCheckpoint && checkpoint != null)
        {
            float distantaLaPunct = Vector3.Distance(transform.position, checkpoint.position);
            if (distantaLaPunct <= razaAtac + 1f)
            {
                aAjunsLaCheckpoint = true;
            }
        }
        
        if (!aTrecutDePoarta && poarta != null && tintaCristal != null)
        {
            float distantaInamicLaCristal = Vector3.Distance(transform.position, tintaCristal.position);
            float distantaPoartaLaCristal = Vector3.Distance(poarta.transform.position, tintaCristal.position);
            
            if (distantaInamicLaCristal < distantaPoartaLaCristal)
            {
                aTrecutDePoarta = true; 
            }
        }
        
        base.UrmaresteSiAtacaJucator();
    }
    
    protected override void ComportamentFaraTinta()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
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
