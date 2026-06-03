using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;

public class HealthDisplay : NetworkBehaviour
{
    [Header("Referinte UI")]
    public Slider healthBar;
    public Health health;
    private Camera cam;
    
    private void Start()
    {
        cam = Camera.main;
        if (healthBar != null)
        {
            healthBar.interactable = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }
        if (health != null)
        {
            BasePlayer player = health.GetComponent<BasePlayer>();
            if (player != null && player.IsOwner)
            {
                gameObject.SetActive(false);
                return;
            }
            
            health.maxHealth.OnValueChanged += SetMaxHealth;
            health.currentHealth.OnValueChanged += UpdateHealthBar;
            bool isNetworkDefault = (health.maxHealth.Value == 100);

            if (!IsServer || !isNetworkDefault)
            {
                SetMaxHealth(0, health.maxHealth.Value);
                UpdateHealthBar(0, health.currentHealth.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
        {
            health.currentHealth.OnValueChanged -= UpdateHealthBar;
        }
    }
    
    void SetMaxHealth(int oldMax, int newMax)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = newMax;
            
            if (health != null && healthBar.value < health.currentHealth.Value)
            {
                healthBar.value = health.currentHealth.Value;
            }
        }
    }
    void UpdateHealthBar(int oldHealth, int newHealth)
    {
        if (healthBar != null)
        {
            if (newHealth > healthBar.maxValue)
            {
                healthBar.maxValue = newHealth;
            }
            healthBar.value = newHealth;
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            return;
        }
        transform.rotation = cam.transform.rotation;
    }
}
