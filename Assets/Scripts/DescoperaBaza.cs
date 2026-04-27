using Unity.Netcode;
using UnityEngine;

public class DescoperireBaza : NetworkBehaviour
{
    private bool bazaGasita = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || bazaGasita)
        {
            return;
        }

        BasePlayer jucator = other.GetComponent<BasePlayer>();
        if (jucator != null)
        {
            bazaGasita = true;
            Debug.Log("Baza inamică a fost descoperită!");
            
            if (WarRoomManager.Instance != null)
            {
                WarRoomManager.Instance.ActiveazaButon();
            }
        }
    }
}