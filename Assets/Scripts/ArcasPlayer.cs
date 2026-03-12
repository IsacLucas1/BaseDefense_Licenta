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
        if (IsServer)
        {
            speed.Value = 6f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 80;
                health.currentHealth.Value = 80;
            }
        }
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.cyan);
        }
        
        
    }
    
    protected override void Update()
    {
        base.Update();
        if (!IsOwner || isDead || isRecalling)
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            isBurstMode = !isBurstMode;
            Debug.Log("Arcașul a schimbat modul pe: " + (isBurstMode ? "BURST" : "NORMAL"));
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isShootingBurst)
        {
            if (isBurstMode)
            {
                StartCoroutine(BurstRoutine());
            }
            else
            {
                ShootServerRpc(cameraCap.rotation);
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;
        nextAttackTime = Time.time + attackCooldownBurst;
        for (int i = 0; i < 3; i++)
        {
            ShootServerRpc(cameraCap.rotation);
            yield return new WaitForSeconds(0.1f);
        }
        
        isShootingBurst = false;
    }
    [ServerRpc]
    private void ShootServerRpc(Quaternion rotatieTragere)
    {
        GameObject newSageata = Instantiate(sageataPrefab, spawnPoint.position, rotatieTragere);
        newSageata.GetComponent<NetworkObject>().Spawn();
        Sageata sageataScript = newSageata.GetComponent<Sageata>();
        if (sageataScript != null)
        {
            sageataScript.Initialize(OwnerClientId);
        }
    }
}
