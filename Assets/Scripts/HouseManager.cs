using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public enum TipCasa
{
    Corect,
    NumarInamici,
    CapcanaBaza,      
    CapcanaLocala,    
    BaniPentruToti,
    Nimic
}

public class HouseManager : NetworkBehaviour
{
    public static HouseManager Instance;

    [Header("Setari Case")]
    public int baniRecompensa = 200;
    
    [Header("Setari Capcana Locala")]
    public GameObject inamicPrefab; 
    public int numarInamiciCapcana = 3; 

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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AlocaRoluriCase();
        }
    }

    private void AlocaRoluriCase()
    {
        // Gaseste toate manetele din scena
        LeverCasa[] caseGasite = FindObjectsByType<LeverCasa>(FindObjectsSortMode.None);
        List<LeverCasa> listaCase = new List<LeverCasa>(caseGasite);

        if (listaCase.Count == 0)
        {
            return;
        }

        // Amesteca lista de case pentru a le aloca roluri aleatoriu
        for (int i = 0; i < listaCase.Count; i++)
        {
            LeverCasa temp = listaCase[i];
            int randomIndex = Random.Range(i, listaCase.Count);
            listaCase[i] = listaCase[randomIndex];
            listaCase[randomIndex] = temp;
        }
        
        // Atribuie cate un rol unic primelor case din lista amestecata
        int index = 0;
        if (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.Corect;
        }
        if (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.NumarInamici;
        }
        if (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.CapcanaBaza;
        }
        if (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.CapcanaLocala; 
        }
        if (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.BaniPentruToti;
        }

        while (index < listaCase.Count)
        {
            listaCase[index++].tipulCasei = TipCasa.Nimic;
        }
    }
    
    public void ProceseazaManeta(TipCasa tip, ulong spionId, Vector3 pozitieManeta)
    {
        switch (tip)
        {
            case TipCasa.Corect:
                if (WarRoomManager.Instance != null)
                {
                    WarRoomManager.Instance.ActiveazaButon();
                }
                AfiseazaMesajGlobalClientRpc("Spionul a descoperit Baza Inamica! Butonul din War Room este activ!");
                break;

            case TipCasa.BaniPentruToti:
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var player = client.PlayerObject.GetComponent<BasePlayer>();
                    if (player != null)
                    {
                        player.bani.Value += baniRecompensa;
                    }
                }
                AfiseazaMesajGlobalClientRpc($"Comoara gasita! Toti jucatorii au primit {baniRecompensa} de bani.");
                break;

            case TipCasa.CapcanaBaza:
                AfiseazaMesajGlobalClientRpc("Spionul a declansat o capcana! Baza voastra este atacata!");
                NightSpawner spawner = FindFirstObjectByType<NightSpawner>();
                if (spawner != null)
                {
                    spawner.DeclanseazaAtacSurpriza();
                }
                break;

            case TipCasa.CapcanaLocala: 
                AfiseazaMesajGlobalClientRpc("Spionul a fost prins intr-o ambuscada la o casa din padure!");
                if (inamicPrefab != null)
                {
                    Transform spionTransform = null;
                    
                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(spionId, out var client))
                    {
                        if (client.PlayerObject != null)
                        {
                            spionTransform = client.PlayerObject.transform;
                        }
                    }
                    
                    // Spawneaza un numar de inamici in jurul manetei, la o distanta de 6 unitati
                    Vector3 offsetAfara = new Vector3(6f, 0f, 6f);
                    for (int i = 0; i < numarInamiciCapcana; i++)
                    {
                        Vector3 spawnPos = pozitieManeta + offsetAfara + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                        GameObject inamic = Instantiate(inamicPrefab, spawnPos, Quaternion.identity);
                        inamic.GetComponent<NetworkObject>().Spawn();
                        
                        // Daca inamicul are un script InamicAmbuscada, seteaza spionul ca tinta
                        InamicAmbuscada scriptAmbuscada = inamic.GetComponent<InamicAmbuscada>();
                        if (scriptAmbuscada != null && spionTransform != null)
                        {
                            scriptAmbuscada.SeteazaSpion(spionTransform);
                        }
                    }
                }
                break;

            case TipCasa.NumarInamici:
                int numarInamici = 0;
                if (BazaInamicaManager.Instance != null)
                {
                    numarInamici = BazaInamicaManager.Instance.NumarInamiciInBaza();
                }
                TrimiteMesajPrivatClientRpc($"Informatie: In baza adversa sunt {numarInamici} inamici.", spionId);
                break;

            case TipCasa.Nimic:
                TrimiteMesajPrivatClientRpc("Ai tras maneta... dar nu s-a întamplat nimic.", spionId);
                break;
        }
    }

    [ClientRpc]
    private void AfiseazaMesajGlobalClientRpc(string mesaj)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ArataNotificare(mesaj);
        }
    }

    [ClientRpc]
    private void TrimiteMesajPrivatClientRpc(string mesaj, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ArataNotificare(mesaj);
            }
        }
    }
}