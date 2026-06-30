using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NightSpawner : NetworkBehaviour
{
    [Header("Setari Spawner Obisnuit")]
    public GameObject inamicDeNoaptePrefab; 
    public Transform[] puncteSpawn; 
    public int inamiciDeNoaptePerNoapte = 1;
    public float timpIntreSpawns = 1f;
    
    [Header("Setari Spawner Asediu")]
    public GameObject inamicAsediuPrefab; 
    public Transform punctSpawnAsediu;
    public Poarta poarta;
    public Transform checkpointAsediu;
    public int inamiciAsediuPerNoapte = 3;
    public float timpIntreSpawnsAsediu = 1f;

    
    [Header("Referinte")]
    public DayNightManager dayNightManager;
    public Transform obiectivBaza;
    

    private bool aSpawnatNoapteaAsta = false;
    public List<Health> inamiciInViata = new List<Health>();
    
    private void Update()
    {
        if (!IsServer || dayNightManager == null)
        {
            return;
        }

        // Verifica daca este noapte si daca inamicii au fost deja spawnati
        if (dayNightManager.EsteNoapte && !aSpawnatNoapteaAsta)
        {
            aSpawnatNoapteaAsta = true;
            inamiciInViata.Clear(); // Curata lista de inamici morti/vechi
            
            // Incepe corutina pentru InamiciDeNoapte
            StartCoroutine(SpawnInamiciDeNoapteRoutine());
            
            // Incepe corutina pentru InamiciAsediu daca sunt setati
            if (inamicAsediuPrefab != null && punctSpawnAsediu != null)
            {
                StartCoroutine(SpawnInamiciAsediuRoutine());
            }
        }
        // Reseteaza starea de spawn cand se face zi, pentru a permite spawnarea inamiciilor in noaptea urmatoare
        else if (dayNightManager.EsteZi && aSpawnatNoapteaAsta)
        {
            aSpawnatNoapteaAsta = false;
        }
    }
    
    private IEnumerator SpawnInamiciDeNoapteRoutine()
    {
        for (int i = 0; i < inamiciDeNoaptePerNoapte; i++)
        {
            if (puncteSpawn.Length == 0)
            {
                yield break;
            }
            
            Transform punctSpawnAles = puncteSpawn[Random.Range(0, puncteSpawn.Length)];
            GameObject inamicInstantiat = Instantiate(inamicDeNoaptePrefab, punctSpawnAles.position, punctSpawnAles.rotation);
            
            
            NetworkObject netObj = inamicInstantiat.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            
            InamiciDeNoapte scriptDeNoapte = inamicInstantiat.GetComponent<InamiciDeNoapte>();
            if (scriptDeNoapte != null)
            {
                scriptDeNoapte.tintaBaza = obiectivBaza;
            }
            
            Health healthInamic = inamicInstantiat.GetComponent<Health>();
            if (healthInamic != null)
            {
                inamiciInViata.Add(healthInamic);
            }
            
            yield return new WaitForSeconds(timpIntreSpawns);
        }
    }

    private IEnumerator SpawnInamiciAsediuRoutine()
    {
        for (int i = 0; i < inamiciAsediuPerNoapte; i++)
        {
            GameObject inamicInstantiat = Instantiate(inamicAsediuPrefab, punctSpawnAsediu.position, punctSpawnAsediu.rotation);
            
            NetworkObject netObj = inamicInstantiat.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            
            InamiciAsediu scriptAsediu = inamicInstantiat.GetComponent<InamiciAsediu>();
            if (scriptAsediu != null)
            {
                scriptAsediu.tintaTitan = obiectivBaza;
                scriptAsediu.checkpoint = checkpointAsediu;
                scriptAsediu.poarta = poarta;
            }
            
            Health healthInamic = inamicInstantiat.GetComponent<Health>();
            if (healthInamic != null)
            {
                inamiciInViata.Add(healthInamic);
            }
            
            yield return new WaitForSeconds(timpIntreSpawnsAsediu);
        }
    }
    
    public bool SuntInamiciInViata()
    {
        inamiciInViata.RemoveAll(inamic => inamic == null);

        foreach (var inamic in inamiciInViata)
        {
            if(inamic.currentHealth.Value > 0)
            {
                return true;
            }
        }
        return false;
    }
    
    // Functie apelata de HouseManager cand Spionul trage maneta-capcana
    public void DeclanseazaAtacSurpriza()
    {
        if (IsServer)
        {
            StartCoroutine(SpawnAtacSurprizaRoutine());
        }
    }

    private IEnumerator SpawnAtacSurprizaRoutine()
    {
        inamiciInViata.RemoveAll(inamic => inamic == null);

        for (int i = 0; i < inamiciDeNoaptePerNoapte; i++)
        {
            if (puncteSpawn.Length == 0)
            {
                yield break;
            }

            Transform punctSpawnAles = puncteSpawn[Random.Range(0, puncteSpawn.Length)];
            GameObject inamicInstantiat = Instantiate(inamicDeNoaptePrefab, punctSpawnAles.position, punctSpawnAles.rotation);
            
            NetworkObject netObj = inamicInstantiat.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            
            InamiciDeNoapte scriptDeNoapte = inamicInstantiat.GetComponent<InamiciDeNoapte>();
            if (scriptDeNoapte != null)
            {
                scriptDeNoapte.tintaBaza = obiectivBaza;
            }
            
            Health healthInamic = inamicInstantiat.GetComponent<Health>();
            if (healthInamic != null)
            {
                inamiciInViata.Add(healthInamic);
            }
            
            yield return new WaitForSeconds(timpIntreSpawns);
        }
    }
}