using UnityEngine;

public class TankPlayer : BasePlayer
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            speed.Value = 4f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 200;
                health.currentHealth.Value = 200;
            }
        }
        
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
        }
    }
}
