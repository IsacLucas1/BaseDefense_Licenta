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
    
    // Clase ocupate (pentru dezactivarea butoanelor)
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
    
    private Dictionary<ulong, int> alegeri = new Dictionary<ulong, int>();
    private bool aTrecutLaClase = false;
    
    private void Awake()
    {
        Instance = this;
    }
    
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
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AlegeClasaServerRpc(int indexClasa, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        // Dacă a ales deja, ignorăm
        if (alegeri.ContainsKey(clientId))
        {
            return;
        }
        
        // Dacă clasa e luată, ignorăm
        if (indexClasa == 0 && tankOcupat.Value) return;
        if (indexClasa == 1 && spionOcupat.Value) return;
        if (indexClasa == 2 && constructorOcupat.Value) return;
        if (indexClasa == 3 && medicOcupat.Value) return;
        if (indexClasa == 4 && arcasOcupat.Value) return;
        
        // Marcăm clasa ocupată
        if (indexClasa == 0) tankOcupat.Value = true;
        if (indexClasa == 1) spionOcupat.Value = true;
        if (indexClasa == 2) constructorOcupat.Value = true;
        if (indexClasa == 3) medicOcupat.Value = true;
        if (indexClasa == 4) arcasOcupat.Value = true;
        
        alegeri[clientId] = indexClasa;
        // Dacă toți au ales, spawnăm toți jucătorii!
        if (alegeri.Count >= nrMaxJucatori.Value)
        {
            SpawneazaToti();
        }
    }
    
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
        // Găsim CharacterSelector-ul și ascundem orice meniu activ
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
    
    private IEnumerator SchimbaMeniuClaseDelay()
    {
        ActualizeazaNumaratoareClientRpc("Toți jucătorii s-au conectat!");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("3");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("2");
        yield return new WaitForSeconds(1f);
        ActualizeazaNumaratoareClientRpc("1");
        yield return new WaitForSeconds(1f);
        SchimbaMeniuClaseClientRpc();
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
