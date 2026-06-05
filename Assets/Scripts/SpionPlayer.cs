using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpionPlayer : MeleePlayer
{
    [Header("Setari Backstab")]
    public NetworkVariable<int> multiplicatorDamageBackstab = new NetworkVariable<int>(2);
    public float tolerantaUnghiBackstab = 0.6f;
    
    public ParticleSystem backstabParticles;

    [Header("Setari Invizibilitate")]
    public NetworkVariable<float> durataInvizibilitate = new NetworkVariable<float>(5f);
    public NetworkVariable<float> cooldownInvizibilitate = new NetworkVariable<float>(15f);
    private float nextInvizibilitateTime = 0f;
    private Coroutine corutinaInvizibilitateActiva;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            damageArma.Value = 15;
            atacCooldown.Value = 0.5f;
            
            speed.Value = 7f;

            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 370;
                health.currentHealth.Value = 370;
            }
        }

        Collider[] colidereSpion = GetComponentsInChildren<Collider>();
        UsaCasa[] usi = FindObjectsByType<UsaCasa>(FindObjectsSortMode.None);

        foreach (Collider coliderSpion in colidereSpion)
        {
            foreach (UsaCasa usa in usi)
            {
                Collider coliderUsa = usa.GetComponent<Collider>();
                if (coliderUsa != null)
                {
                    Physics.IgnoreCollision(coliderSpion, coliderUsa, true);
                }
            }
        }
        durataAnimatie = 0.2f;
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
        
        if (!IsOwner || isDead.Value)
        {
            return;
        }
        if (UIManager.Instance != null  && (UIManager.Instance.jocPauza || UIManager.Instance.esteInMagazin))
        {
            return;
        }
        
        if(UIManager.Instance != null)
        {
            float procentaj = 1f;

            if (Time.time < nextInvizibilitateTime)
            {
                float timpRamas = nextInvizibilitateTime- Time.time; 
                procentaj = 1f - (timpRamas / cooldownInvizibilitate.Value);
            }
            
            UIManager.Instance.ActualizeazaCooldownInvizibilitate(procentaj);
        }
        
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextInvizibilitateTime)
        {
            AnuleazaRecall(); 
            ActiveazaInvizibilitateServerRpc();
            nextInvizibilitateTime = Time.time + cooldownInvizibilitate.Value;
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            AnuleazaRecall();
            IncearcaInteractiuneManeta();
        }
    }

    [ServerRpc]
    private void ActiveazaInvizibilitateServerRpc()
    {
        if (Time.time < nextInvizibilitateTime)
        {
            return;
        }

        nextInvizibilitateTime = Time.time + cooldownInvizibilitate.Value;

        if (corutinaInvizibilitateActiva != null)
        {
            StopCoroutine(corutinaInvizibilitateActiva);
        }

        corutinaInvizibilitateActiva = StartCoroutine(RutinaInvizibilitate());
    }
    
    private IEnumerator RutinaInvizibilitate()
    {
        isInvisible.Value = true;
        UpdateVizualInvizibilitateClientRpc(true);
        
        yield return new WaitForSeconds(durataInvizibilitate.Value);
        
        isInvisible.Value = false;
        UpdateVizualInvizibilitateClientRpc(false);
        
        corutinaInvizibilitateActiva = null;
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

        if (!target.CompareTag("Enemy"))
        {
            return;
        }

        Health enemyHealth = target.GetComponent<Health>();
        if (enemyHealth == null || enemyHealth.currentHealth.Value <= 0)
        {
            return;
        }

        NetworkObject netObj = target.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
        {
            return;
        }

        enemyHit = true;
        DamageServerRpc(netObj.NetworkObjectId);
    }
    
    protected override int CalculeazaDamageServer(NetworkObject targetObj)
    {
        int damageFinal = damageArma.Value;

        Vector3 directiePrivireSpion = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 directiePrivireInamic = new Vector3(targetObj.transform.forward.x, 0, targetObj.transform.forward.z).normalized;

        float unghi = Vector3.Dot(directiePrivireSpion, directiePrivireInamic);

        if (unghi > tolerantaUnghiBackstab)
        {
            damageFinal = damageArma.Value * multiplicatorDamageBackstab.Value;
            PlayBackstabEffectsClientRpc();
        }

        return damageFinal + extraDamage.Value;
    }
    
    [ClientRpc]
    private void PlayBackstabEffectsClientRpc()
    {
        if (backstabParticles != null)
        {
            backstabParticles.Play();
        }
    }
    
    private void IncearcaInteractiuneManeta()
    {
        if (cameraCap == null)
        {
            return;
        }
        
        Ray ray = new Ray(cameraCap.position, cameraCap.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            LeverCasa maneta = hit.collider.GetComponent<LeverCasa>();
            if (maneta != null)
            {
                maneta.IncearcaTragere();
            }
        }
    }
    
    protected override void AplicaUpgradeClasa()
    {
        durataInvizibilitate.Value += 3f; 
        cooldownInvizibilitate.Value -= 2f; 
        multiplicatorDamageBackstab.Value += 1; 
    }
}
