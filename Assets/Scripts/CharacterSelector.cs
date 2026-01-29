using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterSelector : NetworkBehaviour
{
    [Header("Referinte UI")]
    public GameObject characterSelectionUI;
    public Button btnTank;
    public Button btnSpion;
    public Button btnConstructor;
    public Button btnMedic;
    public Button btnArcas;
    public Camera lobbyCamera;
    
    [Header("Referinte Prefab Personaje")]
    public GameObject tankPlayerPrefab;
    public GameObject spionPlayerPrefab;
    public GameObject constructorPlayerPrefab;
    public GameObject medicPlayerPrefab;
    public GameObject arcasPlayerPrefab;

    private void Start()
    {
        if(lobbyCamera != null) 
            lobbyCamera.gameObject.SetActive(true);
        
        btnTank.onClick.AddListener(() => RequestSpawnServerRpc(0));
        btnSpion.onClick.AddListener(() => RequestSpawnServerRpc(1));
        btnConstructor.onClick.AddListener(() => RequestSpawnServerRpc(2));
        btnMedic.onClick.AddListener(() => RequestSpawnServerRpc(3));
        btnArcas.onClick.AddListener(() => RequestSpawnServerRpc(4));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    [ServerRpc(RequireOwnership = false)]
    void RequestSpawnServerRpc(int choice, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        GameObject prefabToSpawn = null;
        
        switch (choice)
        {
            case 0: prefabToSpawn = tankPlayerPrefab; break;
            case 1: prefabToSpawn = spionPlayerPrefab; break;
            case 2: prefabToSpawn = constructorPlayerPrefab; break;
            case 3: prefabToSpawn = medicPlayerPrefab; break;
            case 4: prefabToSpawn = arcasPlayerPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            GameObject newPlayer = Instantiate(prefabToSpawn);
            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }
    public void HideMenu()
    { 
        if (lobbyCamera != null)
        {
            StartCoroutine(OpresteCamera());
        }
    }
    IEnumerator OpresteCamera()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.LocalClient.PlayerObject != null);
        yield return null;
        if (lobbyCamera != null)
        {
            lobbyCamera.gameObject.SetActive(false);
        }
        if (characterSelectionUI != null)
        {
            characterSelectionUI.SetActive(false);
        }
    }
}
