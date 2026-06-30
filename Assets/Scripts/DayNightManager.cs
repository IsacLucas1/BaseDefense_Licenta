using Unity.Netcode;
using UnityEngine;

public class DayNightManager : NetworkBehaviour
{
    [Header("Setari Zi/Noapte")] 
    public Light lumina;
    public float ziDurata = 120f;

    [Header("Referinte Night Spawner")]
    public NightSpawner nightSpawner;
    
    private NetworkVariable<float> timpCurent = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> startTime = new NetworkVariable<bool>(false);
    public bool EsteZi => (timpCurent.Value / ziDurata) < 0.5f;
    public bool EsteNoapte => !EsteZi;
    
    private float urmatorulCheckJucatori = 0f;
    public float intervalCheckJucatori = 1f;
    
    public NetworkVariable<bool> timpOpritPentruAsediu = new NetworkVariable<bool>(false);

    private bool SuntDestuiJucatoriSpawnati()
    {
        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        int jucatoriSpawnati = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.GetComponent<BasePlayer>() != null)
            {
                jucatoriSpawnati++;
            }
        }

        
        // Daca GameSessionManager nu este null, foloseste nrMaxJucatori din el, altfel foloseste 2 ca valoare implicita
        int minJucatori = GameSessionManager.Instance != null ? GameSessionManager.Instance.nrMaxJucatori.Value : 2;
        
        return jucatoriSpawnati >= minJucatori;
    }
    
    private void Update()
    {
        if (IsServer)
        {
            // Daca timpul nu a inceput, verifica daca sunt destui jucatori spawnati pentru a incepe ziua
            if (!startTime.Value && Time.time >= urmatorulCheckJucatori)
            {
                urmatorulCheckJucatori = Time.time + intervalCheckJucatori;

                if (SuntDestuiJucatoriSpawnati())
                {
                    startTime.Value = true;
                }
            }

            // Cronometrul a pornit
            if (startTime.Value)
            {
                bool opresteTimpulPentruInamici = false;

                // Daca timpul curent este aproape de sfarsitul zilei, verifica daca mai sunt inamici in
                // viata si opreste timpul pana la eliminarea tuturor
                if (timpCurent.Value >= ziDurata - 10f)
                {
                    if (nightSpawner != null && nightSpawner.SuntInamiciInViata())
                    {
                        opresteTimpulPentruInamici = true;
                    }
                }

                // Daca timpul nu este oprit pentru inamici si nici pentru atacul final, incrementeaza timpul curent
                if (!opresteTimpulPentruInamici && !timpOpritPentruAsediu.Value)
                {
                    timpCurent.Value += Time.deltaTime;
                    if (timpCurent.Value >= ziDurata)
                    {
                        timpCurent.Value = 0f;
                    }
                }
            }
        }
        
        // Fiecare client actualizeaza rotatia soarelui local in functie de NetworkVariable
        UpdateLumina();
    }
    
    private void UpdateLumina()
    {
        if (lumina == null)
        {
            return;
        }
        
        float procentZi = timpCurent.Value / ziDurata;

        float unghiRotatieSoare = (procentZi * 360f);
        
        lumina.transform.rotation = Quaternion.Euler(unghiRotatieSoare, 170f, 0f);
    }
    
    // ServerRpc pentru a opri timpul curent pentru atacul final. Este apelat din alte script-uri
    [ServerRpc]
    public void OpresteTimpulPentruAsediuServerRpc()
    {
        timpOpritPentruAsediu.Value = true;
    }

    [ServerRpc]
    public void SeteazaTimpAtacFinalServerRpc()
    {
        timpCurent.Value = ziDurata * 0.25f; 
        timpOpritPentruAsediu.Value = true;
    }
}
