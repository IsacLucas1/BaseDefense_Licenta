using UnityEngine;

public class SpionPlayer : BasePlayer
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            speed.Value = 7f;
        
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 100;
                health.currentHealth.Value = 100;
            }
        }
        
        base.OnNetworkSpawn();
        
        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
        }
    }
}
