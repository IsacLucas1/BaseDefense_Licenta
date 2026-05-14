using UnityEngine;

public class ZonaMagazin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BasePlayer player = other.GetComponent<BasePlayer>();
        
        if (player != null && player.IsOwner)
        {
            player.SeteazaInZonaMagazin(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BasePlayer player = other.GetComponent<BasePlayer>();
        
        if (player != null && player.IsOwner)
        {
            player.SeteazaInZonaMagazin(false);
            
            if (UIManager.Instance != null && UIManager.Instance.ShopPanel != null && UIManager.Instance.ShopPanel.activeSelf)
            {
                UIManager.Instance.ArataMagazin(false);
            }
        }
    }
}
