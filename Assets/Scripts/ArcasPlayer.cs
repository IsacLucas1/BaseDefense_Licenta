using UnityEngine;
using Unity.Netcode;

public class ArcasPlayer: BasePlayer
{
    
    [Header("Setari de ATAC")]
    public GameObject sageataPrefab;
    public Transform spawnPoint;
    public float attackCooldown = 0.5f;
    
    private float nextAttackTime = 0f;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 7f;
        transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.cyan);
        }
        
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 80;
                health.currentHealth.Value = 80;
            }
        }
    }
    
    protected override void Update()
    {
        base.Update();
        if (!IsOwner)
        {
            return;
        }
        if (isDead)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            
            ShootServerRpc();
        }
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        GameObject newSageata = Instantiate(sageataPrefab, spawnPoint.position, spawnPoint.rotation);
        newSageata.GetComponent<NetworkObject>().Spawn();
        Sageata sageataScript = newSageata.GetComponent<Sageata>();
        if (sageataScript != null)
        {
            sageataScript.Initialize(OwnerClientId);
        }
    }
}
