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
    public NetworkVariable<int> sagetiPerBurst = new NetworkVariable<int>(3);
    
    public override void OnNetworkSpawn()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            animator.SetFloat("AnimSpeed", 1f);
        }
        
        if (IsServer)
        {
            speed.Value = 6f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 90;
                health.currentHealth.Value = 90;
            }
        }
        base.OnNetworkSpawn();
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
        
        // Schimbare mod de tragere intre normal si burst
        if (Input.GetKeyDown(KeyCode.T))
        {
            isBurstMode = !isBurstMode;
        }
        
        // Tragerea efectiva la click stanga, cu verificarea cooldown-ului si a modului de tragere
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isShootingBurst)
        {
            AnuleazaRecall();
            if (isBurstMode)
            {
                StartCoroutine(BurstRoutine());
            }
            else
            {
                if (netAnimator != null) 
                {
                    netAnimator.SetTrigger("Attack");
                }
                
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    public override int ObtineDamageTotal()
    {
        if (sageataPrefab != null)
        {
            Sageata sag = sageataPrefab.GetComponent<Sageata>();
            if (sag != null)
            {
                return sag.damage + extraDamage.Value;
            }
        }
        return extraDamage.Value;
    }
    
    public void ExecutaTragereDinAnimatie()
    {
        if (!IsOwner)
        {
            return;
        }
        if (isBurstMode) 
        {
            return;
        }
        ShootServerRpc(cameraCap.rotation);
    }
    
    [ServerRpc]
    private void ShootServerRpc(Quaternion rotatieTragere)
    {
        GameObject newSageata = Instantiate(sageataPrefab, spawnPoint.position, rotatieTragere);
        Sageata sageataScript = newSageata.GetComponent<Sageata>();
        
        if (sageataScript != null)
        {
            sageataScript.Initialize(OwnerClientId);
        }
        
        newSageata.GetComponent<NetworkObject>().Spawn();
    }
    
    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;
        nextAttackTime = Time.time + attackCooldownBurst;
        if (netAnimator != null) 
        {
            netAnimator.SetTrigger("Attack");
        }
        
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < sagetiPerBurst.Value; i++)
        {
            ShootServerRpc(cameraCap.rotation);
            yield return new WaitForSeconds(0.08f);
        }
        
        isShootingBurst = false;
    }
    
    protected override void AplicaUpgradeClasa()
    {
        sagetiPerBurst.Value = 4; 
        
        Debug.Log("Arcasul a primit Burst Suprem: 4 sageti!");
    }
}
