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
        
        // Adauga listeneri pentru butoanele de selectie a clasei
        btnTank.onClick.AddListener(() => ComandaSpawn(0));
        btnSpion.onClick.AddListener(() => ComandaSpawn(1));
        btnConstructor.onClick.AddListener(() => ComandaSpawn(2));
        btnMedic.onClick.AddListener(() => ComandaSpawn(3));
        btnArcas.onClick.AddListener(() => ComandaSpawn(4));

        // Creare joc / conectare la joc prin Unity Relay
        btnStartHost.onClick.AddListener(() =>
        {
            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.CreateGame();
            }
        });
        btnStartClient.onClick.AddListener(() =>
        {
            if (RelayManager.Instance != null)
            {
                RelayManager.Instance.JoinGame();
            }
        });
    }

    // Functie pentru a schimba de la panoul de start la cel de asteptare
    public void SchimbaPanelAsteptare()
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
    
    // Afiseaza meniul de alegere a clasei si ascunde panoul de asteptare
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

    // Verifica ce clase sunt disponibile si actualizeaza textul care arata cati jucatori sunt conectati
    private void Update()
    {
        // Daca este in panelul de asteptare, actualizeaza textul cu numarul de jucatori conectati
        if(AsteptarePanel != null && AsteptarePanel.activeSelf && GameSessionManager.Instance != null && NetworkManager.Singleton != null)
        {
            // Daca nu este nici server si nici client conectat, se afiseaza mesajul de conectare
            if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient)
            {
                if (textJucatoriConectati != null)
                {
                    textJucatoriConectati.text = "Se conectează la server...";
                }
                return;
            }
            
            // Citeste de la Server in timp real numerele de jucatori conectati si maximul de jucatori
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
        
        // Daca este in panelul de alegere a clasei, actualizeaza starea butoanelor in functie de ce clase sunt deja ocupate
        if (ClasePanel != null && ClasePanel.activeSelf && GameSessionManager.Instance != null)
        {
            btnTank.interactable = !GameSessionManager.Instance.tankOcupat.Value && !aDatClick;
            btnSpion.interactable = !GameSessionManager.Instance.spionOcupat.Value && !aDatClick;
            btnConstructor.interactable = !GameSessionManager.Instance.constructorOcupat.Value && !aDatClick;
            btnMedic.interactable = !GameSessionManager.Instance.medicOcupat.Value && !aDatClick;
            btnArcas.interactable = !GameSessionManager.Instance.arcasOcupat.Value && !aDatClick;
        }
    }

    // Trimite comanda catre server pentru a alege clasa si a spawn-ui jucatorul
    public void ComandaSpawn(int index)
    {
        if (aDatClick)
        {
            return;
        }

        aDatClick = true;
        GameSessionManager.Instance.AlegeClasaServerRpc(index);
    }
    
    // Revine la ecranul de start al jocului
    public void RevinoLaStart()
    {
        if (AsteptarePanel != null)
        {
            AsteptarePanel.SetActive(false);
        }
        if (StartPanel != null)
        {
            StartPanel.SetActive(true);
        }
    }
}
