using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class FinalAttackManager : NetworkBehaviour
{
    public static FinalAttackManager Instance { get; private set; }

    [Header("Referinte")] 
    public GameObject bazaInamica;
    public GameObject deathZone;
    
    [Header("Stare Atac Final")]
    public NetworkVariable<bool> atacFinalDeclansat = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> aInceputAtacul = new NetworkVariable<bool>(false);
    
    public NetworkVariable<int> jucatoriReady = new NetworkVariable<int>(0);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void DeclanseazaFazaFinala()
    {
        if (!IsServer)
        {
            return;
        }

        atacFinalDeclansat.Value = true;
        
        if (bazaInamica != null)
        {
            bazaInamica.SetActive(true);
            ActiveazaBazaClientRpc();
        }
        
        DayNightManager dayNightManager = FindFirstObjectByType<DayNightManager>();

        if (dayNightManager != null)
        {
            if (dayNightManager.EsteZi)
            {
                dayNightManager.SeteazaTimpAtacFinalServerRpc();
                PornesteDeathStormClientRpc();
            }
            else
            {
                StartCoroutine(AsteaptaZiuaUrmatoare(dayNightManager));
            }
        }
        
        Debug.Log("Faza finala a fost declansata!");
    }
    
    [ClientRpc]
    private void ActiveazaBazaClientRpc()
    {
        if (bazaInamica != null)
        {
            bazaInamica.SetActive(true);
        }
    }

    [ClientRpc]
    private void PornesteDeathStormClientRpc()
    {
        if (deathZone != null)
        {
            deathZone.SetActive(true);
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ArataNotificare("Atentie! Timpul s-a oprit! Furtuna se apropie!");
        }
    }

    private IEnumerator AsteaptaZiuaUrmatoare(DayNightManager dayNightManager)
    {
        while (dayNightManager.EsteNoapte)
        {
            yield return new WaitForSeconds(1f);
        }

        dayNightManager.SeteazaTimpAtacFinalServerRpc();
        PornesteDeathStormClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NumaraJucatoriReadyPentruAtacFinalServerRpc()
    {
        jucatoriReady.Value++;

        int nrJucatoriConectati = GameSessionManager.Instance.jucatoriConectati.Value;
        
        if (jucatoriReady.Value >= nrJucatoriConectati)
        {
            IncepeAsediulEfectiv();
        }
    }
    
    public void TrimiteReadySpreServer()
    {
        NumaraJucatoriReadyPentruAtacFinalServerRpc();
    }
    
    private void IncepeAsediulEfectiv()
    {
        aInceputAtacul.Value = true;
        Debug.Log("Toti jucatorii sunt gata! Incepe asediul final!");
        DeclanseazaCinematicClientRpc();
    }
    
    [ClientRpc]
    private void DeclanseazaCinematicClientRpc()
    {
        // Aici vom declansa animatia sau un script de Camera Shake pe fiecare client
        Debug.Log("CINEMATIC: CAMERA SHAKE SI RIDICARE ZID");
    }
}
