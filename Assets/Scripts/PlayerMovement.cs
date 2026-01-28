using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.blue;
        }
    }
    
    void Update()
    {
        if (!IsOwner) return;
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(x, 0, z).normalized;
        
        transform.position += movement * speed * Time.deltaTime;

    }
}
