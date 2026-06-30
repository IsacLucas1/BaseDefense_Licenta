using UnityEngine;

public class MedicAnimReceiver : MonoBehaviour
{
    private MedicPlayer medicPlayer;

    void Start()
    {
        medicPlayer = GetComponentInParent<MedicPlayer>();
    }

    public void TriggerTragere()   
    {
        if (medicPlayer != null)
        {
            medicPlayer.ExecutaRazaDinAnimatie();
        }
    }
}