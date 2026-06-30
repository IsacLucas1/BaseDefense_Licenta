using UnityEngine;
using Unity.Netcode;

public class InamiciAsediu : InamiciAI
{
    [Header("Setari Asediu")] 
    public Transform checkpoint;
    public Transform tintaTitan;
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
        // Daca nu a ajuns la checkpoint, tinta este checkpoint-ul
        if (!aAjunsLaCheckpoint && checkpoint != null)
        {
            tinta = checkpoint;
        }
        // Daca a trecut de checkpoint, dar nu a trecut de poarta si poarta este inchisa, tinta este poarta
        else if (!aTrecutDePoarta && poarta != null && !poarta.EsteAccesibilFizic() && poarta.viata.Value > 0)
        {
            tinta = poarta.transform;
        }
        else
        {
            tinta = tintaTitan;
        }
    }

    protected override void UrmaresteSiAtacaJucator()
    {
        if (tinta == null)
        {
            return;
        }
        
        // Verificare daca a ajuns la checkpoint 
        if (!aAjunsLaCheckpoint && checkpoint != null)
        {
            float distantaLaPunct = Vector3.Distance(transform.position, checkpoint.position);
            if (distantaLaPunct <= razaAtac + 1f)
            {
                aAjunsLaCheckpoint = true;
            }
        }
        
        // Verificare daca a trecut de poarta
        if (!aTrecutDePoarta && poarta != null && tintaTitan != null)
        {
            float distantaInamicLatitan = Vector3.Distance(transform.position, tintaTitan.position);
            float distantaPoartaLatitan = Vector3.Distance(poarta.transform.position, tintaTitan.position);
            
            if (distantaInamicLatitan < distantaPoartaLatitan)
            {
                aTrecutDePoarta = true; 
            }
        }
        
        // Apeleaza functionalitatea de baza care muta NavMeshAgent-ul catre tinta
        base.UrmaresteSiAtacaJucator();
    }
    
    // Daca nu are tinta, opreste agentul
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
