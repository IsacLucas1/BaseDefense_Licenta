using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class GameSessionManager : NetworkBehaviour
{
    public static GameSessionManager Instance { get; private set; }
    
    public NetworkVariable<int> nrMaxJucatori = new NetworkVariable<int>(
        1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<int> jucatoriConectati = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    
    // Flag-uri care arata ce clase au fost deja ocupate (pentru dezactivarea butoanelor)
    public NetworkVariable<bool> tankOcupat = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> spionOcupat = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> constructorOcupat = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> medicOcupat = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> arcasOcupat = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);
    
    // Dictionar care tine evidenta clasei alese de fiecare jucator (clientId -> indexClasa)
    private Dictionary<ulong, int> alegeri = new Dictionary<ulong, int>();
    private bool aTrecutLaClase = false; // Flag care indica daca s-a trecut la meniul de alegere a claselor
    
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
    
    // Preia numarul maxim de jucatori setat de host si il seteaza in NetworkVariable folosind informatia din RelayManager
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (IsServer)
            {
                // Preia numărul setat din Dropdown
                if (RelayManager.Instance != null)
                {
                    nrMaxJucatori.Value = RelayManager.Instance.nrMaxDeJucatoriAlesDeHost;
                }
            }
        }
    }
    
    // Verifica daca s-au conectat destui jucatori pentru pornirea numaratoarei inverse
    private void Update()
    {
        if (IsServer && IsSpawned)
        {
            jucatoriConectati.Value = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (!aTrecutLaClase && jucatoriConectati.Value >= nrMaxJucatori.Value && nrMaxJucatori.Value > 0)
            {
                aTrecutLaClase = true;
                StartCoroutine(SchimbaMeniuClaseDelay());
            }
        }
    }
    
    // Coroutine care afiseaza un countdown de 3 secunde inainte de a schimba meniul la alegerea claselor
    private IEnumerator SchimbaMeniuClaseDelay()
    {
        ActualizeazaNumaratoareClientRpc("Toti jucatorii s-au conectat!");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("3");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("2");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("1");
        yield return new WaitForSeconds(1f);
        SchimbaMeniuClaseClientRpc();
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AlegeClasaServerRpc(int indexClasa, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        // Daca a ales deja, ignoram cererea pentru prevenirea spam-ului sau bug-urile
        if (alegeri.ContainsKey(clientId))
        {
            return;
        }
        
        // Daca clasa e luata, ignoram
        if (indexClasa == 0 && tankOcupat.Value) return;
        if (indexClasa == 1 && spionOcupat.Value) return;
        if (indexClasa == 2 && constructorOcupat.Value) return;
        if (indexClasa == 3 && medicOcupat.Value) return;
        if (indexClasa == 4 && arcasOcupat.Value) return;
        
        // Marcheaza clasa ocupata si salveaza alegerea. Dezactiveaza butonul pentru ceilalti
        if (indexClasa == 0) tankOcupat.Value = true;
        if (indexClasa == 1) spionOcupat.Value = true;
        if (indexClasa == 2) constructorOcupat.Value = true;
        if (indexClasa == 3) medicOcupat.Value = true;
        if (indexClasa == 4) arcasOcupat.Value = true;
        
        // Salveaza alegerea jucatorului in dictionar
        alegeri[clientId] = indexClasa;
        // Dacă toti au ales, spawneaza toti jucatorii
        if (alegeri.Count >= nrMaxJucatori.Value)
        {
            SpawneazaToti();
        }
    }
    
    // Itereaza prin toti clientii din dictionar și foloseste scriptul PlayerSpawner pentru a instantia jucatorul
    // cu clasa aleasa. Apoi trimite un ClientRpc pentru a ascunde meniul de alegere a clasei la toti jucatorii.
    private void SpawneazaToti()
    {
        foreach (var pereche in alegeri)
        {
            ulong clientId = pereche.Key;
            int clasa = pereche.Value;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var spawner = client.PlayerObject.GetComponent<PlayerSpawner>();
                if (spawner != null)
                {
                    spawner.SpawnDinServer(clasa, clientId);
                }
            }
        }
        // Spunem tuturor să ascundă meniul
        AscundeMeniuClientRpc();
    }
    
    [ClientRpc]
    private void AscundeMeniuClientRpc()
    {
        // Gasim CharacterSelector-ul si ascundem orice meniu activ
        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            if (cs.characterSelectionUI != null)
            {
                cs.characterSelectionUI.SetActive(false);
            }
            
            if (cs.lobbyCamera != null)
            {
                cs.lobbyCamera.gameObject.SetActive(false);
            }
        }
    }
    
    [ClientRpc]
    private void ActualizeazaNumaratoareClientRpc(string text)
    {
        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        cs.numaratoareInversaPornita = true;
        if (cs != null && cs.textJucatoriConectati != null)
        {
            cs.textJucatoriConectati.text = text;
        }
    }
    
    [ClientRpc]
    private void SchimbaMeniuClaseClientRpc()
    {
        CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
        if (cs != null)
        {
            cs.ActiveazaMeniuClase();
        }
    }
}
