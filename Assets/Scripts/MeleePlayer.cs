using UnityEngine;
using Unity.Netcode;
using System.Collections;

public abstract class MeleePlayer : BasePlayer
{
    [Header("Setari Atac")] 
    public NetworkVariable<int> damageArma = new NetworkVariable<int>(10);
    public NetworkVariable<float> atacCooldown = new NetworkVariable<float>(0.7f);
    
    [Header("Setari Hitbox (Sfera de Lovire)")]
    [Tooltip("Cat de departe in fața jucatorului se aplică lovitura")]
    public float distantaLovitura = 1.5f;
    [Tooltip("Cat de mare/lată este raza zonei de lovire")]
    public float razaLovitura = 1.5f;
    
    protected float nextAttackTime = 0f;
    protected float nextAttackTimeServer = 0f;
    
    [Tooltip("Durata reala a clipului de atac, in secunde")]
    public float lungimeClipAtac = 2.1f;
    [Tooltip("Cat sa dureze animatia de atac pe ecran fata de cat dureaza cooldown-ul real (pune sub 1)")]
    public float durataAtacVizibila = 0.6f;
    
    protected bool isAttacking = false;
    protected bool canDealDamage = false;
    protected bool enemyHit = false;
    
    protected override void SetupLocalPlayer()
    {
        base.SetupLocalPlayer();
        
        damageArma.OnValueChanged += (oldVal, newVal) => 
        {
            if (IsOwner && UIManager.Instance != null)
            {
                UIManager.Instance.ActualizeazaDamage(ObtineDamageTotal());
            }
        };
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); 
        atacCooldown.OnValueChanged += (vechi, nou) => ActualizeazaVitezaAtac();
        isDead.OnValueChanged += (vechi, nou) =>
        {
            if (nou)
            {
                isAttacking = false;
                canDealDamage = false;
                enemyHit = false;
            }
        };
        ActualizeazaVitezaAtac();
    }

    protected void ActualizeazaVitezaAtac()
    {
        if (animator != null && atacCooldown.Value > 0f && durataAtacVizibila > 0f)
        {
            animator.SetFloat("AttackSpeed", lungimeClipAtac / atacCooldown.Value / durataAtacVizibila);
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
        
        if (canDealDamage && !enemyHit)
        {
            DetecteazaLovitura();
        }
        
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isAttacking)
        {
            AnuleazaRecall();
            IncearcaSaAtaci();
        }
    }
    
    public override int ObtineDamageTotal()
    {
        return damageArma.Value + extraDamage.Value;
    }
    
    //Desfasoara zona sferica (hitbox) in fata jucatorului pentru a gasi toti inamicii
    private void DetecteazaLovitura()
    {
        Vector3 centruLovitura = transform.position + transform.forward * distantaLovitura;
        Collider[] hitColliders = Physics.OverlapSphere(centruLovitura, razaLovitura);

        foreach (Collider hitCollider in hitColliders)
        {
            InamicLovit(hitCollider);
            // Daca un inamic a fost lovit, nu mai continua verificarea altor coliziuni
            if (enemyHit)
            {
                break;
            }
        }
    }
    
    public virtual void InamicLovit(Collider target)
    {
        if (!IsOwner || !canDealDamage || enemyHit)
        {
            return;
        }

        if (target.CompareTag("Enemy"))
        {
            // Trage un Linecast de la jucator catre centrul inamicului
            // Daca se intersecteaza cu un alt collider care nu este inamicul
            // sau jucatorul, atunci nu se aplica damage
            Vector3 startPos = transform.position + Vector3.up * 1f;
            Vector3 targetPos = target.bounds.center; 
            
            if (Physics.Linecast(startPos, targetPos, out RaycastHit hit, 
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != target && !hit.collider.CompareTag("Player"))
                {
                    return; 
                }
            }
            
            Health enemyHealth = target.GetComponent<Health>();
            
            // Aplica damage doar daca inamicul are componenta Health si este inca in viata
            if (enemyHealth != null && enemyHealth.currentHealth.Value > 0)
            {
                enemyHit = true; 
                NetworkObject netObj = target.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    DamageServerRpc(netObj.NetworkObjectId);
                }
            }
        }
    }
    
    protected void IncearcaSaAtaci()
    {
        isAttacking = true;
        nextAttackTime = Time.time + atacCooldown.Value;
        
        // Daca exista un animator de retea, declanseaza animatia de atac
        if (netAnimator != null)
        {
            netAnimator.ResetTrigger("Attack");
            netAnimator.SetTrigger("Attack");
            StartCoroutine(ResetAtac());
        }
    }

    private IEnumerator ResetAtac()
    {
        yield return new WaitForSeconds(atacCooldown.Value);
        isAttacking = false;
    }

    public void ExecutaLovituraDinAnimatie()
    {
        if (!IsOwner || isDead.Value)
        {
            return;
        }

        StartCoroutine(FereastraLovituraRoutine());
    }
    
    private IEnumerator FereastraLovituraRoutine()
    {
        canDealDamage = true;
        enemyHit = false;
        
        yield return new WaitForSeconds(0.25f);
        canDealDamage = false;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 centrulLoviturii = transform.position + Vector3.up * 1f + transform.forward * distantaLovitura;
        Gizmos.DrawWireSphere(centrulLoviturii, razaLovitura);
    }
}
