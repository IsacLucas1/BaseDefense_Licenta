using UnityEngine;
using Unity.Netcode;

public class InamicAmbuscada : InamiciAI
{
    private Transform spionTinta;
    private Transform cristalBaza;
    private bool esteLaCristal = false;

    private SiegePathfinder siegePathfinder;

    public NetworkVariable<float> vitezaSincronizata = new NetworkVariable<float>(5f);
    public NetworkVariable<int> damageSincronizat = new NetworkVariable<int>(10);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        GameObject objCristal = GameObject.Find("Cristal");
        if (objCristal != null)
        {
            cristalBaza = objCristal.transform;
        }
        
        siegePathfinder = GetComponent<SiegePathfinder>();
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
        if (cristalBaza == null)
        {
            return;
        }
        
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
        
        if (siegePathfinder != null && siegePathfinder.AsediazaZidCurent && siegePathfinder.ZidDeSpart != null)
        {
            float distantaPanaLaZid = Vector3.Distance(transform.position, siegePathfinder.ZidDeSpart.position);
            
            if (distantaPanaLaZid <= razaAtac + 1.5f)
            {
                tinta = siegePathfinder.ZidDeSpart;
                return; 
            }
        }

        tauntTarget = null;
        tauntEndTime = 0f;
        
        base.GasesteJucator(); 
    }

    protected override void ComportamentFaraTinta()
    {
        if (cristalBaza == null)
        {
            return;
        }

        if (esteLaCristal)
        {
            agent.isStopped = true;
            Vector3 directie = new Vector3(cristalBaza.position.x, transform.position.y, cristalBaza.position.z);
            transform.LookAt(directie);
            
            if (Time.time >= nextAttackTime)
            {
                tinta = cristalBaza; 
                Ataca();            
                nextAttackTime = Time.time + cooldownAtac;
            }
            return;
        }

        siegePathfinder.CalculeazaSiNavigheaza(cristalBaza.position);
        
        if (siegePathfinder.AsediazaZidCurent && siegePathfinder.ZidDeSpart != null)
        {
            Transform zid = siegePathfinder.ZidDeSpart;
            float distantaPanaLaZid = Vector3.Distance(transform.position, zid.position);

            if (distantaPanaLaZid <= razaAtac + 1.5f)
            {
                agent.isStopped = true;
                Vector3 directieZid = new Vector3(zid.position.x, transform.position.y, zid.position.z);
                transform.LookAt(directieZid);
                
                if (Time.time >= nextAttackTime)
                {
                    tinta = zid; 
                    Ataca();     
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
}