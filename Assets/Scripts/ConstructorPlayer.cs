using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;

public class ConstructorPlayer : MeleePlayer
{ 
    [Header("Setari Constructor")]
    public int multiplicatorLemn = 2;
    public int costConstructie = 10;
    
    [Header("Setari Constructie")]
    public float constructieCooldown = 0.8f;
    private float nextConstructieTime = 0f;
    
    
    public override void OnNetworkSpawn()
    {
        damageArma = 10;
        atacCooldown = 0.7f;
        durataAnimatie = 0.3f;
        if (IsServer)
        {
            speed.Value = 5f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 120;
                health.currentHealth.Value = 120;
            }
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
        
        if (!IsOwner || isDead)
        {
            return;
        }
        if (UIManager.Instance != null && UIManager.Instance.jocPauza)
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
                    if(lemn.Value >= costConstructie)
                    {
                        Debug.Log("Construiesc cu costul: " + costConstructie);
                        ConstruiesteZidServerRpc(zid.NetworkObjectId, costConstructie);
                        nextConstructieTime = Time.time + constructieCooldown;
                    }
                    else
                    {
                        Debug.Log("Nu ai destul lemn! Ai " + lemn.Value + " dar iti trebuie " + costConstructie);
                    }
                }
                else
                {
                    Debug.Log("Acest zid este deja la 100% viață!");
                }
            }
        }
    }

    [ServerRpc]
    private void ConstruiesteZidServerRpc(ulong zidId, int cost)
    {
        if (lemn.Value >= cost)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zidId, out var obj))
            {
                Zid zid = obj.GetComponent<Zid>();
                if (zid != null && zid.viata.Value < zid.viataMax.Value)
                {
                    lemn.Value -= cost;
                    zid.ConstruiesteSauReparaServerRpc(20);
                    Debug.Log("Zidul are: " + zid.viata.Value + " / " + zid.viataMax);
                }
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
                int damageDistrugere = 12;
                Debug.Log($"Am lovit zidul! I-am dat {damageDistrugere} damage.");
                DamageZidServerRpc(zid.NetworkObjectId, damageDistrugere);
            }
            else if(zid != null && zid.viata.Value <= 0)
            {
                Debug.Log("Acest zid este deja distrus!");
            }
            else
            {
                Debug.Log("Mă uit la " + hit.collider.name + " dar nu este un Zid.");
            }
        }
    }

    [ServerRpc]
    private void DamageZidServerRpc(ulong zidId, int damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zidId, out var obj))
        {
            Zid zid = obj.GetComponent<Zid>();
            if (zid != null)
            {
                zid.PrimesteDamage(damage);
            }
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        costConstructie = 5; // Înjumătățim costul zidurilor
        Debug.Log("Constructorul a primit Upgrade-ul Suprem: Ziduri la jumătate de preț!");
    }
}
