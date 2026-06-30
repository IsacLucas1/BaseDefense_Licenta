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
            atacCooldown.Value = 1f;
            
            speed.Value = 5f;
        
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
    
    // Metoda overide pentru a adauga lemn cu un multiplicator specific clasei Constructor
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
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            AnuleazaRecall();
            IncearcaActionarePoarta();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            AnuleazaRecall();
            IncearcaSaDistrugaZid();
        }
    }
    
    private void IncearcaSaConstruiascaSauRepare()
    {
        // Raza de la camera pentru a detecta zidul in fata jucatorului
        Ray ray = new Ray(cameraCap.transform.position, cameraCap.transform.forward);
        
        // Verifica daca raza intersecteaza un zid in raza de distantaAdunare
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            Zid zid = hit.collider.GetComponentInParent<Zid>();

            if (zid != null)
            {
                if (zid.viata.Value < zid.viataMax.Value)
                {
                    if(lemn.Value >= costConstructie.Value)
                    {
                        ConstruiesteZidServerRpc(zid.NetworkObjectId);
                        nextConstructieTime = Time.time + constructieCooldown;
                    }
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

        // Verifica daca zidul exista in SpawnedObjects si aplica construcția sau repararea fix pe acel zid
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zidId, out var obj))
        {
            Zid zid = obj.GetComponent<Zid>();
            if (zid != null && zid.viata.Value < zid.viataMax.Value)
            {
                lemn.Value -= cost;
                zid.ConstruiesteSauRepara(20);
            }
        }
    }
    
    private void IncearcaSaDistrugaZid()
    {
        Ray ray = new Ray(cameraCap.transform.position, cameraCap.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            Zid zid = hit.collider.GetComponentInParent<Zid>();

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
    
    // Metoda din ConstructorPlayer pentru a incerca actionarea unei porti
    private void IncearcaActionarePoarta()
    {
        Ray ray = new Ray(cameraCap.transform.position, cameraCap.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            ButonPoarta buton = hit.collider.GetComponent<ButonPoarta>();
            if (buton != null)
            {
                buton.IncearcaActionare(this);
            }
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        costConstructie.Value = 5; 
        Debug.Log("Constructorul a primit Upgrade-ul Suprem: Ziduri la jumatate de pret!");
    }
    
    protected override string ObtinePromptInteractiune(Collider col)
    {
        Zid zid = col.GetComponentInParent<Zid>();
        if (zid != null)
        {
            if (zid.viata.Value <= 0)
            {
                return "Press [F] pentru a construi zidul";
            }
            if (zid.viata.Value < zid.viataMax.Value)
            {
                return "Press [F] pentru a repara zidul";
            }
            if (zid.viata.Value >= zid.viataMax.Value)
            {
                return "Zidul are viata maxima!";
            }
        }

        ButonPoarta buton = col.GetComponent<ButonPoarta>();
        if (buton != null && buton.poarta != null && buton.poarta.viata.Value > 0)
        {
            return buton.poarta.isOpen.Value
                ? "Press [G] pentru a inchide poarta"
                : "Press [G] pentru a deschide poarta";
        }

        DepozitLemn depozit = col.GetComponent<DepozitLemn>();
        if (depozit != null)
        {
            if (depozit.lemnStocat.Value <= 0)
            {
                return "Nu ai ce colecta";
            }
            return "Press [Z] pentru a lua lemn din depozit";
        }

        return base.ObtinePromptInteractiune(col);
    }
}
