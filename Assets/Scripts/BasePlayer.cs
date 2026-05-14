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
    public NetworkVariable<int> extraDamage = new NetworkVariable<int>(0);
    public float distantaAdunare = 3f;
    
    public float taiereCooldown = 1f;
    private float nextTaiereTime = 0f;
    private bool inZonaMagazin = false;
    public bool upgradeClasaCumparat = false;
    
    [Header("Efecte Vizuale")]
    public NetworkVariable<bool> isInvisible = new NetworkVariable<bool>(false);
    public ParticleSystem efectMoarte;
    public Animator animator;
    
    protected ClientNetworkAnimator netAnimator;
    
    private Rigidbody rb;
    public bool isDead { get; protected set; } = false;
    protected bool isRecalling = false;
    protected Coroutine recallCoroutineActiva;
    
    public NetworkVariable<Vector3> respawnPosition = new NetworkVariable<Vector3>();
    private Quaternion respawnRotation;
    
    private Coroutine corutinaVitezaActiva;
    private Coroutine corutinaDamageActiva;
    private int bonusVitezaActiv = 0;
    private int bonusDamageActiv = 0;
    private float buffVitezaTimpFinal;
    private float buffVitezaDurataTotala;
    private float buffDamageTimpFinal;
    private float buffDamageDurataTotala;
    private float timpRamasViteza = 0f;
    private float timpRamasDamage = 0f;
    
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        speed.OnValueChanged += ActualizeazaTextViteza;
        animator = GetComponentInChildren<Animator>();
        netAnimator = GetComponentInChildren<ClientNetworkAnimator>();
        
        if (efectMoarte != null)
        {
            efectMoarte.gameObject.SetActive(false); 
        }
        
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
    
    public virtual int ObtineDamageTotal()
    {
        return extraDamage.Value;
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
            extraDamage.OnValueChanged += ActualizeazaTextDamage;
            
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
                UIManager.Instance.ActualizeazaDamage(ObtineDamageTotal());
                
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
    
    private void ActualizeazaTextDamage(int valoareVeche, int valoareNoua)
    {
        if (IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.ActualizeazaDamage(ObtineDamageTotal());
        }
    }

    public override void OnNetworkDespawn()
    {
        lemn.OnValueChanged -= ActualizeazaTextLemn;
        speed.OnValueChanged -= ActualizeazaTextViteza;
        bani.OnValueChanged -= ActualizeazaTextBani;
        extraDamage.OnValueChanged -= ActualizeazaTextDamage;
        
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

        if (UIManager.Instance != null && UIManager.Instance.jocPauza)
        {
            return;
        }
        
        HandleMouseLook();

        if (UIManager.Instance != null)
        {
            if (Time.time < buffVitezaTimpFinal)
            {
                float timpRamas = buffVitezaTimpFinal - Time.time;
                float procentaj = timpRamas / buffVitezaDurataTotala;
                UIManager.Instance.ActualizeazaBuffViteza(procentaj);
            }

            if (Time.time < buffDamageTimpFinal)
            {
                float timpRamas = buffDamageTimpFinal - Time.time;
                float procentaj = timpRamas / buffDamageDurataTotala;
                UIManager.Instance.ActualizeazaBuffDamage(procentaj);
            }
        }

        if (GetComponent<Health>().currentHealth.Value > 0)
        {
            if(Input.GetKeyDown(KeyCode.K))
            {
                AnuleazaRecall();
                RequestSelfDamageServerRpc();
            }
        }
        if(Input.GetKeyDown(KeyCode.B) && !isRecalling)
        {
            recallCoroutineActiva = StartCoroutine(StartRecallCoroutine());
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            AnuleazaRecall();
            
            if (UIManager.Instance != null && UIManager.Instance.ShopPanel != null && UIManager.Instance.ShopPanel.activeSelf)
            {
                UIManager.Instance.ArataMagazin(false);
            }
            else if (inZonaMagazin)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ArataMagazin(true);
                }
            }
            else if (Time.time >= nextTaiereTime)
            {
                IncearcaSaTaieCopac();
                nextTaiereTime = Time.time + taiereCooldown;
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            AnuleazaRecall();
            IncearcaInteractiuneaWarRoom();
        }
    }

    public void SeteazaInZonaMagazin(bool stare)
    {
        inZonaMagazin = stare;
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
                if (timpRamasViteza <= 0f)
                {
                    bonusVitezaActiv = valoare;
                    speed.Value += bonusVitezaActiv;
                }
                timpRamasViteza += durata;
                
                ActiveazaBuffUIClientRpc(TipCamp.Viteza, timpRamasViteza);

                if (corutinaVitezaActiva == null)
                {
                    corutinaVitezaActiva = StartCoroutine(BuffVitezaRoutine());
                }
                
                break;
            case TipCamp.Damage:
                if (timpRamasDamage <= 0f)
                {
                    bonusDamageActiv = valoare;
                    extraDamage.Value += bonusDamageActiv;
                }
                timpRamasDamage += durata;
                
                ActiveazaBuffUIClientRpc(TipCamp.Damage, timpRamasDamage);
                
                if (corutinaDamageActiva == null)
                {
                    corutinaDamageActiva = StartCoroutine(BuffDamageRoutine());
                }
                break;
            case TipCamp.Bani:
                AdaugaBani(valoare);
                break;
            case TipCamp.Lemn:
                AdaugaLemn(valoare);
                break;
            case TipCamp.Obisnuit:
                AdaugaBani(valoare);
                break;
        }
    }

    [ClientRpc]
    private void ActiveazaBuffUIClientRpc(TipCamp tip, float durata)
    {
        if (!IsOwner || UIManager.Instance == null)
        {
            return;
        } 

        if (tip == TipCamp.Viteza)
        {
            buffVitezaDurataTotala = durata;
            buffVitezaTimpFinal = Time.time + durata;
            UIManager.Instance.SeteazaVizibilitateBuffViteza(true);
        }
        else if (tip == TipCamp.Damage)
        {
            buffDamageDurataTotala = durata;
            buffDamageTimpFinal = Time.time + durata;
            UIManager.Instance.SeteazaVizibilitateBuffDamage(true);
        }
    }

    [ClientRpc]
    private void DezactiveazaBuffUIClientRpc(TipCamp tip)
    {
        if (!IsOwner || UIManager.Instance == null)
        {
            return;
        }

        if (tip == TipCamp.Viteza)
        {
            UIManager.Instance.SeteazaVizibilitateBuffViteza(false);
        }
        else if (tip == TipCamp.Damage)
        {
            UIManager.Instance.SeteazaVizibilitateBuffDamage(false);
        }
    }
    protected IEnumerator BuffVitezaRoutine()
    {
        while (timpRamasViteza > 0)
        {
            timpRamasViteza -= Time.deltaTime; 
            yield return null;
        }
        
        speed.Value -= bonusVitezaActiv;
        bonusVitezaActiv = 0;
        corutinaVitezaActiva = null;
       
        DezactiveazaBuffUIClientRpc(TipCamp.Viteza);
    }
    protected IEnumerator BuffDamageRoutine()
    {
        while (timpRamasDamage > 0)
        {
            timpRamasDamage -= Time.deltaTime;
            yield return null;
        }
        
        extraDamage.Value -= bonusDamageActiv;
        bonusDamageActiv = 0;
        corutinaDamageActiva = null;
        
        DezactiveazaBuffUIClientRpc(TipCamp.Damage);
    }
    
    [ServerRpc]
    protected void DamageServerRpc(ulong targetID, int amount, ulong attackerId)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetID, out NetworkObject targetObj))
        {
            Health targetHealth = targetObj.GetComponent<Health>();
            if (targetHealth != null && targetHealth.currentHealth.Value > 0)
            {
                targetHealth.TakeDamage(amount, attackerId);
            }
        }
    }
    
    protected void AnuleazaRecall()
    {
        if (isRecalling)
        {
            isRecalling = false;
            if (recallCoroutineActiva != null)
            {
                StopCoroutine(recallCoroutineActiva);
                recallCoroutineActiva = null;
            }
            Debug.Log("Recall anulat din cauza unei actiuni.");
        }
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
        if (!IsOwner || isDead || (UIManager.Instance != null && UIManager.Instance.jocPauza))
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

        if (animator != null && animator.isActiveAndEnabled) 
        {
            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", z);
        }
        
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
        
        if (stareMoarte && efectMoarte != null)
        {
            efectMoarte.gameObject.SetActive(true); 
            efectMoarte.Play(true); 
        }
        else if (!stareMoarte && efectMoarte != null)
        {
            efectMoarte.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            efectMoarte.gameObject.SetActive(false);
        }
        
        Renderer[] renderer = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderer)
        {
            if (r is LineRenderer)
            {
                continue;
            }
            
            if (efectMoarte != null && r.transform.IsChildOf(efectMoarte.transform))
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
    
    private void IncearcaInteractiuneaWarRoom()
    {
        Ray ray = new Ray(cameraCap.position, cameraCap.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            ButonWarRoom warRoom = hit.collider.GetComponent<ButonWarRoom>();
            if (warRoom != null)
            {
                warRoom.ApasaButon();
            }
            
            DepozitLemn depozit = hit.collider.GetComponent<DepozitLemn>();
            if (depozit != null)
            {
                depozit.IncearcaDepozitareLemn(this);
            }
        }
    }
    
    [ServerRpc]
    public void CumparaUpgradeServerRpc(int upgradeId)
    {
        int cost = 0;
        
        switch (upgradeId)
        {
            case 1: cost = 100; break; // Viteza
            case 2: cost = 150; break; // Damage
            case 3: cost = 50;  break; // + Lemn
            case 4: cost = 40;  break; // + Viata
            case 5: cost = 200; break; // + Viata Max
            case 6: cost = 60;  break; // Bonus Viteza temp
            case 7: cost = 80;  break; // Bonus Damage temp
            case 8: cost = 120; break; // Taiere rapida
            case 9: cost = 300; break; // Viata Ziduri
        }
        
        if (bani.Value < cost)
        {
            AfiseazaEroareMagazinClientRpc("Nu ai suficienți bani!");
            return; 
        }
        
        if (upgradeId == 4)
        {
            Health h = GetComponent<Health>();
            if (h != null && h.currentHealth.Value >= h.maxHealth.Value)
            {
                AfiseazaEroareMagazinClientRpc("Ai deja viața la maximum!"); 
                return; 
            }
        }
        else if (upgradeId == 6)
        {
            if (timpRamasViteza > 0f)
            {
                AfiseazaEroareMagazinClientRpc("Ai deja un bonus de viteză activ!"); 
                return;
            }
        }
        else if (upgradeId == 7)
        {
            if (timpRamasDamage > 0f)
            {
                AfiseazaEroareMagazinClientRpc("Ai deja un bonus de damage activ!"); 
                return;
            }
        }
        
        bani.Value -= cost; 

        switch (upgradeId)
        {
            case 1: speed.Value += 1f; break;
            case 2: extraDamage.Value += 5; break;
            case 3: lemn.Value += 50; break;
            case 4: 
                Health h1 = GetComponent<Health>();
                if (h1 != null)
                {
                    h1.Heal(50);
                }
                break;
            case 5: 
                Health h2 = GetComponent<Health>();
                if (h2 != null)
                {
                    h2.maxHealth.Value += 50;
                    h2.currentHealth.Value += 50;
                }
                break;
            case 6: PrimesteRecompensa(TipCamp.Viteza, 1, 30f); break;
            case 7: PrimesteRecompensa(TipCamp.Damage, 10, 30f); break;
            case 8: UpgradeTaiereLemnClientRpc(); break;
            case 9: 
                Zid[] ziduri = FindObjectsByType<Zid>(FindObjectsSortMode.None);
                foreach (Zid z in ziduri)
                {
                    z.CresteCapacitateaServer(50);
                }
                break;
        }
        Debug.Log($"Upgrade-ul cu ID-ul {upgradeId} a fost achizitionat!");
    }

    [ClientRpc]
    private void AfiseazaEroareMagazinClientRpc(string mesajEroare)
    {
        if (IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.ArataEroareMagazin(mesajEroare);
        }
    }
    
    [ClientRpc]
    private void UpgradeTaiereLemnClientRpc()
    {
        if (IsOwner)
        {
            taiereCooldown = Mathf.Max(0.2f, taiereCooldown - 0.2f);
        }
    }
    
    [ServerRpc]
    public void CumparaUpgradeClasaServerRpc()
    {
        int cost = 250; 
        
        if (upgradeClasaCumparat)
        {
            AfiseazaEroareMagazinClientRpc("Ai cumpărat deja Upgrade-ul de Clasă!");
            return;
        }
        
        if (bani.Value < cost)
        {
            AfiseazaEroareMagazinClientRpc("Nu ai suficienți bani pentru Upgrade-ul de Clasă!");
            return;
        }

        bani.Value -= cost;
        
        AplicaUpgradeClasa(); 
        ConfirmaUpgradeCumparatClientRpc(); 
        
        Debug.Log("Upgrade de clasă achiziționat!");
    }

    [ClientRpc]
    private void ConfirmaUpgradeCumparatClientRpc()
    {
        upgradeClasaCumparat = true;
        if (IsOwner && !IsServer)
        {
            AplicaUpgradeClasa();
        }
    }
    
    protected virtual void AplicaUpgradeClasa()
    {
        
    }
    
}
