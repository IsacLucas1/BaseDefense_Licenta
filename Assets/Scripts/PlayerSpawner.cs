using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("ListaPersonaje")]
    public GameObject[] playerPrefabs;
    
    public void SpawneazaJucator(int indexClasa)
    {
        if (IsOwner)
        {
            SpawnCharacterServerRPC(indexClasa, OwnerClientId);
        }
    }

    [ServerRpc]
    private void SpawnCharacterServerRPC(int index, ulong clientId)
    {
        if(index >= 0 && index < playerPrefabs.Length)
        {
            GameObject playerInstance = Instantiate(playerPrefabs[index]);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
