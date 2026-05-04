using UnityEngine;

public class ArcasAnimReceiver : MonoBehaviour
{
    private ArcasPlayer playerScript;

    void Start()
    {
        playerScript = GetComponentInParent<ArcasPlayer>();
    }

    public void TriggerTragere()
    {
        if (playerScript != null)
        {
            playerScript.ExecutaTragereDinAnimatie();
        }
    }
}
