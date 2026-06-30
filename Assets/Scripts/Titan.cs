using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Health))]
public class Titan : NetworkBehaviour
{
    private Health health;

    [Header("Bara de viata Titan")]
    public Slider healthTitanSlider;
    public TextMeshProUGUI healthTitanText;
    
    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged += OnHealthChanged;
            ActualizeazaBara(health.currentHealth.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(int vechi, int nou)
    {
        ActualizeazaBara(nou);
        if (IsServer && nou <= 0)
        {
            if (FinalAttackManager.Instance != null)
            {
                FinalAttackManager.Instance.Infrangere();
            }
        }
    }
    
    private void ActualizeazaBara(int viataCurenta)
    {
        if (healthTitanText != null)
        {
            healthTitanText.text = "<sprite=0> " + viataCurenta + " / " + health.maxHealth.Value;
        }
    }
}