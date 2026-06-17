using UnityEngine;
using Unity.Netcode;

public class ReadyCheckZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsClient)
        {
            return;
        }
        // Ne asigurăm că doar jucătorul LOCAL primește meniul
        if (other.CompareTag("Player"))
        {
            BasePlayer jucator = other.GetComponent<BasePlayer>();
            if (jucator != null && jucator.IsOwner)
            {
                if (UIManager.Instance != null)
                {
                    // Vom crea această funcție în UIManager
                    UIManager.Instance.ArataPanouReadyCheck(true);
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!IsClient)
        {
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            BasePlayer jucator = other.GetComponent<BasePlayer>();
            if (jucator != null && jucator.IsOwner)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ArataPanouReadyCheck(false);
                }
            }
        }
    }
}
