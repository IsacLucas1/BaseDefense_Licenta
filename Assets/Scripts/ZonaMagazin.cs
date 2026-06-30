using UnityEngine;

public class ZonaMagazin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BasePlayer player = other.GetComponent<BasePlayer>();
        
        // Se asigura ca deschide meniul pentru proprietarul obiectului (jucatorul care a intrat in zona)
        if (player != null && player.IsOwner)
        {
            player.SeteazaInZonaMagazin(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BasePlayer player = other.GetComponent<BasePlayer>();
        
        // Se asigura ca inchide meniul pentru proprietarul obiectului (jucatorul care a iesit din zona)
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
