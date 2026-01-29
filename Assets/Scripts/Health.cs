using UnityEngine;
using Unity.Netcode;


public class Health : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100);

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
}
