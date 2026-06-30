using UnityEngine;
using Unity.Netcode;


public class Health : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(100);

    private BasePlayer basePlayer;
    private InamiciAI inamiciAI;
    
    // Bifat doar pe poarta inamica: invulnerabila pana incepe atacul final
    [Tooltip("Bifat doar pe poarta inamica: invulnerabil pana incepe atacul final")]
    public bool invulnerabilPanaLaAtaculFinal = false;
    
    // Seteaza referintele la instantiere
    private void Awake()
    {
        basePlayer = GetComponent<BasePlayer>();
        inamiciAI = GetComponent<InamiciAI>();
    }
    
    // Abonare si dezabonare la evenimentul de schimbare a vietii
    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    // Metoda prin care o entitate (inamic sau jucator) primeste o lovitura
    public void TakeDamage(int damage, ulong attackerId = ulong.MaxValue)
    {
        if (IsServer)
        {
            // Daca inamicul nu este actic, nu se aplica damage
            if (inamiciAI != null && inamiciAI.EsteAdormit)
            {
                return;
            }
            
            // Daca este invulnerabil pana la atacul final si atacul final nu a inceput, nu se aplica damage
            if (invulnerabilPanaLaAtaculFinal && (FinalAttackManager.Instance == null || !FinalAttackManager.Instance.aInceputAtacul.Value))
            {
                return;
            }
            
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                BasePlayer killerPlayer = null;
                
                // Determina cine a ucis inamicul
                if (attackerId != ulong.MaxValue)
                {
                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerId, out NetworkClient client))
                    {
                        killerPlayer = client.PlayerObject.GetComponent<BasePlayer>();
                    }
                }
                
                // Apeleaza metoda Moarte() a entitatii care a murit
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
    
    // Se apeleaza cand valoarea vietii se schimba in retea
    // Afiseaza un mesaj in consola si verifica daca entitatea a murit
    private void OnHealthChanged(int oldValue, int newValue)
    {
        Debug.Log(transform.name + " a primit damage. Viata curenta: " + newValue);
        if (newValue <= 0)
        {
             Debug.Log(transform.name + " a murit.");
        }
    }
    
    // Reseteaza viata entitatii la valoarea maxima dupa respawn
    public void ResetHealth()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth.Value;
        }
    }

    // Adauga o cantitate de viata la un jucator fara a depasi maximul de viata
    public void Heal(int healAmount)
    {
        if (IsServer)
        {
            int viataNoua = Mathf.Clamp(currentHealth.Value + healAmount, 0, maxHealth.Value);
            currentHealth.Value = viataNoua;
        }
    }
}
