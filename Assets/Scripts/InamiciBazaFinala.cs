using UnityEngine;
using Unity.Netcode;

public class InamiciBazaFinala : InamiciAI
{
    [Header("Setari Baza Finala")]
    public int viata = 150;
    public int damage = 20;
    
    public NetworkVariable<bool> aAparut = new NetworkVariable<bool>(false);
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"[InamicBaza] OnNetworkSpawn {name}, IsServer={IsServer}");
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (health != null)
            {
                health.maxHealth.Value = viata;
                health.currentHealth.Value = viata;
            }
            damageAtac = damage;
            SeteazaAdormit(true);
        }

        aAparut.OnValueChanged += OnAparitieChanged;
        ActualizeazaVizibilitate();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        aAparut.OnValueChanged -= OnAparitieChanged;
    }
    
    private void OnAparitieChanged(bool oldValue, bool newValue)
    {
        ActualizeazaVizibilitate();
    }
    
    protected override void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        ActualizeazaVizibilitate();
    }
    
    private void ActualizeazaVizibilitate()
    {
        bool vizibil = aAparut.Value && !isDead.Value;
        Debug.Log($"[InamicBaza] Vizibilitate {name}: aAparut={aAparut.Value}, isDead={isDead.Value} -> ToggleVisuals({vizibil})");
        ToggleVisuals(vizibil);
    }
    
    public void Aparitie()
    {
        Debug.Log($"[InamicBaza] Aparitie {name}");
        if (IsServer)
        {
            aAparut.Value = true;
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
        
        if (BazaInamicaManager.Instance != null)
        {
            BazaInamicaManager.Instance.VerificaVictorie();
        }
    }
    
    protected override void ComportamentFaraTinta()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }
}
