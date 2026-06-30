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
    public NetworkVariable<int> jucatoriDeAsteptat = new NetworkVariable<int>(0);
    
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
        
        // Activeaza baza inamica si porneste rutina de pregatire pentru atacul final
        if (bazaInamica != null)
        {
            bazaInamica.SetActive(true);
            ActiveazaBazaClientRpc();
        }
        
        if (BazaInamicaManager.Instance != null)
        {
            // Trezește inamicii din baza inamică și îi face să apară în interiorul bazei
            BazaInamicaManager.Instance.AparitieInamiciInBaza();
        }
        
        DayNightManager dayNightManager = FindFirstObjectByType<DayNightManager>();

        if (dayNightManager != null)
        {
            StartCoroutine(RutinaPregatireAtacFinal(dayNightManager));
        }
    }
    
    private IEnumerator RutinaPregatireAtacFinal(DayNightManager dayNightManager)
    {
        while (dayNightManager.EsteNoapte)
        {
            yield return new WaitForSeconds(1f);
        }
        
        // Setează timpul de atac final pe server și așteaptă până când timpul este oprit pentru asediu
        dayNightManager.SeteazaTimpAtacFinalServerRpc();

        yield return new WaitUntil(() => dayNightManager.timpOpritPentruAsediu.Value);
        dayNightManager.timpOpritPentruAsediu.Value = true;
        // Porneste furtuna si activeaza zonele ready pentru jucatori
        PornesteDeathStormClientRpc();
        ActiveazaZoneReadyClientRpc();
        
        if (DeathZoneManager.Instance != null)
        {
            DeathZoneManager.Instance.PornesteFurtuna();
        }
        RecalculeazaReady();
    }
    
    [ClientRpc]
    private void ActiveazaBazaClientRpc()
    {
        if (bazaInamica != null)
        {
            bazaInamica.SetActive(true);
        }
    }
    
    // 
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

    // Primeste notificare de la client ca jucatorul este gata pentru atacul final
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NumaraJucatoriReadyPentruAtacFinalServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (jucatoriCareAuDatReady.Contains(clientId))
        {
            return;
        }
        jucatoriCareAuDatReady.Add(clientId);
        RecalculeazaReady();
        VerificaConditieStartAsediuFinal();
    }
    
    // Recalculeaza numarul de jucatori care sunt gata pentru atacul final
    private void RecalculeazaReady()
    {
        if (!IsServer)
        {
            return;
        }

        int vii = 0;
        int viiReady = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }
            BasePlayer bp = client.PlayerObject.GetComponent<BasePlayer>();
            if (bp == null || bp.isDead.Value)
            {
                continue;
            }

            vii++;
            if (jucatoriCareAuDatReady.Contains(client.ClientId))
            {
                viiReady++;
            }
        }

        jucatoriDeAsteptat.Value = vii;
        jucatoriReady.Value = viiReady;
    }
    
    // Verifica daca toti jucatorii sunt gata pentru atacul final si daca da, declanseaza atacul
    private void VerificaConditieStartAsediuFinal()
    {
        if (!IsServer || aInceputAtacul.Value)
        {
            return;
        }

        if (jucatoriDeAsteptat.Value > 0 && jucatoriReady.Value >= jucatoriDeAsteptat.Value)
        {
            IncepeAsediulEfectiv();
        }
    }
    
    // Notifica serverul ca un jucator a murit si recalculaza numarul de jucatori vii si gata pentru atacul final
    public void JucatorAMurit()
    {
        if (!IsServer) return;

        RecalculeazaReady();
        VerificaInfrangere();    
        VerificaConditieStartAsediuFinal();   
    }
    
    public void TrimiteReadySpreServer()
    {
        NumaraJucatoriReadyPentruAtacFinalServerRpc();
    }
    
    // Se declanseaza cand au dat toti ready
    private void IncepeAsediulEfectiv()
    {
        if (aInceputAtacul.Value)
        {
            return;
        }

        aInceputAtacul.Value = true;
        AscundeZoneReadyClientRpc();

        StartCoroutine(RutinaStartAtacFinal());
    }

    // Rutina care se ocupa de inceperea atacului final, trezeste inamicii
    // exteriori si deblocheaza miscarea jucatorilor dupa un delay
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
    private void AscundeZoneReadyClientRpc()
    {
        if (containerZoneReady != null)
        {
            containerZoneReady.SetActive(false);
        }
    }
    
    // Obtine playerul local, daca exista
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
        if (!IsServer || jocTerminat)
        {
            return;
        }
        jocTerminat = true;
        AfiseazaFinalClientRpc(true);
    }

    public void Infrangere()
    {
        if (!IsServer || jocTerminat)
        {
            return;
        }
        jocTerminat = true;
        AfiseazaFinalClientRpc(false);
    }

    // Verifica daca toti jucatorii sunt morti si daca da, declanseaza infrangerea
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
