using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.VisualScripting;

public class ConstructorPlayer : BasePlayer
{ 
    [Header("Setari Constructor")]
    public int multiplicatorLemn = 2;
    
    [Header("Setari Sabie")]
    public int damageSabie = 10;
    public float sabieCooldown = 0.7f;
    
    [Header("Setari Constructie")]
    public float constructieCooldown = 0.8f;
    
    private float nextConstructieTime = 0f;
    private float nextSabieTime = 0f;
    private float nextSabieTimeServer = 0f;
    
    [Header("Referinte Vizuale Sabie")]
    public Transform pivotSabie;
    
    private bool isAttacking = false;
    private bool canDealdamage = false;
    private bool enemyHit = false;
    
    public override void OnNetworkSpawn()
    {
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
        
        if(Input.GetMouseButton(0) && Time.time >= nextSabieTime && !isAttacking)
        {
            AnuleazaRecall();
            IncearcaSaAtaci();
        }
    }
    
    private void IncearcaSaAtaci()
    {
        nextSabieTime = Time.time + sabieCooldown;
        StartCoroutine(AnimatieAtacSabie());
        PerformAttackServerRpc();
    }

    [ServerRpc]
    private void PerformAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        if (Time.time < nextSabieTimeServer)
        {
            return;
        }
        nextSabieTimeServer = Time.time + sabieCooldown;
        PlayAttackAnimationClientRpc();
    }
    
    [ClientRpc]
    private void PlayAttackAnimationClientRpc()
    {
        if (IsOwner)
        {
            return;
        }
        StartCoroutine(AnimatieAtacSabie());
    }

    private IEnumerator AnimatieAtacSabie()
    {
        if (pivotSabie == null)
        {
            yield break;
        }
        
        isAttacking = true;
        canDealdamage = true;
        enemyHit = false;
        Quaternion rotatieInitiala = pivotSabie.localRotation;
        Quaternion rotatieAtac = rotatieInitiala * Quaternion.Euler(90f, 0f, 0f);
        
        float timpAnimatie = 0f;
        float durataAnimatie = 0.3f;

        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotSabie.localRotation = Quaternion.Lerp(rotatieInitiala, rotatieAtac, timpAnimatie / durataAnimatie);
            yield return null;
        }
        
        canDealdamage = false;
        yield return new WaitForSeconds(0.05f);
        
        timpAnimatie = 0f;
        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotSabie.localRotation = Quaternion.Lerp(rotatieAtac, rotatieInitiala, timpAnimatie / durataAnimatie);
            yield return null;
        }

        pivotSabie.localRotation = rotatieInitiala;
        isAttacking = false;
        enemyHit = false;
    }

    public void InamicLovit(Collider target)
    {
        if (!IsOwner || !canDealdamage || enemyHit) return;

        if (target.CompareTag("Enemy"))
        {
            Health enemyHealth = target.GetComponent<Health>();

            if (enemyHealth != null && enemyHealth.currentHealth.Value > 0)
            {
                enemyHit = true; 
                NetworkObject netObj = target.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    DamageServerRpc(netObj.NetworkObjectId, damageSabie + extraDamage.Value, OwnerClientId);
                }
            }
        }
        else
        {
            enemyHit = true;
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
                if (zid.viata.Value < zid.viataMax)
                {
                    int costConstructie = 10;
                                    
                    if(lemn.Value >= costConstructie)
                    {
                        Debug.Log("Am trimis comanda la server sa construiasca zidul!");
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
            else
            {
                Debug.Log("Mă uit la " + hit.collider.name + " dar nu este un Zid.");
            }
        }
        else
        {
            Debug.Log("Nu mă uit la niciun obiect sau sunt prea departe.");
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
                if (zid != null && zid.viata.Value < zid.viataMax)
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
}
