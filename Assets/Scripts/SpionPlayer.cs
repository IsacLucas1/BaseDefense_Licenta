using UnityEngine;

public class SpionPlayer : BasePlayer
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        speed = 8f;
        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
        }
    }
}
