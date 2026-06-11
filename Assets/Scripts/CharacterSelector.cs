using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CharacterSelector : NetworkBehaviour
{
    [Header("Referinte UI")]
    public GameObject characterSelectionUI;
    public GameObject StartPanel;
    public GameObject ClasePanel;
    public GameObject AsteptarePanel;
    public Button btnTank;
    public Button btnSpion;
    public Button btnConstructor;
    public Button btnMedic;
    public Button btnArcas;
    public Camera lobbyCamera;

    [Header("ButoaneStart")]
    public Button btnStartHost;
    public Button btnStartClient;
    
    [Header("Texte Asteptare")]
    public TMP_Text textJucatoriConectati;
    
    [Header("LocatiiSpawn")]
    public Transform[] spawnPoints;

    private bool aDatClick = false;
    public bool numaratoareInversaPornita = false;
    
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
        
        if (AsteptarePanel != null)
        {
            AsteptarePanel.SetActive(false);
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
            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.CreateGame();
                SchimbaPanelAsteptare();
            }
        });
        btnStartClient.onClick.AddListener(() =>
        {
            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.JoinGame();
                SchimbaPanelAsteptare();
            }
        });
    }

    void SchimbaPanelAsteptare()
    {
        if (StartPanel != null)
        {
            StartPanel.SetActive(false);
        }
        if (AsteptarePanel != null)
        {
            AsteptarePanel.SetActive(true);
        }
    }
    
    public void ActiveazaMeniuClase()
    {
        if (AsteptarePanel != null)
        {
            AsteptarePanel.SetActive(false);
        }
        
        if (ClasePanel != null)
        {
            ClasePanel.SetActive(true);
        }
    }

    private void Update()
    {
        if(AsteptarePanel != null && AsteptarePanel.activeSelf && GameSessionManager.Instance != null && NetworkManager.Singleton != null)
        {
            
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient)
            {
                if (textJucatoriConectati != null)
                {
                    textJucatoriConectati.text = "Se conectează la server...";
                }
                return; // Oprim codul aici până ne conectăm
            }
            
            // Citim de la Server în timp real numerele
            if (!numaratoareInversaPornita)
            {
                int jucatoriCurenti = GameSessionManager.Instance.jucatoriConectati.Value;
                int jucatoriMaxim = GameSessionManager.Instance.nrMaxJucatori.Value;
                if (textJucatoriConectati != null)
                {
                    textJucatoriConectati.text = "Jucatori conectati: " + jucatoriCurenti + " / " + jucatoriMaxim;
                }
            }
        }
        
        if (ClasePanel != null && ClasePanel.activeSelf && GameSessionManager.Instance != null)
        {
            btnTank.interactable = !GameSessionManager.Instance.tankOcupat.Value && !aDatClick;
            btnSpion.interactable = !GameSessionManager.Instance.spionOcupat.Value && !aDatClick;
            btnConstructor.interactable = !GameSessionManager.Instance.constructorOcupat.Value && !aDatClick;
            btnMedic.interactable = !GameSessionManager.Instance.medicOcupat.Value && !aDatClick;
            btnArcas.interactable = !GameSessionManager.Instance.arcasOcupat.Value && !aDatClick;
        }
    }

    public void ComandaSpawn(int index)
    {
        if (aDatClick)
        {
            return;
        }

        aDatClick = true;
        GameSessionManager.Instance.AlegeClasaServerRpc(index);
    }
}
