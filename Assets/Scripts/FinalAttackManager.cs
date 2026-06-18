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
    
    [Header("Zone Ready (0=Tank,1=Spion,2=Constructor,3=Medic,4=Arcas)")]
    public GameObject containerZoneReady;
    public ReadyZonaClasa[] zoneReady;
    
    [Header("Stare Atac Final")]
    public NetworkVariable<bool> atacFinalDeclansat = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> aInceputAtacul = new NetworkVariable<bool>(false);
    
    public NetworkVariable<int> jucatoriReady = new NetworkVariable<int>(0);
    private List<ulong> jucatoriCareAuDatReady = new List<ulong>();
    
    private bool jocTerminat = false;
    
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
        
        if (BazaInamicaManager.Instance != null)
        {
            BazaInamicaManager.Instance.AparitieInamiciInBaza();
        }
        
        DayNightManager dayNightManager = FindFirstObjectByType<DayNightManager>();

        if (dayNightManager != null)
        {
            StartCoroutine(RutinaPregatireAtacFinal(dayNightManager));
        }
        
        Debug.Log("Faza finala a fost declansata!");
    }
    
    private IEnumerator RutinaPregatireAtacFinal(DayNightManager dayNightManager)
    {
        while (dayNightManager.EsteNoapte)
        {
            yield return new WaitForSeconds(1f);
        }
        
        dayNightManager.SeteazaTimpAtacFinalServerRpc();

        yield return new WaitUntil(() => dayNightManager.timpOpritPentruAsediu.Value);
        dayNightManager.timpOpritPentruAsediu.Value = true;
        PornesteDeathStormClientRpc();
        ActiveazaZoneReadyClientRpc();
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
    private void ActiveazaZoneReadyClientRpc()
    {
        BasePlayer player = ObtinePlayerLocal();
        if (player == null)
        {
            return;
        }

        int clasaLocala = GetIndexClasa(player);
        if (zoneReady == null)
        {
            return;
        }

        if (containerZoneReady != null)
        {
            containerZoneReady.SetActive(true);
        }

        foreach (var zona in zoneReady)
        {
            if (zona != null)
            {
                zona.gameObject.SetActive(zona.indexClasa == clasaLocala);
            }
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NumaraJucatoriReadyPentruAtacFinalServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (jucatoriCareAuDatReady.Contains(clientId))
        {
            return;
        }
        jucatoriCareAuDatReady.Add(clientId);
        jucatoriReady.Value = jucatoriCareAuDatReady.Count;
        
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
        if (aInceputAtacul.Value)
        {
            return;
        }

        aInceputAtacul.Value = true;
        Debug.Log("Toti jucatorii sunt gata! Incepe asediul final!");
        DeclanseazaCinematicClientRpc();
        AscundeZoneReadyClientRpc();

        StartCoroutine(RutinaStartAtacFinal());
    }

    private IEnumerator RutinaStartAtacFinal()
    {
        if (BazaInamicaManager.Instance != null)
        {
            BazaInamicaManager.Instance.AparitieInamiciExteriori();
        }
        
        AfiseazaMesajStartAtacClientRpc();
        
        yield return new WaitForSeconds(3f);
        
        AscundeMesajStartAtacClientRpc();

        if (BazaInamicaManager.Instance != null)
        {
            BazaInamicaManager.Instance.TrezesteExteriori();
        }
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                BasePlayer bp = client.PlayerObject.GetComponent<BasePlayer>();
                if (bp != null)
                {
                    bp.DeblocheazaMiscarea();
                }
            }
        }
    }
    
    [ClientRpc]
    private void AfiseazaMesajStartAtacClientRpc()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AfiseazaMesajStartAtac("Acum incepe Atacul Final!");
        }
    }

    [ClientRpc]
    private void AscundeMesajStartAtacClientRpc()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AscundeMesajStartAtac();
        }
    }
    
    [ClientRpc]
    private void DeclanseazaCinematicClientRpc()
    {
        // Aici vom declansa animatia sau un script de Camera Shake pe fiecare client
        Debug.Log("CINEMATIC: CAMERA SHAKE SI RIDICARE ZID");
    }
    
    [ClientRpc]
    private void AscundeZoneReadyClientRpc()
    {
        if (containerZoneReady != null)
        {
            containerZoneReady.SetActive(false);
        }
    }
    
    private BasePlayer ObtinePlayerLocal()
    {
        if (NetworkManager.Singleton == null)
        {
            return null;
        }
        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient == null || localClient.PlayerObject == null)
        {
            return null;
        }
        return localClient.PlayerObject.GetComponent<BasePlayer>();
    }

    public static int GetIndexClasa(BasePlayer p)
    {
        if (p is TankPlayer) return 0;
        if (p is SpionPlayer) return 1;
        if (p is ConstructorPlayer) return 2;
        if (p is MedicPlayer) return 3;
        if (p is ArcasPlayer) return 4;
        return -1;
    }

    public void Victorie()
    {
        if (!IsServer || jocTerminat) return;
        jocTerminat = true;
        Debug.Log("VICTORIE!");
        AfiseazaFinalClientRpc(true);
    }

    public void Infrangere()
    {
        if (!IsServer || jocTerminat) return;
        jocTerminat = true;
        Debug.Log("INFRANGERE!");
        AfiseazaFinalClientRpc(false);
    }

    public void VerificaInfrangere()
    {
        if (!IsServer || jocTerminat)
        {
            return;
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                BasePlayer bp = client.PlayerObject.GetComponent<BasePlayer>();
                if (bp != null && !bp.isDead.Value)
                {
                    return; 
                }
            }
        }
        
        Infrangere();
    }

    [ClientRpc]
    private void AfiseazaFinalClientRpc(bool victorie)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ArataEcranFinal(victorie);
        }
    }
}
