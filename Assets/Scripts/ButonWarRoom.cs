using UnityEngine;

public class ButonWarRoom : MonoBehaviour
{
    public void ApasaButon()
    {
        if (WarRoomManager.Instance != null && WarRoomManager.Instance.butonActiv.Value)
        {
            WarRoomManager.Instance.IncepeVotul();
        }
    }
}