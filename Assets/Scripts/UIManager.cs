using UnityEngine;
using TMPro;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("Interfata Jucator")]
    public GameObject InterfataJucator;
    
    [Header("UI Elements")]
    public TMP_Text textLemn;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        
        if (InterfataJucator != null)
        {
            InterfataJucator.SetActive(false);
        }
    }
    
    public void ActiveazaInterfataJucator()
    {
        if (InterfataJucator != null)
        {
            InterfataJucator.SetActive(true);
        }
    }
    
    public void ActualizeazaLemn(int cantitate)
    {
        if (textLemn != null)
        {
            textLemn.text = "Lemn: " + cantitate;
        }
    }
}
