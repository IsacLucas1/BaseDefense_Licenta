using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;

public class MedicPlayer : BasePlayer
{
    [Header("Arma Heal")]
    public NetworkVariable<float> healCooldown = new NetworkVariable<float>(1.0f);
    public NetworkVariable<int> healAmount = new NetworkVariable<int>(10);
    public NetworkVariable<float> healRange = new NetworkVariable<float>(10f);

    [Header("Arma Damage")]   
    public NetworkVariable<float> damageCooldown = new NetworkVariable<float>(1.0f);
    public NetworkVariable<int> damageAmount = new NetworkVariable<int>(10);
    public NetworkVariable<float> damageRange = new NetworkVariable<float>(10f);
    
    [Header("Efecte Vizuale")]
    public LineRenderer healBeam;
    public LineRenderer damageBeam;
    public Transform healBeamSpawnPoint;
    public Transform damageBeamSpawnPoint;
    
    private float nextHealTime = 0.0f;
    private float nextDamageTime = 0.0f;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            speed.Value = 5f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 100;
                health.currentHealth.Value = 100;
            }
        }
        
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(1f, 1f, 1f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
        }

        if (healBeam != null)
        {
            healBeam.enabled = false;
        }
        if (damageBeam != null)
        {
            damageBeam.enabled = false;
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
        
        if(Input.GetMouseButtonDown(1) && Time.time >= nextHealTime)
        {
            AnuleazaRecall();
            TryToHeal();
            nextHealTime = Time.time + healCooldown.Value;
        }
        
        if(Input.GetMouseButtonDown(0) && Time.time >= nextDamageTime)
        {
            AnuleazaRecall();
            TryToDamage();
            nextDamageTime = Time.time + damageCooldown.Value;
        }
        
    }
    
    public override int ObtineDamageTotal()
    {
        return damageAmount.Value + extraDamage.Value;
    }
    
    private void TryToHeal()
    {
        if(healBeamSpawnPoint == null || cameraCap == null)
        {
            return;
        }
        
        Vector3 startPoint = healBeamSpawnPoint.position; 
        Ray ray = new Ray(startPoint, cameraCap.forward);
        
        Vector3 endPoint = ray.origin + ray.direction * healRange.Value;
        if (Physics.Raycast(ray, out RaycastHit hit, healRange.Value))
        {
            endPoint = hit.point;
            
            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null && targetHealth.gameObject != this.gameObject)
            {
                ulong targetID = targetHealth.GetComponent<NetworkObject>().NetworkObjectId;
                HealServerRpc(targetID);
            }
        }
        
        AfiseazaLaserServerRpc(startPoint, endPoint);
    }

    [ServerRpc]
    private void HealServerRpc(ulong targetID)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetID, out NetworkObject targetObj))
        {
            Health targetHealth = targetObj.GetComponent<Health>();
            if (targetHealth != null && targetHealth.currentHealth.Value > 0)
            {
                targetHealth.Heal(healAmount.Value);
            }
        }
    }

    [ServerRpc]
    private void AfiseazaLaserServerRpc(Vector3 start, Vector3 end)
    {
        AfiseazaLaserClientRpc(start, end);
    }

    [ClientRpc]
    private void AfiseazaLaserClientRpc(Vector3 start, Vector3 end)
    {
        StartCoroutine(RazaVizualaCoroutine(healBeam, start, end));
    }
    
    private void TryToDamage()
    {
        if(damageBeamSpawnPoint == null || cameraCap == null)
        {
            return;
        }
        
        Vector3 startPoint = damageBeamSpawnPoint.position; 
        Ray ray = new Ray(startPoint, cameraCap.forward);
        
        Vector3 endPoint = ray.origin + ray.direction * damageRange.Value;
        if (Physics.Raycast(ray, out RaycastHit hit, damageRange.Value))
        {
            endPoint = hit.point;
            
            Health targetHealth = hit.collider.GetComponent<Health>();
            if (hit.collider.CompareTag("Enemy") && targetHealth != null && targetHealth.gameObject != this.gameObject)
            {
                NetworkObject netObj = targetHealth.GetComponent<NetworkObject>();
                
                if(netObj != null && netObj.IsSpawned)
                { 
                    ulong targetID = targetHealth.GetComponent<NetworkObject>().NetworkObjectId;
                    DamageServerRpc(targetID);
                }
            }
        }
        AfiseazaLaserDamageServerRpc(startPoint, endPoint);
    }
    
    [ServerRpc]
    private void AfiseazaLaserDamageServerRpc(Vector3 start, Vector3 end)
    {
        AfiseazaLaserDamageClientRpc(start, end);
    }
    
    [ClientRpc]
    private void AfiseazaLaserDamageClientRpc(Vector3 start, Vector3 end)
    {
        StartCoroutine(RazaVizualaCoroutine(damageBeam, start, end));
    }
    
    private IEnumerator RazaVizualaCoroutine(LineRenderer raza, Vector3 start, Vector3 end)
    {
        if (raza != null)
        {
            raza.enabled = true;
            raza.SetPosition(0, start);
            raza.SetPosition(1, end);
            
            yield return new WaitForSeconds(0.2f);
            raza.enabled = false;
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        healAmount.Value = 30; 
        healRange.Value = 20f;
        healCooldown.Value = 0.4f; 

        Debug.Log("Medicul a primit Upgrade-ul Suprem: Hyper-Heal activat!");
    }
}
