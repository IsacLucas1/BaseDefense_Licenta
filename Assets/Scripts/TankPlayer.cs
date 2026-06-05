using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TankPlayer : MeleePlayer
{
    [Header("Setari Tanc")] 
    public NetworkVariable<float> tauntRadius = new NetworkVariable<float>(20f);
    public float tauntDuration = 5f;
    public NetworkVariable<float> tauntCooldown = new NetworkVariable<float>(10f);
    private float nextTauntTime = 0f;
    
    public ParticleSystem tauntParticles;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            damageArma.Value = 20;
            atacCooldown.Value = 2f;
            
            speed.Value = 4f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 200;
                health.currentHealth.Value = 200;
            }
        }
        durataAnimatie = 0.8f;
        base.OnNetworkSpawn();
    }

    protected override void SetupLocalPlayer()
    {
        base.SetupLocalPlayer();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SeteazaVizibilitateTaunt(true);
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

            if (Time.time < nextTauntTime)
            {
                float timpRamas = nextTauntTime- Time.time; 
                procentaj = 1f - (timpRamas / tauntCooldown.Value);
            }
            
            UIManager.Instance.ActualizeazaCooldownTaunt(procentaj);
        }
        
        if (Input.GetKeyDown(KeyCode.T) && Time.time >= nextTauntTime)
        {
            AnuleazaRecall();
            ActiveazaTaunt();
        }
        
        
    }
    
    private void ActiveazaTaunt()
    {
        nextTauntTime = Time.time + tauntCooldown.Value;
        TauntServerRpc();
    }

    [ServerRpc]
    private void TauntServerRpc()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, tauntRadius.Value);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                InamiciAI inamic = hitCollider.GetComponent<InamiciAI>();
                if (inamic != null)
                {
                    inamic.AplicaTaunt(this.transform, tauntDuration);
                }
            }
        }
        PlayTauntEffectClientRpc();
    }

    [ClientRpc]
    private void PlayTauntEffectClientRpc()
    {
        if(tauntParticles != null)
        {
            tauntParticles.Play();
        }
        Debug.Log("Tancul a activat Taunt!");
    }
    
    protected override void AplicaUpgradeClasa()
    {
        tauntRadius.Value += 10f; 
        tauntCooldown.Value -= 3f; 
        Debug.Log("Tancul a primit Super-Taunt!");
    }
}
