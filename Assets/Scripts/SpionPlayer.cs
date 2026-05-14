using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpionPlayer : MeleePlayer
{
    [Header("Setari Backstab")]
    public int multiplicatorDamageBackstab = 2;
    public float tolerantaUnghiBackstab = 0.6f;
    
    public ParticleSystem backstabParticles;

    [Header("Setari Invizibilitate")]
    public float durataInvizibilitate = 5f;
    public float cooldownInvizibilitate = 15f;
    private float nextInvizibilitateTime = 0f;
    
    public override void OnNetworkSpawn()
    {
        damageArma = 15;
        atacCooldown = 0.5f;
        durataAnimatie = 0.2f;

        if (IsServer)
        {
            speed.Value = 7f;

            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 70;
                health.currentHealth.Value = 70;
            }
        }

        base.OnNetworkSpawn();

        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
        }
    }
    
    protected override void SetupLocalPlayer()
    {
        base.SetupLocalPlayer();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SeteazaVizibilitateInvizibilitate(true);
        }
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
        
        if(UIManager.Instance != null)
        {
            float procentaj = 1f;

            if (Time.time < nextInvizibilitateTime)
            {
                float timpRamas = nextInvizibilitateTime- Time.time; 
                procentaj = 1f - (timpRamas / cooldownInvizibilitate);
            }
            
            UIManager.Instance.ActualizeazaCooldownInvizibilitate(procentaj);
        }
        
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextInvizibilitateTime)
        {
            AnuleazaRecall(); 
            ActiveazaInvizibilitateServerRpc();
            nextInvizibilitateTime = Time.time + cooldownInvizibilitate;
        }
    }

    [ServerRpc]
    private void ActiveazaInvizibilitateServerRpc()
    {
        StartCoroutine(RutinaInvizibilitate());
    }
    
    private IEnumerator RutinaInvizibilitate()
    {
        isInvisible.Value = true;
        UpdateVizualInvizibilitateClientRpc(true);
        
        yield return new WaitForSeconds(durataInvizibilitate);
        
        isInvisible.Value = false;
        UpdateVizualInvizibilitateClientRpc(false);
    }

    [ClientRpc]
    private void UpdateVizualInvizibilitateClientRpc(bool invizibil)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.GetComponent<ParticleSystem>() != null || r is LineRenderer) continue;
            
            if (r.material.HasProperty("_BaseColor"))
            {
                Color col = r.material.GetColor("_BaseColor");
                col.a = invizibil ? 0.4f : 1.0f; 
                r.material.SetColor("_BaseColor", col);
            }
        }
    }

    protected override IEnumerator AnimatieAtacArma()
    {
        if (pivotArma == null)
        {
            yield break;
        }

        isAttacking = true;
        canDealdamage = true;
        enemyHit = false;
        
        Vector3 pozitieInitiala = pivotArma.localPosition;
        Vector3 offsetAtac = pivotArma.localRotation * new Vector3(0, 1.5f, 0);
        Vector3 pozitieAtac = offsetAtac + pozitieInitiala;

        float timpAnimatie = 0f;
        
        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotArma.localPosition = Vector3.Lerp(pozitieInitiala, pozitieAtac, timpAnimatie / durataAnimatie);
            yield return null;
        }
        
        canDealdamage = false;
        yield return new WaitForSeconds(0.05f);
            
        timpAnimatie = 0f;
        while (timpAnimatie < durataAnimatie)
        {
            timpAnimatie += Time.deltaTime;
            pivotArma.localPosition = Vector3.Lerp(pozitieAtac, pozitieInitiala, timpAnimatie / durataAnimatie);
            yield return null;
        }
        
        pivotArma.localPosition = pozitieInitiala;
        isAttacking = false;
        enemyHit = false;
    }
    
    public override void InamicLovit(Collider target)
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
                    int damageFinal = damageArma;
                    
                    Vector3 directiePrivireSpion = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                    Vector3 directiePrivireInamic = new Vector3(target.transform.forward.x, 0, target.transform.forward.z).normalized;
                    
                    float unghi = Vector3.Dot(directiePrivireSpion, directiePrivireInamic);

                    if (unghi > tolerantaUnghiBackstab)
                    {
                        damageFinal = damageArma * multiplicatorDamageBackstab;
                        Debug.Log("Critical Hit!! " + damageFinal);
                        PlayBackstabEffectsServerRpc();
                    }
                    else
                    {
                        Debug.Log("Normal hit " + damageFinal);
                    }
                    
                    DamageServerRpc(netObj.NetworkObjectId, damageFinal + extraDamage.Value, OwnerClientId);
                }
            }
        }
    }

    [ServerRpc]
    private void PlayBackstabEffectsServerRpc()
    {
        PlayBackstabEffectsClientRpc();
    }

    [ClientRpc]
    private void PlayBackstabEffectsClientRpc()
    {
        if (backstabParticles != null)
        {
            backstabParticles.Play();
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        durataInvizibilitate += 3f; 
        cooldownInvizibilitate -= 2f; 
        multiplicatorDamageBackstab += 1; 
    }
}
