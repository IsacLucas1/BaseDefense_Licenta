using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public abstract class InamiciAI : NetworkBehaviour
{
    [Header("Setari Urmarire")]
    public float razaDetectie = 14f;
    public float distantaMaxUrmarire = 16f;
    
    [Header("Setari Atac")]
    public float razaAtac = 2f;
    public int damageAtac = 10;
    public float cooldownAtac = 1.5f;

    [Header("Efecte Status (Crowd Control)")]
    protected Transform tauntTarget;
    protected float tauntEndTime = 0f;
    
    protected float nextAttackTime = 0f;
    protected NavMeshAgent agent;
    protected Transform tinta;
    protected Health health;
    
    protected NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
        }

        if (IsServer && health != null)
        {
            health.maxHealth.Value = 50;
            health.currentHealth.Value = 50;
        }
        
        isDead.OnValueChanged += OnDeathStateChanged;
        ToggleVisuals(!isDead.Value);
    }
    
    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeathStateChanged;
    }

    protected virtual void Update()
    {
        if (!IsServer || isDead.Value) return;

        GasesteJucator();

        if (tinta != null)
        {
            UrmaresteSiAtacaJucator();
        }
        else
        {
            ComportamentFaraTinta(); 
        }
    }

    public void AplicaTaunt(Transform noulTauntTarget, float durataTaunt)
    {
        if (!IsServer || isDead.Value)
        {
            return;
        }
        
        tauntTarget = noulTauntTarget;
        tauntEndTime = Time.time + durataTaunt;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }
    }
    
    protected virtual void GasesteJucator()
    {
        if(Time.time < tauntEndTime && tauntTarget != null)
        {
            if (VerificaLimitaUrmarire(tauntTarget.position))
            {
                tinta = tauntTarget;
                return;
            }
        }
        else if (Time.time < tauntEndTime && tauntTarget == null)
        {
            tauntTarget = null;
        }
        
        float razaCautare = razaDetectie * 1.5f;
        Collider[] jucatoriInZona = Physics.OverlapSphere(transform.position, razaCautare);
        
        Transform celMaiApropiatJucator = null;
        float distantaMinima = Mathf.Infinity;

        foreach (Collider col in jucatoriInZona)
        {
            BasePlayer player = col.GetComponent<BasePlayer>();
            if (player != null && !player.isDead && !player.isInvisible.Value)
            {
                if (!VerificaLimitaUrmarire(col.transform.position)) continue;
                
                float distantaJucatorInamic = Vector3.Distance(transform.position, col.transform.position);
                float distantaEvaluata = distantaJucatorInamic;
                
                if (tinta == col.transform) distantaEvaluata -= 2f;
                
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
            if (agent.hasPath) agent.ResetPath();
        }
        tinta = celMaiApropiatJucator;
    }
    
    protected virtual bool VerificaLimitaUrmarire(Vector3 pozitieJucator) { return true; }

    protected virtual void UrmaresteSiAtacaJucator()
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
    
    protected virtual void Ataca()
    {
        if (tinta != null)
        {
            Health healthTinta = tinta.GetComponent<Health>();
            if (healthTinta != null) healthTinta.TakeDamage(damageAtac);
        }
    }
    
    protected abstract void ComportamentFaraTinta();
    
    public virtual void Moarte(BasePlayer killer)
    {
        if (!IsServer) return;
        isDead.Value = true;
        agent.enabled = false;
        tinta = null;
    }

    protected virtual void OnDeathStateChanged(bool wasDead, bool isDeadNow)
    {
        ToggleVisuals(!isDeadNow);
    }
    
    protected void ToggleVisuals(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is LineRenderer) continue;
            r.enabled = active;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = active;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases) c.enabled = active;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, razaDetectie); 
        Gizmos.color = new Color(0.8f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, razaAtac);
    }
}