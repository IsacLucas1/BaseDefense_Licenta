using UnityEngine;

public class TankPlayer : BasePlayer
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 3f;
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
        }
    }
}
