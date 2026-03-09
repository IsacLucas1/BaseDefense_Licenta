using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class InamiciAI : NetworkBehaviour
{
    [Header("Setari Camp Inamici")]
    private Vector3 centruCamp;
    
    [Header("Setari Urmarire")]
    public float razaDetectie = 9f;
    public float distantaMaxUrmarire = 15f;
    
    [Header("Setari Atac")]
    public float razaAtac = 2f;
    public int damageAtac = 10;
    public float cooldownAtac = 1.5f;

    private float nextAttackTime = 0f;
    
    private NavMeshAgent agent;
    private Transform tinta;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
    }

    private void Update()
    {
        if(!IsServer)
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
