using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("Interfata Jucator")]
    public GameObject InterfataJucator;
    
    [Header("UI Elements")]
    public TMP_Text textLemn;
    public TMP_Text textViteza;
    public Slider sliderViata;
    public TMP_Text textViata;

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
    
    public void ActualizeazaViteza(float coeficientViteza)
    {
        if (textViteza != null)
        {
            textViteza.text = "Viteza: " +  coeficientViteza.ToString("F2");
        }
    }
    
    public void ActualizeazaViata(int valoareCurenta, int valoareMaxima)
    {
        if (sliderViata != null)
        {
            sliderViata.maxValue = valoareMaxima;
            sliderViata.value = valoareCurenta;
        }

        if (textViata != null)
        {
            textViata.text = valoareCurenta + " / " + valoareMaxima;
        }
    }
}
