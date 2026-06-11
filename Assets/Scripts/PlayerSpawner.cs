using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("ListaPersonaje")]
    public GameObject[] playerPrefabs;
    
    public void SpawnDinServer(int index, ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }
        
        if (index >= 0 && index < playerPrefabs.Length)
        {
            // Folosim spawnPoints din CharacterSelector
            Vector3 pozitie = Vector3.zero + Vector3.up * 2;
            Quaternion rotatie = Quaternion.identity;
            CharacterSelector cs = FindFirstObjectByType<CharacterSelector>();
            
            if (cs != null && cs.spawnPoints != null && index < cs.spawnPoints.Length && cs.spawnPoints[index] != null)
            {
                pozitie = cs.spawnPoints[index].position;
                rotatie = cs.spawnPoints[index].rotation;
            }
            
            GameObject playerInstance = Instantiate(playerPrefabs[index], pozitie, rotatie);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            BasePlayer basePlayer = playerInstance.GetComponent<BasePlayer>();
            
            if (basePlayer != null)
            {
                basePlayer.SetSpawnPoint(pozitie, rotatie);
            }
            
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
