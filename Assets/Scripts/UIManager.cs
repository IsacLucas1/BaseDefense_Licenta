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
    public TMP_Text textBani;
    public TMP_Text textDamage;
    public Slider sliderViata;
    public TMP_Text textViata;
    
    [Header("Sistem Vot War Room")]
    public GameObject WarRoomVotingPanel;
    public TMPro.TextMeshProUGUI textTimpVot;
    public TMPro.TextMeshProUGUI textScorVot;
    public GameObject butonDa;
    public GameObject butonNu;
    
    [Header("Abilitati Cooldown")]
    public Image imagineCooldownTaunt;
    public Image imagineCooldownInvizibilitate;
    public Image imagineBuffViteza;
    public Image imagineBuffDamage;
    
    
    
    public bool jocPauza = false;

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
            textLemn.text = "<sprite=0>: " + cantitate;
        }
    }
    
    public void ActualizeazaViteza(float coeficientViteza)
    {
        if (textViteza != null)
        {
            textViteza.text = "<sprite=0>: " +  coeficientViteza.ToString("F2");
        }
    }
    
    public void ActualizeazaBani(int cantitate)
    {
        if (textBani != null)
        {
            textBani.text = "<sprite=0>: " + cantitate;
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
            textViata.text ="<sprite=0> " + valoareCurenta + " / " + valoareMaxima;
        }
    }
    
    public void ActualizeazaDamage(int valoareDamage)
    {
        if (textDamage != null)
        {
            textDamage.text = "<sprite=0>: " + valoareDamage;
        }
    }
    
    public void ArataPanouVot(bool activeaza)
    {
        if (WarRoomVotingPanel != null)
        {
            WarRoomVotingPanel.SetActive(activeaza);

            if (activeaza)
            {
                jocPauza = true;
                
                if (WarRoomManager.Instance != null)
                {
                    ActualizeazaTextTimpVot(WarRoomManager.Instance.durataVot);
                }
                ActualizeazaScorVot(0,0);
                
                if (butonDa != null)
                {
                    butonDa.SetActive(true);
                }
                if (butonNu != null)
                {
                    butonNu.SetActive(true);
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                StartCoroutine(DeblocheazaJocDupaVot());
            }
        }
    }
    
    public void ActualizeazaTextTimpVot(float timp)
    {
        if (textTimpVot != null)
        {
            textTimpVot.text = "Timp Rămas: " + timp.ToString("0") + "s";
        }
    }

    public void ActualizeazaScorVot(int da, int nu)
    {
        if (textScorVot != null)
        {
            textScorVot.text = "DA: " + da + " / 3  |  NU: " + nu;
        }
    }
    
    public void ButonVotDa()
    {
        WarRoomManager.Instance.InregistreazaVotServerRpc(true);
        AscundeButonVoturi();
    }

    public void ButonVotNu()
    {
        WarRoomManager.Instance.InregistreazaVotServerRpc(false);
        AscundeButonVoturi();
    }
    
    private void AscundeButonVoturi()
    {
        if (butonDa != null)
        {
            butonDa.SetActive(false);
        }

        if (butonNu != null)
        {
            butonNu.SetActive(false);
        }
    }
    
    private System.Collections.IEnumerator DeblocheazaJocDupaVot()
    {
        yield return new WaitForSeconds(0.1f);
        jocPauza = false;
    }
    
    public void SeteazaVizibilitateTaunt(bool vizibil)
    {
        if (imagineCooldownTaunt != null)
        {
            imagineCooldownTaunt.gameObject.SetActive(vizibil);
        }
    }
    
    public void ActualizeazaCooldownTaunt(float procentaj)
    {
        if (imagineCooldownTaunt != null)
        {
            imagineCooldownTaunt.fillAmount = procentaj;
        }
    }
    
    public void SeteazaVizibilitateInvizibilitate(bool vizibil)
    {
        if (imagineCooldownInvizibilitate != null)
        {
            imagineCooldownInvizibilitate.gameObject.SetActive(vizibil);
        }
    }
    
    public void ActualizeazaCooldownInvizibilitate(float procentaj)
    {
        if (imagineCooldownInvizibilitate != null)
        {
            imagineCooldownInvizibilitate.fillAmount = procentaj;
        }
    }
    
    public void SeteazaVizibilitateBuffViteza(bool vizibil)
    {
        if (imagineBuffViteza != null)
        {
            imagineBuffViteza.gameObject.SetActive(vizibil);
        }
    }

    public void ActualizeazaBuffViteza(float procentaj)
    {
        if (imagineBuffViteza != null)
        {
            imagineBuffViteza.fillAmount = procentaj;
        }
    }

    public void SeteazaVizibilitateBuffDamage(bool vizibil)
    {
        if (imagineBuffDamage != null)
        {
            imagineBuffDamage.gameObject.SetActive(vizibil);
        }
    }

    public void ActualizeazaBuffDamage(float procentaj)
    {
        if (imagineBuffDamage != null)
        {
            imagineBuffDamage.fillAmount = procentaj;
        }
    }
    
}
