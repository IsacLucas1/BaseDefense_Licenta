using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ArcasPlayer: BasePlayer
{
    
    [Header("Setari de ATAC")]
    public GameObject sageataPrefab;
    public Transform spawnPoint;
    public float attackCooldown = 0.5f;
    public float attackCooldownBurst = 1.0f;
    
    private float nextAttackTime = 0f;
    private bool isBurstMode = false;
    private bool isShootingBurst = false;
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

        if (Input.GetKeyDown(KeyCode.T))
        {
            isBurstMode = !isBurstMode;
            Debug.Log("Arcașul a schimbat modul pe: " + (isBurstMode ? "BURST" : "NORMAL"));
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            {
                if (isBurstMode)
                {
                    StartCoroutine(BurstRoutine());
                }
                else
                {
                    ShootServerRpc();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
        }
    }

    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;
        nextAttackTime = Time.time + attackCooldownBurst;
        for (int i = 0; i < 3; i++)
        {
            ShootServerRpc();
            yield return new WaitForSeconds(0.1f);
        }
        
        isShootingBurst = false;
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
