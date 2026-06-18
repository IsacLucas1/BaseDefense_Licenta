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
    
    protected bool adormit = false;
    public bool EsteAdormit => adormit;
    
    protected NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    public bool EsteMort => isDead.Value;
    
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
        if (!IsServer || isDead.Value)
        {
            return;
        }

        if (adormit)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
            }
            return;
        }
        
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
            tinta = tauntTarget;
            return;
        }
        else if (Time.time >= tauntEndTime)
        {
            tauntTarget = null;
        }
        
        float razaCautare = razaDetectie * 1.25f;
        Collider[] jucatoriInZona = Physics.OverlapSphere(transform.position, razaCautare);
        
        Transform celMaiApropiatJucator = null;
        float distantaMinima = Mathf.Infinity;

        foreach (Collider col in jucatoriInZona)
        {
            BasePlayer player = col.GetComponent<BasePlayer>();
            if (player != null && !player.isDead.Value && !player.isInvisible.Value)
            {
                if (!VerificaLimitaUrmarire(col.transform.position)) continue;
                if (!PoateUrmariJucator(player)) continue;
                
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
            if (agent.hasPath) agent.ResetPath();
        }
        tinta = celMaiApropiatJucator;
    }

    protected virtual bool VerificaLimitaUrmarire(Vector3 pozitieJucator)
    {
        return true;
    }

    protected virtual bool PoateUrmariJucator(BasePlayer player)
    {
        return true;
    }

    protected virtual void UrmaresteSiAtacaJucator()
    {
        Collider tintaCol = tinta.GetComponentInChildren<Collider>();
        Vector3 punctTinta = tintaCol != null ? tintaCol.ClosestPoint(transform.position) : tinta.position;
        float distantaJucator = Vector3.Distance(transform.position, punctTinta);

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

            if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Vector3 directieSpreTinta = (tinta.position - transform.position).normalized;
                
                if (Physics.Raycast(transform.position + Vector3.up, directieSpreTinta, out RaycastHit hit, Vector3.Distance(transform.position, tinta.position)))
                {
                    Zid zid = hit.collider.GetComponentInParent<Zid>();
                    
                    if (zid != null && zid.viata.Value > 0)
                    {
                        Collider zidCol = hit.collider; 
                        Vector3 punctSuprafata = zidCol.ClosestPoint(transform.position);
                        float distantaPanaLaZid = Vector3.Distance(transform.position, punctSuprafata);

                        if (distantaPanaLaZid <= razaAtac + 0.5f)
                        {
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            
                            Vector3 directieZid = new Vector3(zid.transform.position.x, transform.position.y, zid.transform.position.z);
                            transform.LookAt(directieZid);

                            if (Time.time >= nextAttackTime)
                            {
                                Transform tintaVeche = tinta; 
                                tinta = zid.transform;
                                Ataca();
                                tinta = tintaVeche;
                                nextAttackTime = Time.time + cooldownAtac;
                            }
                        }
                    }
                }
            }
        }
    }
    
    protected virtual void Ataca()
    {
        if (tinta != null)
        {
            Health healthTinta = tinta.GetComponent<Health>();
            if (healthTinta != null)
            {
                healthTinta.TakeDamage(damageAtac);
            }
            else
            {
                Zid zidTinta = tinta.GetComponentInParent<Zid>();
                if (zidTinta != null)
                {
                    zidTinta.PrimesteDamage(damageAtac);
                }
            }
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
        foreach (var c in colliders)
        {
            c.enabled = active;
        }

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.gameObject.name == "CanvasRespawnCamp")
            {
                continue;
            }
            c.enabled = active;
        }
    }
    
    public void SeteazaAdormit(bool stare)
    {
        if (!IsServer)
        {
            return;
        }
        adormit = stare;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = stare;
        }
    }

    public virtual void Trezeste()
    {
        if (!IsServer)
        {
            return;
        }
        adormit = false;
        Debug.Log($"[Trezeste] {name}: agentActiv={(agent != null && agent.isActiveAndEnabled)}, peNavMesh={(agent != null && agent.isOnNavMesh)}");
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, razaDetectie); 
        Gizmos.color = new Color(0.8f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, razaAtac);
    }
}
