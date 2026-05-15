using UnityEngine;
using Unity.Netcode;

public class InamicAmbuscada : InamiciAI
{
    private Transform spionTinta;
    private Transform cristalBaza;
    private bool esteLaCristal = false;

    public NetworkVariable<float> vitezaSincronizata = new NetworkVariable<float>(5f);
    public NetworkVariable<int> damageSincronizat = new NetworkVariable<int>(10);

    public override void OnNetworkSpawn()
    {
        GameObject objCristal = GameObject.Find("Cristal");

        if (objCristal != null)
        {
            cristalBaza = objCristal.transform;
        }

        if (!IsServer)
        {
            if (agent != null)
            {
                agent.enabled = false;
            }
        }

        Collider[] colidereInamic = GetComponentsInChildren<Collider>();
        UsaCasa[] usi = FindObjectsByType<UsaCasa>(FindObjectsSortMode.None);

        foreach (Collider colInamic in colidereInamic)
        {
            foreach (UsaCasa usa in usi)
            {
                Collider colUsa = usa.GetComponent<Collider>();
                if (colUsa != null && colInamic != null)
                {
                    Physics.IgnoreCollision(colInamic, colUsa, true);
                }
            }
        }
        
        isDead.OnValueChanged += OnDeathStateChanged;
        ToggleVisuals(!isDead.Value);
    }

    public void SeteazaSpion(Transform spion)
    {
        if (!IsServer)
        {
            return;
        }

        spionTinta = spion;
        tinta = spion;

        BasePlayer scriptSpion = spion.GetComponent<BasePlayer>();
        Health viataSpion = spion.GetComponent<Health>();

        if (scriptSpion != null)
        {
            vitezaSincronizata.Value = scriptSpion.speed.Value - 1f;
            damageSincronizat.Value = scriptSpion.ObtineDamageTotal();
        }

        if (viataSpion != null && health != null)
        {
            health.maxHealth.Value = viataSpion.maxHealth.Value;
            health.currentHealth.Value = viataSpion.maxHealth.Value;
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (!IsServer || isDead.Value)
        {
            return;
        }
        
        if (!esteLaCristal && cristalBaza != null)
        {
            float distantaPanaLaBaza = Vector3.Distance(transform.position, cristalBaza.position);
            if (distantaPanaLaBaza < razaAtac + 2f)
            {
                esteLaCristal = true;
                tinta = cristalBaza;
            }
        }
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.speed = vitezaSincronizata.Value;
        }
        this.damageAtac = damageSincronizat.Value;
    }
    
    protected override void GasesteJucator()
    {
        if (esteLaCristal)
        {
            tinta = cristalBaza;
            return;
        }
        
        if (spionTinta != null)
        {
            BasePlayer playerSpion = spionTinta.GetComponent<BasePlayer>();
            if (playerSpion != null && !playerSpion.isDead)
            {
                tinta = spionTinta;
                return; 
            }
        }
        
        base.GasesteJucator(); 
    }

    protected override void ComportamentFaraTinta()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && cristalBaza != null)
        {
            agent.isStopped = false;
            agent.SetDestination(cristalBaza.position);
        }
    }
    
    protected override void Ataca()
    {
        if (IsServer && tinta != null)
        {
            Health healthTinta = tinta.GetComponent<Health>();
            if (healthTinta != null)
            {
                healthTinta.TakeDamage(damageSincronizat.Value);
            }
        }
    }
}