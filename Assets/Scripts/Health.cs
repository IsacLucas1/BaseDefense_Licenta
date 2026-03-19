using UnityEngine;
using Unity.Netcode;


public class Health : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100);

    private BasePlayer basePlayer;
    private InamiciAI inamiciAI;
    
    private void Awake()
    {
        basePlayer = GetComponent<BasePlayer>();
        inamiciAI = GetComponent<InamiciAI>();
    }
    
    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    public void TakeDamage(int damage, ulong attackerId = ulong.MaxValue)
    {
        if (IsServer)
        {
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                BasePlayer killerPlayer = null;
                
                if (attackerId != ulong.MaxValue)
                {
                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerId, out NetworkClient client))
                    {
                        killerPlayer = client.PlayerObject.GetComponent<BasePlayer>();
                    }
                }
                
                if (basePlayer != null)
                {
                    basePlayer.Moarte();
                }
                else if (inamiciAI != null)
                {
                    inamiciAI.Moarte(killerPlayer);
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
            int viataNoua = Mathf.Clamp(currentHealth.Value + healAmount, 0, maxHealth.Value);
            currentHealth.Value = viataNoua;
        }
    }
}
