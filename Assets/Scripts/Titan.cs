using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Titan : NetworkBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged += OnHealthChanged;
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
        if (IsServer && nou <= 0)
        {
            if (FinalAttackManager.Instance != null)
            {
                FinalAttackManager.Instance.Infrangere();
            }
        }
    }
}