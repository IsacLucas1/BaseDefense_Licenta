using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; } 
    public int nrMaxDeJucatoriAlesDeHost { get; private set; } 

    [Header("UI Elements")]
    public TMP_InputField joinInput;
    public TMP_Text codeDisplayText;
    public TMP_Dropdown nrMaxJucatoriDropdown;

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
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
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
            
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Eroare la Create: " + e);
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
            Debug.LogError("Nu ai introdus niciun cod!");
            return;
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
            
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Cod greșit sau eroare: " + e);
        }
    }
}