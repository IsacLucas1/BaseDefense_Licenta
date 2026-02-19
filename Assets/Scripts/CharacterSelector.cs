using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterSelector : NetworkBehaviour
{
    [Header("Referinte UI")] public GameObject characterSelectionUI;
    public GameObject StartPanel;
    public GameObject ClasePanel;
    public Button btnTank;
    public Button btnSpion;
    public Button btnConstructor;
    public Button btnMedic;
    public Button btnArcas;
    public Camera lobbyCamera;

    [Header("ButoaneStart")]
    public Button btnStartHost;
    public Button btnStartClient;
    
    [Header("LocatiiSpawn")]
    public Transform[] spawnPoints;

    private bool aDatClick = false;
    private void Start()
    {
        if (lobbyCamera != null)
        {
            lobbyCamera.gameObject.SetActive(true);
        }

        if (StartPanel != null)
        {
            StartPanel.SetActive(true);
        }

        if (ClasePanel != null)
        {
            ClasePanel.SetActive(false);
        }

        btnTank.onClick.AddListener(() => ComandaSpawn(0));
        btnSpion.onClick.AddListener(() => ComandaSpawn(1));
        btnConstructor.onClick.AddListener(() => ComandaSpawn(2));
        btnMedic.onClick.AddListener(() => ComandaSpawn(3));
        btnArcas.onClick.AddListener(() => ComandaSpawn(4));

        btnStartHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            SchimbaMeniuClase();
        });
        btnStartClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            SchimbaMeniuClase();
        });
    }

    void SchimbaMeniuClase()
    {
        if (StartPanel != null)
        {
            StartPanel.SetActive(false);
        }

        if (ClasePanel != null)
        {
            ClasePanel.SetActive(true);
        }
    }

    public void ComandaSpawn(int index)
    {
        if (aDatClick)
        {
            return;
        }
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            
            var spawner = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerSpawner>();

            if (spawner != null)
            {
                Vector3 pozitie = Vector3.zero + Vector3.up *2;
                Quaternion rotatie = Quaternion.identity;
                aDatClick = true;
                
                if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
                {
                    pozitie = spawnPoints[index].position;
                    rotatie = spawnPoints[index].rotation;
                }
                spawner.SpawneazaJucator(index, pozitie, rotatie);
                StartCoroutine(AscundeTot());
            }
        }
    }

    IEnumerator AscundeTot()
    {
        yield return new WaitForSeconds(0.1f);

        if (characterSelectionUI != null)
        {
            characterSelectionUI.SetActive(false);
        }

        if (lobbyCamera != null)
        {
            lobbyCamera.gameObject.SetActive(false);
        }
    }
}
