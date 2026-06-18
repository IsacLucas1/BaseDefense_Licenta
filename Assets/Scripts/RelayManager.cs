using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Collections;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; } 
    public int nrMaxDeJucatoriAlesDeHost { get; private set; } 

    [Header("UI Elements")]
    public TMP_InputField joinInput;
    public TMP_Text codeDisplayText;
    public TMP_Dropdown nrMaxJucatoriDropdown;
    public TMP_Text mesajEroare;

    private Coroutine corutinaEroare;
    private void Awake()
    {
        // Păstrăm scriptul activ ca să poată fi citit din Scena de Joc
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    async void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
        
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    public async void CreateGame()
    {
        if (nrMaxJucatoriDropdown != null)
        {
            nrMaxDeJucatoriAlesDeHost = nrMaxJucatoriDropdown.value + 1;
        }
        else
        {
            nrMaxDeJucatoriAlesDeHost = 5; // Default 
        }

        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            cs.SchimbaPanelAsteptare();
        }
        
        try
        {
            int locuriDeCerut = Mathf.Max(1, nrMaxDeJucatoriAlesDeHost - 1);
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(locuriDeCerut);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Cod generat: " + joinCode);
           
            if (codeDisplayText != null)
            {
                codeDisplayText.text = "Cod camera: " + joinCode;
            }
           
            joinInput.text = joinCode;
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Eroare la Create: " + e);
            AfiseazaEroare("Eroare la crearea camerei. Incearca din nou.");
            RevinoLaStart();
        }
    }
    
    public async void JoinGame()
    {
        string code = joinInput.text;
        
        if (codeDisplayText != null)
        {
            codeDisplayText.text = "Cod camera: " + code;
        }

        if (string.IsNullOrEmpty(code))
        {
            AfiseazaEroare("Nu ai introdus niciun cod!");
            RevinoLaStart();
            return;
        }

        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            cs.SchimbaPanelAsteptare();
        }
        
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            // Ne asiguram ca suntem abonati la deconectare exact acum
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Cod greșit sau eroare: " + e);
            AfiseazaEroare("Cod gresit! Incearca din nou.");
            RevinoLaStart();
        }
    }
    
    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int conectatiAcum = NetworkManager.Singleton.ConnectedClientsList.Count;
        
        Debug.Log($"[Approval] ruleaza. conectati={conectatiAcum}, max={nrMaxDeJucatoriAlesDeHost}");

        if (conectatiAcum >= nrMaxDeJucatoriAlesDeHost)
        {
            response.Approved = false;
            response.Reason = "Camera este plina!";
            response.CreatePlayerObject = false;
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // Doar clientul respins isi trateaza propria deconectare
        if (NetworkManager.Singleton.IsServer)
        {
            return;
        }

        string motiv = NetworkManager.Singleton.DisconnectReason;
        Debug.Log($"[Disconnect] deconectat. motiv='{motiv}'");
        
        if (string.IsNullOrEmpty(motiv))
        {
            motiv = "Nu te-ai putut conecta la camera.";
        }

        AfiseazaEroare(motiv);
        RevinoLaStart();
    }
    
    private void RevinoLaStart()
    {
        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            cs.RevinoLaStart();
        }
    }
    
    private void AfiseazaEroare(string mesaj)
    {
        if (mesajEroare == null)
        {
            return;
        }

        if (corutinaEroare != null)
        {
            StopCoroutine(corutinaEroare);
        }
        corutinaEroare = StartCoroutine(EroareRoutine(mesaj));
    }

    private IEnumerator EroareRoutine(string mesaj)
    {
        mesajEroare.text = mesaj;
        yield return new WaitForSeconds(5f);
        mesajEroare.text = "";
    }
}