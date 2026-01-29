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
    }

    public override void OnNetworkSpawn()
    {
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }
        if (health != null)
        {
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
        healthBar.maxValue = newMax;
    }
    void UpdateHealthBar(int oldHealth, int newHealth)
    {
        healthBar.value = newHealth;
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
