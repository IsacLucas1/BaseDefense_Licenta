using UnityEngine;
using Unity.Netcode;

public class DeathZoneManager : NetworkBehaviour
{
    public static DeathZoneManager Instance { get; private set; }
    
    [Header ("Centru si raze")]
    public Transform centruZona;
    public float razaInitiala = 200f;
    public float razaFinala = 30f;
    public float vitezaStrangere = 5f;
    
    [Header("Damage")]
    public int damagePerTick = 10;
    public float intervalTick = 1.5f;
    
    [Header("Vizual")]
    public GameObject vizualFurtuna;
    public float inaltimeVizual = 50f;
    
    public NetworkVariable<float> razaCurenta = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> furtunaActiva = new NetworkVariable<bool>(false);
    
    private float nextTickTime = 0f;
    
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
    
    public void PornesteFurtuna()
    {
        if (!IsServer)
        {
            return;
        }
        
        razaCurenta.Value = razaInitiala;
        nextTickTime = Time.time + intervalTick;
        furtunaActiva.Value = true;
    }
    
    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }       
        
        if (!furtunaActiva.Value)
        {
            return;
        }
        
        if (IsServer)
        {
            if (razaCurenta.Value > razaFinala)
            {
                razaCurenta.Value = Mathf.MoveTowards(razaCurenta.Value, razaFinala, vitezaStrangere * Time.deltaTime);
            }
            
            if (Time.time >= nextTickTime)
            {
                nextTickTime = Time.time + intervalTick;
                AplicaDamageInFurtuna();
            }
        }
        
        ActualizeazaVizual();
        ActualizeazaOverlayLocal();
    }
    
    private void AplicaDamageInFurtuna()
    {
        if (centruZona == null)
        {
            return;
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }
            
            BasePlayer player = client.PlayerObject.GetComponent<BasePlayer>();
            if (player == null || player.isDead.Value)
            {
                continue;
            }

            if (DistantaOrizontala(player.transform.position) > razaCurenta.Value)
            {
                Health health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damagePerTick);
                }
            }
        }
    }
    
    private void ActualizeazaVizual()
    {
        if (vizualFurtuna == null || centruZona == null)
        {
            return;
        }

        if (!vizualFurtuna.activeSelf)
        {
            vizualFurtuna.SetActive(true);
        }

        vizualFurtuna.transform.position = new Vector3(centruZona.position.x, vizualFurtuna.transform.position.y, centruZona.position.z);
        vizualFurtuna.transform.localScale = new Vector3(razaCurenta.Value * 2f, inaltimeVizual, razaCurenta.Value * 2f);
    }
    
    private void ActualizeazaOverlayLocal()
    {
        if (centruZona == null || UIManager.Instance == null)
        {
            return;
        }

        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
        {
            UIManager.Instance.ArataFurtunaOverlay(false);
            return;
        }

        BasePlayer player = nm.LocalClient.PlayerObject.GetComponent<BasePlayer>();
        if (player == null || player.isDead.Value)
        {
            UIManager.Instance.ArataFurtunaOverlay(false);
            return;
        }

        bool inFurtuna = DistantaOrizontala(player.transform.position) > razaCurenta.Value;
        UIManager.Instance.ArataFurtunaOverlay(inFurtuna);
    }

    private float DistantaOrizontala(Vector3 pozitie)
    {
        float dx = pozitie.x - centruZona.position.x;
        float dz = pozitie.z - centruZona.position.z;
        
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
    
    private void OnDrawGizmos()
    {
        if (centruZona == null)
        {
            return;
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(centruZona.position, razaInitiala);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centruZona.position, razaFinala);
        
        if (Application.isPlaying && IsSpawned)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centruZona.position, razaCurenta.Value);
        }
    }
}
