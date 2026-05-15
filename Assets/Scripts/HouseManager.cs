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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
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
        LeverCasa[] caseGasite = FindObjectsByType<LeverCasa>(FindObjectsSortMode.None);
        List<LeverCasa> listaCase = new List<LeverCasa>(caseGasite);

        if (listaCase.Count == 0)
        {
            return;
        }

        for (int i = 0; i < listaCase.Count; i++)
        {
            LeverCasa temp = listaCase[i];
            int randomIndex = Random.Range(i, listaCase.Count);
            listaCase[i] = listaCase[randomIndex];
            listaCase[randomIndex] = temp;
        }
        
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
                AfiseazaMesajGlobalClientRpc("Spionul a descoperit Baza Inamică! Butonul din War Room este activ!");
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
                AfiseazaMesajGlobalClientRpc($"Comoară găsită! Toți jucătorii au primit {baniRecompensa} de bani.");
                break;

            case TipCasa.CapcanaBaza:
                AfiseazaMesajGlobalClientRpc("Spionul a declanșat o capcană! Baza voastră este atacată!");
                NightSpawner spawner = FindFirstObjectByType<NightSpawner>();
                if (spawner != null)
                {
                    spawner.DeclanseazaAtacSurpriza();
                }
                break;

            case TipCasa.CapcanaLocala: 
                AfiseazaMesajGlobalClientRpc("Spionul a fost prins într-o ambuscadă la o casă din pădure!");
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

                    if (spionTransform == null)
                    {
                        Debug.LogError($"[Eroare Netcode] Nu am putut găsi Spionul cu ClientId {spionId} pe server!");
                    }
                    
                    Vector3 offsetAfara = new Vector3(6f, 0f, 6f);
                    for (int i = 0; i < numarInamiciCapcana; i++)
                    {
                        Vector3 spawnPos = pozitieManeta + offsetAfara + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
                        GameObject inamic = Instantiate(inamicPrefab, spawnPos, Quaternion.identity);
                        inamic.GetComponent<NetworkObject>().Spawn();
                        
                        InamicAmbuscada scriptAmbuscada = inamic.GetComponent<InamicAmbuscada>();
                        if (scriptAmbuscada != null && spionTransform != null)
                        {
                            scriptAmbuscada.SeteazaSpion(spionTransform);
                        }
                    }
                }
                break;

            case TipCasa.NumarInamici:
                InamiciAI[] inamici = FindObjectsByType<InamiciAI>(FindObjectsSortMode.None);
                int numarInamici = 0;
                foreach (var inamic in inamici)
                {
                    Health h = inamic.GetComponent<Health>();
                    if (h != null && h.currentHealth.Value > 0)
                    {
                        numarInamici++;
                    }
                }
                TrimiteMesajPrivatClientRpc($"Informație: Momentan sunt {numarInamici} inamici în viață pe hartă.", spionId);
                break;

            case TipCasa.Nimic:
                TrimiteMesajPrivatClientRpc("Ai tras maneta... dar nu s-a întâmplat nimic.", spionId);
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