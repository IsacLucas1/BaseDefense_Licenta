using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Netcode.Components;

public class InamiciCamp : InamiciAI
{
    [Header("Setari Camp Inamici")]
    private Vector3 centruCamp;
    public float timpRespawn = 10f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); 
        
        if (IsServer)
        {
            centruCamp = transform.position;
        }
    }

    
    protected override bool VerificaLimitaUrmarire(Vector3 pozitieJucator)
    {
        float distantaJucatorCentru = Vector3.Distance(pozitieJucator, centruCamp);
        return distantaJucatorCentru <= distantaMaxUrmarire;
    }

    
    protected override void ComportamentFaraTinta()
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

    public override void Moarte(BasePlayer killer)
    {
        base.Moarte(killer); 

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

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); 
        
        Vector3 centru = Application.isPlaying ? centruCamp : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centru, distantaMaxUrmarire);
    }
}