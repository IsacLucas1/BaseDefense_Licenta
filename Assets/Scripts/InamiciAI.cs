using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.Netcode.Components;

public class InamiciAI : NetworkBehaviour
{
    [Header("Setari Camp Inamici")]
    private Vector3 centruCamp;
    public float timpRespawn = 10f;
    
    [Header("Setari Urmarire")]
    public float razaDetectie = 14f;
    public float distantaMaxUrmarire = 16f;
    
    [Header("Setari Atac")]
    public float razaAtac = 2f;
    public int damageAtac = 10;
    public float cooldownAtac = 1.5f;

    private float nextAttackTime = 0f;
    
    private NavMeshAgent agent;
    private Transform tinta;
    private Health health;
    
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            centruCamp = transform.position;
        }
        else
        {
            agent.enabled = false;
        }

        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 50;
                health.currentHealth.Value = 50;
            }
        }
        
        isDead.OnValueChanged = OnDeathStateChanged;
        ToggleVisuals(!isDead.Value);
    }
    
    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    private void Update()
    {
        if(!IsServer || isDead.Value)
        {
            return;
        }

        GasesteJucator();

        if (tinta != null)
        {
            UrmaresteSiAtacaJucator();
        }
        else
        {
            IntoarcereCamp();
        }
    }

    private void GasesteJucator()
    {
        float razaCautare = razaDetectie * 1.5f;
        Collider[] jucatoriInZona = Physics.OverlapSphere(transform.position, razaCautare);
        
        Transform celMaiApropiatJucator = null;
        float distantaMinima = Mathf.Infinity;

        foreach (Collider col in jucatoriInZona)
        {
            BasePlayer player = col.GetComponent<BasePlayer>();
            if (player != null && !player.isDead)
            {
                float distantaJucatorCentru = Vector3.Distance(col.transform.position, centruCamp);
                if(distantaJucatorCentru > distantaMaxUrmarire)
                {
                    continue;
                }
                
                float distantaJucatorInamic = Vector3.Distance(transform.position, col.transform.position);
                float distantaEvaluata = distantaJucatorInamic;
                if (tinta == col.transform)
                {
                    distantaEvaluata -= 2f;
                }
                
                float razaAcceptata = (tinta == col.transform) ? razaCautare : razaDetectie;
                if(distantaJucatorInamic <= razaAcceptata && distantaEvaluata < distantaMinima)
                {
                    distantaMinima = distantaEvaluata;
                    celMaiApropiatJucator = col.transform;
                }
            }
        }

        if (tinta != celMaiApropiatJucator)
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
        tinta = celMaiApropiatJucator;
    }

    private void UrmaresteSiAtacaJucator()
    {
        float distantaJucator = Vector3.Distance(transform.position, tinta.position);

        if (distantaJucator <= razaAtac)
        {
            agent.isStopped = true;

            Vector3 directieprivire = new Vector3(tinta.position.x, transform.position.y, tinta.position.z);
            transform.LookAt(directieprivire);

            if (Time.time >= nextAttackTime)
            {
                Ataca();
                nextAttackTime = Time.time + cooldownAtac;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(tinta.position);
        }
    }
    
    private void Ataca()
    {
        if (tinta != null)
        {
            Health healthTinta = tinta.GetComponent<Health>();
            if (healthTinta != null)
            {
                healthTinta.TakeDamage(damageAtac);
            }
        }
    }

    private void IntoarcereCamp()
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }
        if (Vector3.Distance(transform.position, centruCamp) > 1f)
        {
            if (Vector3.Distance(agent.destination, centruCamp) > 1f)
            {
                agent.SetDestination(centruCamp);
            }
        }
        else
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    public void Moarte(BasePlayer killer)
    {
        if(!IsServer)
        {
            return;
        }
        
        isDead.Value = true;
        agent.enabled = false;
        tinta = null;

        CampReward loot = GetComponent<CampReward>();
        if (loot != null)
        {
            loot.OferaRecompensa(killer);
        }
        
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        float timpAsteptareInitial = Mathf.Max(0f, timpRespawn - 0.2f);
        yield return new WaitForSeconds(timpAsteptareInitial);

        agent.enabled = false;
        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(centruCamp, transform.rotation, transform.localScale);
        }
        else
        {
            transform.position = centruCamp;
        }
        
        agent.Warp(centruCamp);
        
        if (health != null)
        {
            health.ResetHealth();
        }
        
        isDead.Value = false;
        agent.enabled = true;
    }

    private void OnDeathStateChanged(bool wasDead, bool isDeadNow)
    {
        ToggleVisuals(!isDeadNow);
    }
    
    private void ToggleVisuals(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is LineRenderer)
            {
                continue;
            }
            r.enabled = active;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            c.enabled = active;
        }

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            c.enabled = active;
        }
    }
    
    private void OnDrawGizmos()
    {
        Vector3 centru = Application.isPlaying ? centruCamp : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, razaDetectie); // Zona in care te vede

        Gizmos.color = new Color(0.8f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, razaAtac);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centru, distantaMaxUrmarire); // Cat de departe merge inainte sa se dea batut
    }
}
