using UnityEngine;
using Unity.Netcode;


public class Health : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100);

    private BasePlayer basePlayer;
    
    private void Awake()
    {
        basePlayer = GetComponent<BasePlayer>();
    }
    
    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                if (basePlayer != null)
                {
                    basePlayer.Moarte();
                }
            }
        }
    }
    
    private void OnHealthChanged(int oldValue, int newValue)
    {
        Debug.Log(transform.name + " a primit daune. Sănătate curentă: " + newValue);
        if (newValue <= 0)
        {
             Debug.Log(transform.name + " a murit.");
        }
    }
    
    public void ResetHealth()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth.Value;
        }
    }

    public void Heal(int healAmount)
    {
        if (IsServer)
        {
            currentHealth.Value += healAmount;

            if (currentHealth.Value > maxHealth.Value)
            {
                currentHealth.Value = maxHealth.Value;
            }
        }
    }
}
