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
    public static RelayManager Instance { get; private set; } // Singleton pentru a putea fi accesat din alte scripturi
    public int nrMaxDeJucatoriAlesDeHost { get; private set; } // Numarul de jucatori setat de host (1-5)

    [Header("UI Elements")]
    public TMP_InputField joinInput; 
    public TMP_Text codeDisplayText; 
    public TMP_Dropdown nrMaxJucatoriDropdown; 
    public TMP_Text mesajEroare; 

    private Coroutine corutinaEroare;
    private void Awake()
    {
        // Singleton pattern pentru a ne asigura ca exista o singura instanta a RelayManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    async void Start()
    {
        // Abonare la evenimentul de deconectare a clientului
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
        
        // Initializare servicii Unity si autentificare anonima
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    
    // Dezabonare de la evenimentul de deconectare a clientului la distrugerea obiectului
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    // Crearea unei camere de joc. Se creeaza o alocare pe Unity Relay si porneste serverul
    public async void CreateGame()
    {
        // Preia numarul maxim de jucatori ales de host din dropdown
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
            // Cere o alocare de la Unity Relay pentru numarul de jucatori ales
            int locuriDeCerut = Mathf.Max(1, nrMaxDeJucatoriAlesDeHost - 1);
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(locuriDeCerut);
            // Generarea codului de acces
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Cod generat: " + joinCode);
           
            if (codeDisplayText != null)
            {
                codeDisplayText.text = "Cod camera: " + joinCode;
            }
           
            joinInput.text = joinCode;
            
            // Configurarea conexiunii pentru host 
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );
            
            // Functie de verificare a conxiunii
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            // Porneste host-ul
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            AfiseazaEroare("Eroare la crearea camerei. Incearca din nou.");
            RevinoLaStart();
        }
    }
    
    // Alaturarea la o camera de joc existenta folosind codul de acces
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
            // Cere Unity Relay sa conecteze la meci folosind codul de acces 
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            
            // Configurarea conexiunii pentru client 
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            // Se asigura ca este abonat la deconectare exact acum
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            // Porneste clientul
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            AfiseazaEroare("Cod gresit! Incearca din nou.");
            RevinoLaStart();
        }
    }
    
    // Metoda de verificare a conexiunii pentru host.
    // Se verifica daca numarul de jucatori conectati depaseste limita setata de host
    // Daca da, respinge conexiunea si trimite un mesaj de eroare 
    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int conectatiAcum = NetworkManager.Singleton.ConnectedClientsList.Count;
        
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

    // Gestionarea deconectarii clientului.
    // Daca clientul este respins, afiseaza un mesaj de eroare si revine la ecranul de start
    private void OnClientDisconnect(ulong clientId)
    {
        // Clientul isi trateaza propria deconectare, nu serverul
        if (NetworkManager.Singleton.IsServer)
        {
            return;
        }

        string motiv = NetworkManager.Singleton.DisconnectReason;
        
        if (string.IsNullOrEmpty(motiv))
        {
            motiv = "Nu te-ai putut conecta la camera.";
        }

        AfiseazaEroare(motiv);
        RevinoLaStart();
    }
    
    
    // Revine la ecranul de start al jocului
    private void RevinoLaStart()
    {
        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            cs.RevinoLaStart();
        }
    }
    
    // Afiseaza un mesaj de eroare pe ecran pentru o perioada scurta de timp
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

    // Coroutine care afiseaza mesajul de eroare pentru 5 secunde
    private IEnumerator EroareRoutine(string mesaj)
    {
        mesajEroare.text = mesaj;
        yield return new WaitForSeconds(5f);
        mesajEroare.text = "";
    }
}