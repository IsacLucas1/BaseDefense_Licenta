using UnityEngine;

public class MedicPlayer : BasePlayer
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 6f;
        transform.localScale = new Vector3(1f, 1f, 1f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
        }
        
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 100;
                health.currentHealth.Value = 100;
            }
        }
    }
    
}
