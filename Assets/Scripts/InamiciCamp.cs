using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine.UI;

public class InamiciCamp : InamiciAI
{
    [Header("Setari Camp Inamici")]
    private Vector3 centruCamp;
    public float timpRespawn = 10f;
    
    [Header("UI Respawn")]
    public GameObject canvasRespawn;
    public Image imagineRespawn;
    
    private Coroutine corutinaUI;
    private Camera cam;
    public float inaltimeCanvas = 2f;
    
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); 
        
        centruCamp = transform.position;
        
        cam = Camera.main;
        
        if (canvasRespawn != null)
        {
            canvasRespawn.SetActive(false);
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
        
        float timpRealRespawn = Mathf.Max(0f, timpRespawn - 0.2f);
        
        PornesteUIRespawnClientRpc(timpRealRespawn);
        StartCoroutine(RespawnRoutine());
    }

    [ClientRpc]
    private void PornesteUIRespawnClientRpc(float durata)
    {
        if (corutinaUI != null)
        {
            StopCoroutine(corutinaUI);
        }
        corutinaUI = StartCoroutine(ActualizeazaCercUI(durata));
    }

    private IEnumerator ActualizeazaCercUI(float durata)
    {
        if (canvasRespawn != null)
        {
            canvasRespawn.SetActive(true);
        }

        float timpScurs = 0f;

        while (timpScurs < durata)
        {
            timpScurs += Time.deltaTime;
            
            if (canvasRespawn != null)
            {
                canvasRespawn.transform.position = centruCamp + new Vector3(0, inaltimeCanvas, 0);
            }
            
            if (imagineRespawn != null)
            {
                imagineRespawn.fillAmount = timpScurs / durata;
            }
            yield return null;
        }

        if (canvasRespawn != null)
        {
            canvasRespawn.SetActive(false);
        }
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

    private void LateUpdate()
    {
        if (canvasRespawn != null && canvasRespawn.activeSelf)
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                canvasRespawn.transform.rotation = cam.transform.rotation;
            }
        }
    }
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); 
        
        Vector3 centru = Application.isPlaying ? centruCamp : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centru, distantaMaxUrmarire);
    }
}