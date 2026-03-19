using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BasePlayer : NetworkBehaviour
{
    [Header("Setari Miscare")]
    public NetworkVariable<float> speed = new NetworkVariable<float>(5f);
    protected float speedReference = 5f;

    [Header("Setari Camera")]
    public Transform cameraCap;
    public float mouseSensitivity = 200f;
    private float _xRotation;

    [Header("Resurse")]
    public NetworkVariable<int> lemn = new NetworkVariable<int>();
    public NetworkVariable<int> bani = new NetworkVariable<int>(); 
    public float distantaAdunare = 3f;
    
    public float taiereCooldown = 1f;
    private float nextTaiereTime = 0f;
        
    private Rigidbody rb;
    public bool isDead { get; protected set; } = false;
    protected bool isRecalling = false;
    
    public NetworkVariable<Vector3> respawnPosition = new NetworkVariable<Vector3>();
    private Quaternion respawnRotation;
    
    private Coroutine corutinaVitezaActiva;
    private Coroutine corutinaDamageActiva;
    private int bonusVitezaActiv = 0;
    private int bonusDamageActiv = 0;
    public int extraDamage { get; protected set; } = 0;
    
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        speed.OnValueChanged += ActualizeazaTextViteza;
        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            DisableRemotePlayer();
        }
    }
    
    public void SetSpawnPoint(Vector3 spawnPosition, Quaternion rotation)
    {
        if (IsServer)
        {
            respawnPosition.Value = spawnPosition;
            respawnRotation = rotation;
        }
    }

    protected virtual void SetupLocalPlayer()
    {
        if (cameraCap != null)
        {
            cameraCap.gameObject.SetActive(true);
            
            var listener = cameraCap.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = true;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            lemn.OnValueChanged += ActualizeazaTextLemn;
            bani.OnValueChanged += ActualizeazaTextBani;
            
            Health healthComp = GetComponent<Health>();
            if (healthComp != null)
            {
                healthComp.currentHealth.OnValueChanged += ActualizeazaViata;
                healthComp.maxHealth.OnValueChanged += ActualizeazaViata;
            }
            
            if(UIManager.Instance != null)
            {
                UIManager.Instance.ActualizeazaLemn(lemn.Value);
                UIManager.Instance.ActualizeazaBani(bani.Value);
                
                float coeficientViteza = speed.Value / speedReference;
                UIManager.Instance.ActualizeazaViteza(coeficientViteza);
                
                if (healthComp != null)
                {
                    UIManager.Instance.ActualizeazaViata(healthComp.currentHealth.Value, healthComp.maxHealth.Value);
                }
                UIManager.Instance.ActiveazaInterfataJucator();
            }
        }
    }

    private void DisableRemotePlayer()
    {
        if (cameraCap != null)
        {
            cameraCap.gameObject.SetActive(false);
            var listener = cameraCap.GetComponent<AudioListener>();
            if (listener)
            {
                listener.enabled = false;
            }
        }
    }
    
    private void ActualizeazaTextLemn(int valoareVeche, int valoareNoua)
    {
        if(IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.ActualizeazaLemn(valoareNoua);
        }
    }
    
    public virtual void AdaugaLemn(int cantitate)
    {
        if (IsServer)
        {
            lemn.Value += cantitate;
        }
    }
    
    private void ActualizeazaTextViteza(float valoareVeche, float valoareNoua)
    {
        if(IsOwner && UIManager.Instance != null)
        {
            float coeficientViteza = valoareNoua / speedReference;
            UIManager.Instance.ActualizeazaViteza(coeficientViteza);
        }
    }
    
    private void ActualizeazaViata(int valoareVeche, int valoareNoua)
    {
        if (IsOwner && UIManager.Instance != null)
        {
            Health healthComp = GetComponent<Health>();
            if (healthComp != null)
            {
                UIManager.Instance.ActualizeazaViata(healthComp.currentHealth.Value, healthComp.maxHealth.Value);
            }
        }
    }
    
    private void ActualizeazaTextBani(int valoareVeche, int valoareNoua)
    {
        if(IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.ActualizeazaBani(valoareNoua);
        }
    }

    public  void AdaugaBani(int cantitate)
    {
        if (IsServer)
        {
            bani.Value += cantitate;
        }
    }

    public override void OnNetworkDespawn()
    {
        lemn.OnValueChanged -= ActualizeazaTextLemn;
        speed.OnValueChanged -= ActualizeazaTextViteza;
        bani.OnValueChanged -= ActualizeazaTextBani;
        
        Health healthComp = GetComponent<Health>();
        if (healthComp != null)
        {
            healthComp.currentHealth.OnValueChanged -= ActualizeazaViata;
            healthComp.maxHealth.OnValueChanged -= ActualizeazaViata;
        }
    }
    
    protected virtual void Update()
    {
        if (!IsOwner || isDead)
        {
            return;
        }
        
        HandleMouseLook();
        if (GetComponent<Health>().currentHealth.Value > 0)
        {
            if(Input.GetKeyDown(KeyCode.K) && !isRecalling)
            {
                RequestSelfDamageServerRpc();
            }
        }
        if(Input.GetKeyDown(KeyCode.B) && !isRecalling)
        {
            StartCoroutine(StartRecallCoroutine());
        }
        
        if(Input.GetKeyDown(KeyCode.E) && !isRecalling && Time.time >= nextTaiereTime)
        {
            IncearcaSaTaieCopac();
            nextTaiereTime = Time.time + taiereCooldown;
        }
    }

    public virtual void PrimesteRecompensa(TipCamp camp, int valoare, float durata)
    {
        if(!IsServer)
        {
            return;
        }

        switch (camp)
        {
            case TipCamp.Viteza:
                if (corutinaVitezaActiva != null)
                {
                    StopCoroutine(corutinaVitezaActiva);
                    speed.Value -= bonusVitezaActiv;
                }
                corutinaVitezaActiva = StartCoroutine(BuffVitezaRoutine(valoare, durata));
                break;
            case TipCamp.Damage:
                if (corutinaDamageActiva != null)
                {
                    StopCoroutine(corutinaDamageActiva);
                    extraDamage -= bonusDamageActiv;
                }
                corutinaDamageActiva = StartCoroutine(BuffDamageRoutine(valoare, durata));
                break;
            case TipCamp.Bani:
                AdaugaBani(valoare);
                Debug.Log($"Jucatorul a primit {valoare} bani!");
                break;
            case TipCamp.Lemn:
                break;
        }
    }

    protected IEnumerator BuffVitezaRoutine(int valoareBonusViteza, float durataBonusViteza)
    {
        bonusVitezaActiv = valoareBonusViteza;
        speed.Value += bonusVitezaActiv;
        
        yield return new WaitForSeconds(durataBonusViteza);
        
        speed.Value -= valoareBonusViteza;
        bonusVitezaActiv = 0;
        corutinaVitezaActiva = null;
    }
    protected IEnumerator BuffDamageRoutine(int valoareBonusDamage, float durataBonusDamage)
    {
        bonusDamageActiv = valoareBonusDamage;
        extraDamage += bonusDamageActiv;
        
        yield return new WaitForSeconds(durataBonusDamage);
        
        extraDamage -= valoareBonusDamage;
        bonusDamageActiv = 0;
        corutinaVitezaActiva = null;
    }
    
    private IEnumerator StartRecallCoroutine()
    {
        isRecalling = true;
        Vector3 pozitieInitiala = transform.position;
        float recallDuration = 3f;

        Debug.Log("Recall initiat...");
        while (recallDuration > 0)
        {
            if (isDead)
            {
                isRecalling = false;
                yield break;
            }
            
            float distanta = Vector3.Distance(pozitieInitiala,transform.position);
            if (distanta > 0.01f)
            {
                    Debug.Log("Recall anulat");
                isRecalling = false;
                yield break;
            }
            recallDuration -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("Recall complet. TeleportareBaza");
        transform.position = respawnPosition.Value;
        if (rb != null)
        {
            rb.position = respawnPosition.Value;
        }
        isRecalling = false;
    }

    private void IncearcaSaTaieCopac()
    {
        Ray ray = new Ray(cameraCap.position, cameraCap.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            Copac copac = hit.collider.GetComponent<Copac>();
            if (copac != null)
            {
                copac.LovesteCopaculServerRPC(NetworkObjectId);
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner || isDead)
        {
            return;
        }
        HandleMovement();
    }
    
    [ServerRpc]
    void RequestSelfDamageServerRpc()
    {
        if (GetComponent<Health>())
        {
            GetComponent<Health>().TakeDamage(10);
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * x + transform.forward * z;
        
        Vector3 targetPosition = rb.position + movement.normalized * speed.Value * Time.deltaTime;
        rb.MovePosition(targetPosition);
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        cameraCap.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void Moarte()
    {
        if (!IsServer || isDead)
        {
            return;
        }
        StartCoroutine(RespawnRoutine());
    }
    
    IEnumerator RespawnRoutine()
    {
        isDead = true;
        EliminaJucatorulClientRpc(true);
        yield return new WaitForSeconds(5f);
        TeleporteazaInBazaClientRpc(respawnPosition.Value);
        transform.rotation = respawnRotation;
        
        if (TryGetComponent<Health>(out var healthComp))
        {
            healthComp.ResetHealth();
        }

        isDead = false;

        yield return new WaitForSeconds(0.1f);
        EliminaJucatorulClientRpc(false);
    }

    [ClientRpc]
    void EliminaJucatorulClientRpc(bool stareMoarte)
    {
        isDead = stareMoarte;
        bool isActive = !stareMoarte;
        
        Renderer[] renderer = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderer)
        {
            if (r is LineRenderer)
            {
                continue;
            }
            r.enabled = isActive;
        }
        
        Collider[] col = GetComponentsInChildren<Collider>(true);
        foreach (var c in col)
        {
            c.enabled = isActive;
        }
        
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            c.enabled = isActive;
        }
        
        if (rb != null)
        {
            if (stareMoarte)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
            }
        }
    }
    
    [ClientRpc]
    void TeleporteazaInBazaClientRpc(Vector3 pozitie)
    {
        transform.position = pozitie;
        if (rb != null)
        {
            rb.position = pozitie;
        }
    }
}
