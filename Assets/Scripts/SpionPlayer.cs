using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpionPlayer : MeleePlayer
{
    [Header("Spion Specific")] public int multiplicatorDamageBackstab = 2;
    public float tolerantaUnghiBackstab = 0.6f;
    
    public ParticleSystem backstabParticles;

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
        else
        {
            enemyHit = true;
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
}
