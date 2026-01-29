using UnityEngine;

public class ArcasPlayer: BasePlayer
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 7f;
        transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.cyan);
        }
        
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 80;
                health.currentHealth.Value = 80;
            }
        }
    }
}
