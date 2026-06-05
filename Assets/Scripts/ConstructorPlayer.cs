using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;

public class ConstructorPlayer : MeleePlayer
{ 
    [Header("Setari Constructor")]
    public int multiplicatorLemn = 2;
    public NetworkVariable<int> costConstructie = new NetworkVariable<int>(10);
    
    [Header("Setari Constructie")]
    public float constructieCooldown = 0.8f;
    private float nextConstructieTime = 0f;
    
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            damageArma.Value = 10;
            atacCooldown.Value = 0.7f;
            durataAnimatie = 0.3f;
            
            speed.Value = 20f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 120;
                health.currentHealth.Value = 120;
            }

            lemn.Value = 4000;
        }
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(1f, 1f, 1f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.yellow);
        }
    }
    
    public override void AdaugaLemn(int cantitate)
    {
        if (IsServer)
        {
             int lemnAdaugat = cantitate * multiplicatorLemn;
             base.AdaugaLemn(lemnAdaugat);
        }
       
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!IsOwner || isDead.Value)
        {
            return;
        }
        if (UIManager.Instance != null  && (UIManager.Instance.jocPauza || UIManager.Instance.esteInMagazin))
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextConstructieTime)
        {
            AnuleazaRecall();
            IncearcaSaConstruiascaSauRepare();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            AnuleazaRecall();
            IncearcaSaDistrugaZid();
        }
    }
    
    private void IncearcaSaConstruiascaSauRepare()
    {
        Ray ray = new Ray(cameraCap.transform.position, cameraCap.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            Zid zid = hit.collider.GetComponent<Zid>();

            if (zid != null)
            {
                if (zid.viata.Value < zid.viataMax.Value)
                {
                    if(lemn.Value >= costConstructie.Value)
                    {
                        Debug.Log("Construiesc cu costul: " + costConstructie.Value);
                        ConstruiesteZidServerRpc(zid.NetworkObjectId);
                        nextConstructieTime = Time.time + constructieCooldown;
                    }
                    else
                    {
                        Debug.Log("Nu ai destul lemn! Ai " + lemn.Value + " dar iti trebuie " + costConstructie.Value);
                    }
                }
                else
                {
                    Debug.Log("Acest zid este deja la 100% viata!");
                }
            }
        }
    }

    [ServerRpc]
    private void ConstruiesteZidServerRpc(ulong zidId)
    {
        int cost = costConstructie.Value;
        if (lemn.Value < cost)
        {
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zidId, out var obj))
        {
            Zid zid = obj.GetComponent<Zid>();
            if (zid != null && zid.viata.Value < zid.viataMax.Value)
            {
                lemn.Value -= cost;
                zid.ConstruiesteSauRepara(20);
                Debug.Log("Zidul are: " + zid.viata.Value + " / " + zid.viataMax);
            }
        }
    }
    
    private void IncearcaSaDistrugaZid()
    {
        Ray ray = new Ray(cameraCap.transform.position, cameraCap.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            Zid zid = hit.collider.GetComponent<Zid>();

            if (zid != null && zid.viata.Value > 0)
            {
                DamageZidServerRpc(zid.NetworkObjectId);
            }
            else if(zid != null && zid.viata.Value <= 0)
            {
                Debug.Log("Acest zid este deja distrus!");
            }
            else
            {
                Debug.Log("Ma uit la " + hit.collider.name + " dar nu este un Zid.");
            }
        }
    }

    [ServerRpc]
    private void DamageZidServerRpc(ulong zidId)
    {
        int damageDistrugere = 10;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zidId, out var obj))
        {
            Zid zid = obj.GetComponent<Zid>();
            if (zid != null)
            {
                zid.PrimesteDamage(damageDistrugere);
            }
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        costConstructie.Value = 5; 
        Debug.Log("Constructorul a primit Upgrade-ul Suprem: Ziduri la jumatate de pret!");
    }
}
