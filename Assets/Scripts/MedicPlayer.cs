using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class MedicPlayer : BasePlayer
{
    [Header("Arma Heal")]
    public float healCooldown = 1.0f;
    int healAmount = 10;
    public float healRange = 10f;

    [Header("Efecte Vizuale")]
    public LineRenderer healBeam;
    public Transform beamSpawnPoint;
    private float nextHealTime = 0.0f;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 6f;
        transform.localScale = new Vector3(1f, 1f, 1f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
        }
        
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 100;
                health.currentHealth.Value = 100;
            }
        }

        if (healBeam != null)
        {
            healBeam.enabled = false;
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (!IsOwner || isDead || isRecalling)
        {
            return;
        }
        
        if(Input.GetMouseButtonDown(1) && Time.time >= nextHealTime)
        {
            TryToHeal();
            nextHealTime = Time.time + healCooldown;
        }
    }
    
    private void TryToHeal()
    {
        Vector3 startPoint = beamSpawnPoint.position; 
        Ray ray = new Ray(startPoint, cameraCap.forward);
        
        Vector3 endPoint = ray.origin + ray.direction * healRange;
        if (Physics.Raycast(ray, out RaycastHit hit, healRange))
        {
            endPoint = hit.point;
            
            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null && targetHealth.gameObject != this.gameObject)
            {
                ulong targetID = targetHealth.GetComponent<NetworkObject>().NetworkObjectId;
                HealServerRpc(targetID, healAmount);
            }
        }
        
        AfiseazaLaserServerRpc(startPoint, endPoint);
    }

    [ServerRpc]
    private void HealServerRpc(ulong targetID, int amount)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetID, out NetworkObject targetObj))
        {
            Health targetHealth = targetObj.GetComponent<Health>();
            if (targetHealth != null && targetHealth.currentHealth.Value > 0)
            {
                targetHealth.Heal(amount);
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
        StartCoroutine(RazaVizualaCoroutine(start, end));
    }
    
    private IEnumerator RazaVizualaCoroutine(Vector3 start, Vector3 end)
    {
        if (healBeam != null)
        {
            healBeam.enabled = true;
            healBeam.SetPosition(0, start);
            healBeam.SetPosition(1, end);
            
            yield return new WaitForSeconds(0.2f);
            healBeam.enabled = false;
        }
    }
}
