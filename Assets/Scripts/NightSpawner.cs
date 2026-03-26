using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NightSpawner : NetworkBehaviour
{
    [Header("Setari Spawner")]
    public GameObject inamicPrefab; 
    [Tooltip("Trage aici cele 3 puncte de spawn (Empty GameObjects)")]
    public Transform[] puncteSpawn; 
    public int inamiciPerNoapte = 10;
    public float timpIntreSpawns = 1.5f;
    
    [Header("Referinte")]
    public DayNightManager dayNightManager;
    public Transform obiectivBaza; 

    private bool aSpawnatNoapteaAsta = false;
    public List<Health> inamiciInViata = new List<Health>();
    
    private void Update()
    {
        if (!IsServer || dayNightManager == null) return;

        if (dayNightManager.EsteNoapte && !aSpawnatNoapteaAsta)
        {
            aSpawnatNoapteaAsta = true;
            StartCoroutine(SpawnInamiciRoutine());
        }
        else if (dayNightManager.EsteZi && aSpawnatNoapteaAsta)
        {
            aSpawnatNoapteaAsta = false;
        }
    }
    
    private IEnumerator SpawnInamiciRoutine()
    {
        inamiciInViata.Clear();
        
        for (int i = 0; i < inamiciPerNoapte; i++)
        {
            if (puncteSpawn.Length == 0)
            {
                yield break;
            }
            
            Transform punctSpawnAles = puncteSpawn[Random.Range(0, puncteSpawn.Length)];
            GameObject inamicInstantiat = Instantiate(inamicPrefab, punctSpawnAles.position, punctSpawnAles.rotation);
            
            
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
}