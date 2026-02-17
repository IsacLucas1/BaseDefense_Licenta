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

    [Header("ButoaneStart")] public Button btnStartHost;
    public Button btnStartClient;

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
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            
            var spawner = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerSpawner>();

            if (spawner != null)
            {
                spawner.SpawneazaJucator(index);
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
