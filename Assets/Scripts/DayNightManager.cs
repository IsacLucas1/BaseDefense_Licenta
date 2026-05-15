using Unity.Netcode;
using UnityEngine;

public class DayNightManager : NetworkBehaviour
{
    [Header("Setari Zi/Noapte")] 
    public Light lumina;
    public float ziDurata = 120f;

    [Header("Referinte Night Spawner")]
    public NightSpawner nightSpawner;
    
    private float nrMinJucatori = 2f;
    private NetworkVariable<bool> startTime = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> timpulMerge = new NetworkVariable<bool>(false);
    public NetworkVariable<float> timpSincronizatServer = new NetworkVariable<float>(0f);
    
    private float timpCurentLocal = 0f;
    private float timerSincronizare = 0f;
    public bool EsteZi => (timpCurentLocal / ziDurata) < 0.5f;
    public bool EsteNoapte => !EsteZi;

    public override void OnNetworkSpawn()
    {
        // Când un jucător intră în joc, preia timpul exact de la server o singură dată
        timpCurentLocal = timpSincronizatServer.Value;
    }
    
    private void Update()
    {
        if (IsServer)
        {
            if (!startTime.Value)
            {
                BasePlayer[] jucatoriSpawnati = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
                if (jucatoriSpawnati.Length >= nrMinJucatori)
                {
                    startTime.Value = true;
                    timpulMerge.Value = true;
                }
            }

            if (startTime.Value)
            {
                bool opresteTimpulPentruInamici = false;

                if (timpCurentLocal >= ziDurata - 2f)
                {
                    if (nightSpawner != null && nightSpawner.SuntInamiciInViata())
                    {
                        opresteTimpulPentruInamici = true;
                    }
                    else if (timpCurentLocal < ziDurata)
                    {
                        timpCurentLocal = ziDurata; 
                        timpSincronizatServer.Value = 0f; 
                        timerSincronizare = 1f; 
                    }
                }

                timpulMerge.Value = !opresteTimpulPentruInamici;
                timerSincronizare += Time.deltaTime;
                
                if (timerSincronizare >= 1f)
                {
                    timpSincronizatServer.Value = timpCurentLocal;
                    timerSincronizare = 0f;
                }
            }
        }
        
        if (timpulMerge.Value)
        {
            timpCurentLocal += Time.deltaTime;
            if (timpCurentLocal >= ziDurata)
            {
                timpCurentLocal = 0f;
            }
        }
        
        if (!IsServer && Mathf.Abs(timpCurentLocal - timpSincronizatServer.Value) > 1f)
        {
            timpCurentLocal = timpSincronizatServer.Value;
        }
        
        UpdateLumina();
    }

    private void UpdateLumina()
    {
        if (lumina == null)
        {
            return;
        }
        
        float procentZi = timpCurentLocal / ziDurata;

        float unghiRotatieSoare = (procentZi * 360f);
        
        lumina.transform.rotation = Quaternion.Euler(unghiRotatieSoare, 170f, 0f);
    }
}
