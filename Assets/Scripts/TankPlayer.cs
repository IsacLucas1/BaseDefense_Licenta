using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TankPlayer : MeleePlayer
{
    [Header("Setari Tanc")] 
    public float tauntRadius = 15f;
    public float tauntDuration = 5f;
    public float tauntCooldown = 10f;
    private float nextTauntTime = 0f;
    
    public ParticleSystem tauntParticles;
    
    public override void OnNetworkSpawn()
    {
        damageArma = 20;
        atacCooldown = 2f;
        durataAnimatie = 0.8f;
        
        if (IsServer)
        {
            speed.Value = 4f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 200;
                health.currentHealth.Value = 200;
            }
        }
        
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
        }
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

            if (Time.time < nextTauntTime)
            {
                float timpRamas = nextTauntTime- Time.time; 
                procentaj = 1f - (timpRamas / tauntCooldown);
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
        nextTauntTime = Time.time + tauntCooldown;
        TauntServerRpc();
    }

    [ServerRpc]
    private void TauntServerRpc()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, tauntRadius);
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
    
}
