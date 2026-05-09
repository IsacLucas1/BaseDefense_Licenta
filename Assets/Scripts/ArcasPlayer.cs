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
       
        /*
        transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.cyan);
        }*/
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
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            isBurstMode = !isBurstMode;
            Debug.Log("Arcașul a schimbat modul pe: " + (isBurstMode ? "BURST" : "NORMAL"));
        }

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
                else if (animator != null) 
                {
                    animator.SetTrigger("Attack");
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
    
    private IEnumerator BurstRoutine()
    {
        isShootingBurst = true;
        nextAttackTime = Time.time + attackCooldownBurst;
        if (netAnimator != null) 
        {
            netAnimator.SetTrigger("Attack");
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        yield return new WaitForSeconds(0.2f);
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
        Sageata sageataScript = newSageata.GetComponent<Sageata>();
        
        if (sageataScript != null)
        {
            sageataScript.Initialize(OwnerClientId);
        }
        
        newSageata.GetComponent<NetworkObject>().Spawn();
    }
}
