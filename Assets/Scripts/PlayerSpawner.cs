using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("ListaPersonaje")]
    public GameObject[] playerPrefabs;
    
    public void SpawneazaJucator(int indexClasa, Vector3 spawnPosition, Quaternion rotation)
    {
        if (IsOwner)
        {
            SpawnCharacterServerRPC(indexClasa, OwnerClientId, spawnPosition, rotation);
        }
    }

    [ServerRpc]
    private void SpawnCharacterServerRPC(int index, ulong clientId, Vector3 spawnPosition, Quaternion rotation)
    {
        if(index >= 0 && index < playerPrefabs.Length)
        {
            GameObject playerInstance = Instantiate(playerPrefabs[index], spawnPosition, rotation);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            BasePlayer basePlayer = playerInstance.GetComponent<BasePlayer>();
            if (basePlayer != null)
            {
                basePlayer.SetSpawnPoint(spawnPosition, rotation);
            }
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
