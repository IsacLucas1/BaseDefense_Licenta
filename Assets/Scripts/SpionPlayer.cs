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

    private void SeteazaTransparent(Material m, bool transparent)
    {
        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 1f); 
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }
    
    [ClientRpc]
    private void UpdateVizualInvizibilitateClientRpc(bool invizibil)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.GetComponent<ParticleSystem>() != null || r is LineRenderer)
            {
                continue;
            }
            
            SeteazaTransparent(r.material, invizibil);
            if (r.material.HasProperty("_BaseColor"))
            {
                Color col = r.material.GetColor("_BaseColor");
                col.a = invizibil ? 0.4f : 1.0f; 
                r.material.SetColor("_BaseColor", col);
            }
        }
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

        if (targetObj.GetComponent<InamiciAI>() != null)
        {
            Vector3 directiePrivireSpion = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 directiePrivireInamic =
                new Vector3(targetObj.transform.forward.x, 0, targetObj.transform.forward.z).normalized;

            float unghi = Vector3.Dot(directiePrivireSpion, directiePrivireInamic);

            if (unghi > tolerantaUnghiBackstab)
            {
                damageFinal = damageArma.Value * multiplicatorDamageBackstab.Value;
                Vector3 punctAtac = transform.position + transform.forward * distantaLovitura + Vector3.up * 0.1f;
                Collider colInamic = targetObj.GetComponent<Collider>();
                Vector3 pozEfect = colInamic != null ? colInamic.ClosestPoint(punctAtac) : punctAtac;
                PlayBackstabEffectsClientRpc(pozEfect);
            }
        }

        return damageFinal + extraDamage.Value;
    }
    
    [ClientRpc]
    private void PlayBackstabEffectsClientRpc(Vector3 pozitie)
    {
        if (backstabParticles != null)
        {
            backstabParticles.transform.position = pozitie + Vector3.up * 1f;
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
    
    protected override string ObtinePromptInteractiune(Collider col)
    {
        LeverCasa lever = col.GetComponent<LeverCasa>();
        if (lever != null)
        {
            return "Press [E] pentru a trage maneta";
        }
        return base.ObtinePromptInteractiune(col);
    }
}
