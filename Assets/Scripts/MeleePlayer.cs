using UnityEngine;
using Unity.Netcode;
using System.Collections;

public abstract class MeleePlayer : BasePlayer
{
    [Header("Setari Atac")] 
    public int damageArma = 10;
    public float atacCooldown = 0.7f;
    public float durataAnimatie = 0.3f;
    
    [Header("Setari Hitbox (Sfera de Lovire)")]
    [Tooltip("Cat de departe in fața jucatorului se aplică lovitura")]
    public float distantaLovitura = 1.5f;
    [Tooltip("Cat de mare/lată este raza zonei de lovire")]
    public float razaLovitura = 1.5f;
    
    protected float nextAttackTime = 0f;
    protected float nextAttackTimeServer = 0f;

    [Header("Referinte Vizuale")] 
    public Transform pivotArma;
    
    protected bool isAttacking = false;
    protected bool canDealdamage = false;
    protected bool enemyHit = false;
    
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
        
        if (canDealdamage && !enemyHit)
        {
            DetecteazaLovitura();
        }
        
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            AnuleazaRecall();
            IncearcaSaAtaci();
        }
    }
    
    public override int ObtineDamageTotal()
    {
        return damageArma + extraDamage.Value;
    }
    
    private void DetecteazaLovitura()
    {
        Vector3 centruLovitura = transform.position + transform.forward * distantaLovitura;
        Collider[] hitColliders = Physics.OverlapSphere(centruLovitura, razaLovitura);

        foreach (Collider hitCollider in hitColliders)
        {
            InamicLovit(hitCollider);
            if (enemyHit)
            {
                break;
            }
        }
    }
    
    protected void IncearcaSaAtaci()
    {
        nextAttackTime = Time.time + atacCooldown;
        if (netAnimator != null)
        {
            netAnimator.SetTrigger("Attack");
        }
        else
        {
            StartCoroutine(AnimatieAtacArma());
        }
        PerformAttackServerRpc();
    }

    [ServerRpc]
    protected void PerformAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        if (Time.time < nextAttackTimeServer)
        {
            return;
        }
        nextAttackTimeServer = Time.time + atacCooldown;
        PlayAttackAnimationClientRpc();
    }

    [ClientRpc]
    protected void PlayAttackAnimationClientRpc()
    {
        if(!IsOwner)
        {
            if (netAnimator != null)
            {
                netAnimator.SetTrigger("Attack");
            }
            else
            {
                StartCoroutine(AnimatieAtacArma());
            }
        }
    }

    public void ExecutaLovituraDinAnimatie()
    {
        if (!IsOwner || isDead)
        {
            return;
        }
        
        canDealdamage = true;
        enemyHit = false;
        
        DetecteazaLovitura();
        
        canDealdamage = false;
    }
    
    protected virtual IEnumerator AnimatieAtacArma()
    {
        if (pivotArma == null)
        {
            yield break;
        }

        isAttacking = true;
        canDealdamage = false;
        enemyHit = false;

        Quaternion rotatieInitiala = pivotArma.localRotation;
        Quaternion rotatieAtac = rotatieInitiala * Quaternion.Euler(90f, 0f, 0f);

        float timpAnimatie = 0f;

        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotArma.localRotation = Quaternion.Lerp(rotatieInitiala, rotatieAtac, timpAnimatie / durataAnimatie);
            
            if (timpAnimatie >= durataAnimatie / 2f) 
            {
                canDealdamage = true;
            }
            yield return null;
        }

        canDealdamage = false;
        yield return new WaitForSeconds(0.1f);

        timpAnimatie = 0f;
        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotArma.localRotation = Quaternion.Lerp(rotatieAtac, rotatieInitiala, timpAnimatie / durataAnimatie);
            yield return null;
        }
        
        pivotArma.localRotation = rotatieInitiala;
        isAttacking = false;
        enemyHit = false;
    }
    
    public virtual void InamicLovit(Collider target)
    {
        if (!IsOwner || !canDealdamage || enemyHit)
        {
            return;
        }

        if (target.CompareTag("Enemy"))
        {
            Health enemyHealth = target.GetComponent<Health>();

            if (enemyHealth != null && enemyHealth.currentHealth.Value > 0)
            {
                enemyHit = true; 
                NetworkObject netObj = target.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    DamageServerRpc(netObj.NetworkObjectId, damageArma + extraDamage.Value, OwnerClientId);
                }
            }
        }
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 centrulLoviturii = transform.position + Vector3.up * 1f + transform.forward * distantaLovitura;
        Gizmos.DrawWireSphere(centrulLoviturii, razaLovitura);
    }
}
