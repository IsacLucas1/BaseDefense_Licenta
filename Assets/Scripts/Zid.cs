using UnityEngine;
using Unity.Netcode;
using UnityEngine.Analytics;
using System.Collections;
using UnityEngine.AI;

public class Zid : NetworkBehaviour
{
    [Header("Setari Zid")]
    public int viataMax = 100;

    public NetworkVariable<int> viata = new NetworkVariable<int>(0);
    
    private Collider zidCollider;
    private Renderer wallRenderer;
    private Color culoareOriginala;
    private NavMeshObstacle navMeshObstacle;
    
    private void Awake()
    {
        zidCollider = GetComponent<Collider>();
        wallRenderer = GetComponentInChildren<Renderer>();
        navMeshObstacle = GetComponent<NavMeshObstacle>();
        if (wallRenderer != null)
        {
            if (wallRenderer.materials[0].HasProperty("_BaseColor"))
            {
                culoareOriginala = wallRenderer.materials[0].GetColor("_BaseColor");
            }
            else
            {
                culoareOriginala = wallRenderer.materials[0].color;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        viata.OnValueChanged += LaSchimbareViata;
        ActualizeazaZid(viata.Value);
    }
    
    public override void OnNetworkDespawn()
    {
        viata.OnValueChanged -= LaSchimbareViata;
    }
    
    private void LaSchimbareViata(int oldValue, int newValue)
    {
        ActualizeazaZid(newValue);
    }
    
    private void ActualizeazaZid(int viataCurenta)
    {
        if (viataCurenta <= 0)
        {
            if (zidCollider != null)
            {
                zidCollider.isTrigger = true;
            }

            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = false;
            }
            SeteazaOpacitate(0.3f, false);
        }
        else if (viataCurenta >= viataMax)
        {
            if (zidCollider != null)
            {
                zidCollider.isTrigger = false;
            }
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
            }
            SeteazaOpacitate(1f, true);
        }
        else
        {
            if (zidCollider != null)
            {
                zidCollider.isTrigger = false;
            }
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
            }
            float procent = (float)viataCurenta / viataMax;
            float alpha = Mathf.Lerp(0.3f, 1f, procent);
            SeteazaOpacitate(alpha, false);
        }
    }

    private void SeteazaOpacitate(float alpha, bool devineOpac)
    {
        if (wallRenderer != null)
        {
            foreach (Material mat in wallRenderer.materials)
            {
                Color c = culoareOriginala;
                c.a = alpha;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", c);
                }

                if (devineOpac)
                {
                    mat.SetFloat("_Surface", 0f);
                    mat.SetFloat("_ZWrite", 1f);
                    mat.renderQueue = 2000;
                }
                else
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.renderQueue = 3000;
                }
            }
            
        }
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ConstruiesteSauReparaServerRpc(int viataAdaugata)
    {
        viata.Value = Mathf.Min(viata.Value + viataAdaugata, viataMax);
    }
    
    public void PrimesteDamage(int damage)
    {
        if (!IsServer)
        {
            return;
        }
        viata.Value = Mathf.Max(viata.Value - damage, 0);
    }
}
