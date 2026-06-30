using UnityEngine;

public class ButonPoarta : MonoBehaviour
{
    public Poarta poarta;
    
    // Metoda din ButonPoarta care este apelata cand jucatorul incearca sa actioneze poarta
    public void IncearcaActionare(ConstructorPlayer jucator)
    {
        if (poarta != null)
        {
            poarta.RequestToggleGateServerRpc();
        }
    }
}
