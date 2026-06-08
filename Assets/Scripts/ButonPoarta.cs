using UnityEngine;

public class ButonPoarta : MonoBehaviour
{
    public Poarta poarta;
    
    public void IncearcaActionare(ConstructorPlayer jucator)
    {
        if (poarta != null)
        {
            poarta.RequestToggleGateServerRpc();
        }
    }
}
