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
    private NetworkVariable<float> timpCurent = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> startTime = new NetworkVariable<bool>(false);
    public bool EsteZi => (timpCurent.Value / ziDurata) < 0.5f;
    public bool EsteNoapte => !EsteZi;

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
                }
            }

            if (startTime.Value)
            {
                bool opresteTimpulPentruInamici = false;

                if (timpCurent.Value >= ziDurata - 10f)
                {
                    if (nightSpawner != null && nightSpawner.SuntInamiciInViata())
                    {
                        opresteTimpulPentruInamici = true;
                    }
                }

                if (!opresteTimpulPentruInamici)
                {
                    timpCurent.Value += Time.deltaTime;
                    if (timpCurent.Value >= ziDurata)
                    {
                        timpCurent.Value = 0f;
                    }
                }
            }
        }
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
}
