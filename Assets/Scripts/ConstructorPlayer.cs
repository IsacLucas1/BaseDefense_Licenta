using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class ConstructorPlayer : BasePlayer
{ 
    [Header("Setari Constructor")]
    public int multiplicatorLemn = 2;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

       
        
        speed = 5f;
        transform.localScale = new Vector3(1f, 1f, 1f);
        
        if (GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetColor("_BaseColor", Color.yellow);
        }
        
        if (IsServer)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                health.maxHealth.Value = 120;
                health.currentHealth.Value = 120;
            }
        }
    }

    public override void AdaugaLemn(int cantitate)
    {
        if (IsServer)
        {
             int lemnAdaugat = cantitate * multiplicatorLemn;
             base.AdaugaLemn(lemnAdaugat);
        }
       
    }
}
