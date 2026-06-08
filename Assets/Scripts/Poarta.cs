using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Poarta : Zid
{
    [Header("SetariPoarta")] 
    public GameObject grilajPoarta;
    public Slider healthPoartaSliderInterior;
    public Slider healthPoartaSliderExterior;
    public TextMeshProUGUI healthPoartaTextInterior;
    public TextMeshProUGUI healthPoartaTextExterior;

    public NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
    private Coroutine fadeCoroutine;
    private Collider[] grilajPoartaColliders;
    public NetworkVariable<double> timpUltimaSchimbare = new NetworkVariable<double>(-100f);

    private void Start()
    {
        if (grilajPoarta != null)
        {
            grilajPoartaColliders = grilajPoarta.GetComponentsInChildren<Collider>();
            wallRenderer = grilajPoarta.GetComponentInChildren<Renderer>();
            
            if (wallRenderer != null)
            {
                culoareOriginala = wallRenderer.materials[0].HasProperty("_BaseColor") 
                    ? wallRenderer.materials[0].GetColor("_BaseColor") 
                    : wallRenderer.materials[0].color;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            viata.Value = viataMax.Value;
            if (healthPoartaTextInterior != null)
            {
                healthPoartaTextInterior.text = "<sprite=0> " + viata.Value + " / " + viataMax.Value;
            }
            if (healthPoartaTextExterior != null)
            {
                healthPoartaTextExterior.text = "<sprite=0> " + viata.Value + " / " + viataMax.Value;
            }
        }
        
        if (healthPoartaSliderInterior != null)
        {
            healthPoartaSliderInterior.maxValue = viataMax.Value;
            healthPoartaSliderInterior.value = viata.Value;
        }
        if (healthPoartaSliderExterior != null)
        {
            healthPoartaSliderExterior.maxValue = viataMax.Value;
            healthPoartaSliderExterior.value = viata.Value;
        }
        
        isOpen.OnValueChanged += (oldVal, newVal) => HandleGateStateChange(newVal);
        
        if (viata.Value > 0 && isOpen.Value)
        {
            float timpTrecut = (float)(NetworkManager.ServerTime.Time - timpUltimaSchimbare.Value);
            
            if (timpTrecut >= 3f)
            {
                if (isOpen.Value)
                {
                    if (wallRenderer != null) wallRenderer.enabled = false;
                    SeteazaOpacitate(0f, false);
                }
                else
                {
                    if (wallRenderer != null) wallRenderer.enabled = true;
                    SeteazaOpacitate(1f, true);
                }
            }
            else
            {
                float targetAlpha = isOpen.Value ? 0f : 1f;
                if (!isOpen.Value && wallRenderer != null) wallRenderer.enabled = true; 
                
                fadeCoroutine = StartCoroutine(FadeGate(targetAlpha, 3f, isOpen.Value, timpTrecut));
            }
        }
    }

    protected override void ActualizeazaZid(int viataCurenta)
    {
        if (healthPoartaSliderInterior != null)
        {
            healthPoartaSliderInterior.maxValue = viataMax.Value;
            healthPoartaSliderInterior.value = viataCurenta;
        }
        if (healthPoartaSliderExterior != null)
        {
            healthPoartaSliderExterior.maxValue = viataMax.Value;
            healthPoartaSliderExterior.value = viataCurenta;
        }
        
        if (healthPoartaTextInterior != null)
        {
            healthPoartaTextInterior.text = "<sprite=0> " + viataCurenta + " / " + viataMax.Value;
        }
        if (healthPoartaTextExterior != null)
        {
            healthPoartaTextExterior.text = "<sprite=0> " + viataCurenta + " / " + viataMax.Value;
        }
        
        if (viataCurenta <= 0)
        {
            if (grilajPoarta != null)
            {
                grilajPoarta.SetActive(false);
            }
            
            SeteazaStareColiziuni(false);
            
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = false;
            }
        }
        else
        {
            if (grilajPoarta != null && !grilajPoarta.activeSelf)
            {
                grilajPoarta.SetActive(true);
            }
            
            bool areColiziune = !isOpen.Value;
            
            SeteazaStareColiziuni(areColiziune);
            
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = areColiziune;
            }
            if (!isOpen.Value)
            {
                SeteazaOpacitate(1f, true);
            }
        }
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestToggleGateServerRpc()
    {
        if (viata.Value > 0)
        {
            isOpen.Value = !isOpen.Value;
            timpUltimaSchimbare.Value = NetworkManager.ServerTime.Time;
        }
    }
    
    private void HandleGateStateChange(bool open)
    {
        if (viata.Value <= 0)
        {
            return;
        } 
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        float targetAlpha = open ? 0f : 1f;
        
        if (!open && wallRenderer != null)
        {
            wallRenderer.enabled = true; 
        }
        fadeCoroutine = StartCoroutine(FadeGate(targetAlpha, 3f, open, 0f));
    }
    
    private void SeteazaStareColiziuni(bool stare)
    {
        if (grilajPoartaColliders != null)
        {
            foreach (Collider col in grilajPoartaColliders)
            {
                col.enabled = stare;
            }
        }
    }
    
    private IEnumerator FadeGate(float targetAlpha, float duration, bool isOpen, float timpInceput)
    {
        if (wallRenderer == null) yield break;
        float startAlpha = wallRenderer.materials[0].HasProperty("_BaseColor") 
            ? wallRenderer.materials[0].GetColor("_BaseColor").a 
            : wallRenderer.materials[0].color.a;
        
        float time = timpInceput;
        
        SeteazaOpacitate(startAlpha, false);
        while (time < duration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SeteazaOpacitate(newAlpha, false);
            yield return null;
        }
        SeteazaOpacitate(targetAlpha, targetAlpha >= 1f);
        if (isOpen)
        {
            SeteazaStareColiziuni(false);
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = false;
            }
            wallRenderer.enabled = false; 
        }
        else
        {
            SeteazaStareColiziuni(true);
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
            }
        }
    }
    
    public bool EsteAccesibilFizic()
    {
        if (isOpen.Value == true)
        {
            if (wallRenderer != null)
            {
                return !wallRenderer.enabled; 
            }
        }
        else 
        {
            if (navMeshObstacle != null)
            {
                return !navMeshObstacle.enabled;
            }
        }
        
        return isOpen.Value;

    }
}
