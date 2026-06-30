using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BasePlayer : NetworkBehaviour
{
    [Header("Setari Miscare")]
    public NetworkVariable<float> speed = new NetworkVariable<float>(5f);
    protected float speedReference = 5f;
    
    [Header("Atac final")]
    public NetworkVariable<bool> miscareBlocata = new NetworkVariable<bool>(false);
    private bool rotireBlocata = false;

    [Header("Setari Camera")]
    public Transform cameraCap;
    public float mouseSensitivity = 200f;
    private float _xRotation;

    [Header("Resurse")]
    public NetworkVariable<int> lemn = new NetworkVariable<int>();
    public NetworkVariable<int> bani = new NetworkVariable<int>();
    public NetworkVariable<int> extraDamage = new NetworkVariable<int>(0);
    public float distantaAdunare = 3f;
    
    public NetworkVariable<float> taiereCooldown = new NetworkVariable<float>(1f);
    private float nextTaiereTime = 0f;
    private bool inZonaMagazin = false;
    public NetworkVariable<bool> upgradeClasaCumparat = new NetworkVariable<bool>(false);
    
    [Header("Efecte Vizuale")]
    public NetworkVariable<bool> isInvisible = new NetworkVariable<bool>(false);
    public ParticleSystem efectMoarte;
    public Animator animator;
    
    protected ClientNetworkAnimator netAnimator;
    
    private Rigidbody rb;
    public NetworkVariable<bool> isDead =new NetworkVariable<bool>(false);
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
    
    private bool miscareBlocataLocal = false;
    
    // Se apeleaza cand jucatorul se conmecteaza la sesiune si se spawneaza pe harta
    // Initializeaza Rigidbody-ul, UI-ul si evenimentele de UI
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
            // Proprietarul activeaza camera si UI-ul pentru jucatorul local
            SetupLocalPlayer();
        }
        else
        {
            // Pentru restul jucatorilor se dezactiveaza
            DisableRemotePlayer();
        }
        
        miscareBlocata.OnValueChanged += OnMiscareBlocataChanged;
    }
    
    // Defineste punctul de unde jucatorul se va respawna dupa moarte
    public void SetSpawnPoint(Vector3 spawnPosition, Quaternion rotation)
    {
        if (IsServer)
        {
            respawnPosition.Value = spawnPosition;
            respawnRotation = rotation;
        }
    }
    
    // Metoda virtuala care poate fi suprascrisa in clasele derivate pentru a obtine damage-ul total al jucatorului
    public virtual int ObtineDamageTotal()
    {
        return extraDamage.Value;
    }

    // Configureaza entitatea ca jucator local, activeaza camera si
    // UI-ul, si se aboneaza la evenimentele de schimbare a valorilor resurselor
    // Este virtuala pentru a putea fi suprascrisa in clasele derivate
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
            
            // Actualizeaza UI-ul cu valorile initiale ale resurselor si atributelor jucatorului
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

    // Ascunde componentele (camera si audio) pentru jucatorii care nu
    // sunt proprietari ai entitatii (care nu sunt local)
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
    
    // Metoda virtuala care poate fi suprascrisa in clasele derivate pentru a adauga lemn jucatorului
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
        if (valoareNoua < valoareVeche)
        {
            AnuleazaRecall();
        }
        
        if (IsOwner && UIManager.Instance != null)
        {
            if (TryGetComponent<Health>(out var healthComp))
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

    // Dezabonari pentru prevenirea erorilor de tip NullReferenceException la distrugere
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
        if (!IsOwner || isDead.Value)
        {
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.jocPauza)
        {
            return;
        }

        //Actualizeaza UI-ul aferent timer-ului pentru buff-urile temporare
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
        
        // Inchiderea magazinului
        if (UIManager.Instance != null && UIManager.Instance.esteInMagazin)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                AnuleazaRecall(); 
                UIManager.Instance.ArataMagazin(false);
            }
            return; 
        }
        
        ActualizeazaPrompt();
        
        if (!rotireBlocata)
        {
            HandleMouseLook();
        }
        
        // Buton pentru sinucidere pentru debug
        if (GetComponent<Health>().currentHealth.Value > 0)
        {
            if(Input.GetKeyDown(KeyCode.K))
            {
                AnuleazaRecall();
                RequestSelfDamageServerRpc();
            }
        }
        
        // Buton pentru recall
        if(Input.GetKeyDown(KeyCode.B) && !isRecalling)
        {
            recallCoroutineActiva = StartCoroutine(StartRecallCoroutine());
        }
        
        // Buton pentru interactiune cu magazinul sau taiere copac
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inZonaMagazin)
            {
                AnuleazaRecall(false);
                UIManager.Instance.ArataMagazin(true); 
            }
            else if (Time.time >= nextTaiereTime)
            {
                AnuleazaRecall();
                IncearcaSaTaieCopac(); 
                nextTaiereTime = Time.time + taiereCooldown.Value;
            }
        }

        // Buton pentru interactiune cu War Room
        if (Input.GetKeyDown(KeyCode.Z))
        {
            AnuleazaRecall(false);
            IncearcaInteractiuneaWarRoom();
        }
    }

    // Seteaza daca jucatorul se afla in zona in care are voie sa deschida magazinul
    public void SeteazaInZonaMagazin(bool stare)
    {
        inZonaMagazin = stare;
    }
    
    public void SeteazaRotireBlocata(bool stare)
    {
        rotireBlocata = stare;
    }
    
    // Metoda care se apeleaza cand jucatorul primeste o recompensa de tip buff sau resursa de la un camp
    public virtual void PrimesteRecompensa(TipCamp camp, int valoare, float durata)
    {
        if(!IsServer)
        {
            return;
        }

        switch (camp)
        {
            case TipCamp.Viteza:
                // Daca nu exista deja un buff de viteza activ, se aplica bonusul 
                if (timpRamasViteza <= 0f)
                {
                    bonusVitezaActiv = valoare;
                    speed.Value += bonusVitezaActiv;
                }
                // Daca exista deja un buff de viteza activ, se adauga timpul ramas la durata noului buff
                timpRamasViteza += durata;
                
                ActiveazaBuffUIClientRpc(TipCamp.Viteza, timpRamasViteza);

                if (corutinaVitezaActiva == null)
                {
                    corutinaVitezaActiva = StartCoroutine(BuffVitezaRoutine());
                }
                
                break;
            case TipCamp.Damage:
                // La fel ca la viteza doar ca pentru damage
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

    // Metode ClientRpc pentru actualizarea UI-ul pentru buff-urile temporare
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
    
    // Corutine pentru gestionarea duratei buff-urilor temporare
    protected IEnumerator BuffVitezaRoutine()
    {
        while (timpRamasViteza > 0)
        {
            timpRamasViteza -= Time.deltaTime; 
            yield return null;
        }
        
        // Dupa ce timpul buff-ului s-a terminat, se scade bonusul de viteza si se reseteaza variabilele
        speed.Value -= bonusVitezaActiv;
        bonusVitezaActiv = 0;
        corutinaVitezaActiva = null; // Permite pornirea unei noi corutine in viitor
       
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
    
    // Metoda ServerRpc care se apeleaza cand jucatorul ataca un inamic
    [ServerRpc]
    protected void DamageServerRpc(ulong targetID, ServerRpcParams rpcParams = default)
    {
        //Obtine ID-ul jucatorului care a trimis cererea de atac
        ulong attackerId = rpcParams.Receive.SenderClientId;

        if (attackerId != OwnerClientId)
        {
            return;
        }

        // Verifica daca obiectul tinta mai exista in retea
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetID, out NetworkObject targetObj))
        {
            return;
        }
        
        Health targetHealth = targetObj.GetComponent<Health>();
        if (targetHealth == null || targetHealth.currentHealth.Value <= 0)
        {
            return;
        }

        // Verifica daca obiectul tinta este eligibil pentru a primi damage
        if (!PoateDaDamageServer(targetObj))
        {
            return;
        }

        int damage = CalculeazaDamageServer(targetObj);
        targetHealth.TakeDamage(damage, attackerId);
    }
    
    protected virtual int CalculeazaDamageServer(NetworkObject targetObj)
    {
        return ObtineDamageTotal();
    }

    protected virtual bool PoateDaDamageServer(NetworkObject targetObj)
    {
        return targetObj.CompareTag("Enemy");
    }
    
    protected void AnuleazaRecall(bool afiseazaMesaj = true)
    {
        if (isRecalling)
        {
            isRecalling = false;
            if (recallCoroutineActiva != null)
            {
                StopCoroutine(recallCoroutineActiva);
                recallCoroutineActiva = null;
            }
            
            if (IsOwner && UIManager.Instance != null)
            {
                if (afiseazaMesaj)
                    UIManager.Instance.ArataMesajRecall("Recall anulat", true);
                else
                    UIManager.Instance.AscundeMesajRecall();
            }
        }
    }
    
    private IEnumerator StartRecallCoroutine()
    {
        isRecalling = true;
        Vector3 pozitieInitiala = transform.position;
        float recallDuration = 3f;
        int ultimaSecunda = -1;
        
        while (recallDuration > 0)
        {
            // Daca a murit opreste corutina imediat
            if (isDead.Value)
            {
                isRecalling = false;
                if (IsOwner && UIManager.Instance != null)
                {
                    UIManager.Instance.AscundeMesajRecall();
                }
                yield break;
            }
            
            // Daca jucatorul s-a miscat anuleaza recall-ul
            float distanta = Vector3.Distance(pozitieInitiala,transform.position);
            if (distanta > 0.01f)
            {
                isRecalling = false;
                if( IsOwner && UIManager.Instance != null)
                {
                    UIManager.Instance.ArataMesajRecall("Recall anulat", true);
                }
                yield break;
            }
            
            int secundaCurenta = Mathf.CeilToInt(recallDuration);
            if (secundaCurenta != ultimaSecunda)
            {
                ultimaSecunda = secundaCurenta;
                if (IsOwner && UIManager.Instance != null)
                {
                    UIManager.Instance.ArataMesajRecall("Recall in\n" + secundaCurenta);
                }
            }
            
            // Decrementeaza timpul ramas pentru recall
            recallDuration -= Time.deltaTime;
            yield return null;
        }
        
        if (IsOwner && UIManager.Instance != null)
        {
            UIManager.Instance.AscundeMesajRecall();
        }
        
        transform.position = respawnPosition.Value;
        RequestRecallServerRpc();
        isRecalling = false;
    }
    
    // Cerere de recall catre server pentru a teleporta jucatorul in baza
    [ServerRpc]
    private void RequestRecallServerRpc()
    {
        if (isDead.Value)
        {
            return;
        }
        
        TeleporteazaInBazaClientRpc(respawnPosition.Value);
    }
    
    // Teleporteaza jucatorul la o pozitie specificata pe toti clientii
    [ClientRpc]
    private void TeleporteazaInBazaClientRpc(Vector3 pozitie)
    {
        transform.position = pozitie;
        if (rb != null)
        {
            rb.position = pozitie;
        }
    }

    // Metoda care incearca sa taie un copac atunci cand jucatorul apasa tasta de interactiune
    private void IncearcaSaTaieCopac()
    {
        // Raza de la pozitia camerei catre inainte pentru a detecta copacii
        Ray ray = new Ray(cameraCap.position, cameraCap.forward);

        // Executa Raycast cu distanta maxima = distantaAdunare (3 unitati)
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare))
        {
            // Verifica daca obiectul lovit are componenta Copac
            Copac copac = hit.collider.GetComponent<Copac>();
            if (copac != null)
            {
                copac.LovesteCopaculServerRPC();
            }
        }
    }
    
    // Metoda care se apeleaza in FixedUpdate pentru a gestiona miscarea jucatorului
    private void FixedUpdate()
    {
        if (!IsOwner || isDead.Value || (UIManager.Instance != null && (UIManager.Instance.jocPauza || UIManager.Instance.esteInMagazin)))
        {
            return;
        }
        HandleMovement();
    }
    
    [ServerRpc]
    private void RequestSelfDamageServerRpc()
    {
        if (GetComponent<Health>())
        {
            GetComponent<Health>().TakeDamage(10);
        }
    }

    private void HandleMovement()
    {
        // Daca miscarea este blocata, se opreste deplasarea si animatia
        if (miscareBlocata.Value || miscareBlocataLocal)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); 
            
            if (animator != null && animator.isActiveAndEnabled) 
            {
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0);
            }
            return; 
        }
        
        // Citeste input-ul de la tastatura pentru miscarea jucatorului
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Actualizeaza animatia de miscare in functie de input
        if (animator != null && animator.isActiveAndEnabled) 
        {
            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", z);
        }
        
        // Calculeaza directia de miscare in functie de orientarea jucatorului
        Vector3 movement = transform.right * x + transform.forward * z;
        
        Vector3 targetVelocity = movement.normalized * speed.Value;
        targetVelocity.y = rb.linearVelocity.y;
        // Aplica viteza calculata la Rigidbody pentru a misca jucatorul
        rb.linearVelocity = targetVelocity; 
    }
    
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Ajusteaza rotatia pe axa X pentru a preveni rasturnarea camerei
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        cameraCap.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        
        //Rotire orizontala fara restrictie
        transform.Rotate(Vector3.up * mouseX);
    }

    // Apelata de Health cand jucatorul nu mai are viata
    public void Moarte()
    {
        if (!IsServer || isDead.Value)
        {
            return;
        }
        StartCoroutine(RespawnRoutine());
    }
    
    // Corutina care gestioneaza procesul de respawn al jucatorului dupa moarte
    private IEnumerator RespawnRoutine()
    {
        // Marcheaza jucatorul ca fiind mort
        isDead.Value = true;
        // Dezactiveaza vizual jucatorul si efectele sale pe toti clientii
        EliminaJucatorulClientRpc(true);
        
        // Daca atacul final a fost declansat, jucatorul nu se mai respawneaza daca moare
        if (FinalAttackManager.Instance != null && FinalAttackManager.Instance.atacFinalDeclansat.Value)
        {
            FinalAttackManager.Instance.JucatorAMurit();
            yield break; 
        }
        
        yield return new WaitForSeconds(5f);
        
        // Respawneaza jucatorul la pozitia de respawn setata
        transform.position = respawnPosition.Value;
        if (rb != null)
        {
            rb.position = respawnPosition.Value;
        }
        
        TeleporteazaInBazaClientRpc(respawnPosition.Value);
        transform.rotation = respawnRotation;
        
        if (TryGetComponent<Health>(out var healthComp))
        {
            healthComp.ResetHealth();
        }

        // Reactiveaza vizual jucatorul
        EliminaJucatorulClientRpc(false);
        yield return new WaitForSeconds(0.1f);
        
        isDead.Value = false;
    }

    [ClientRpc]
    private void EliminaJucatorulClientRpc(bool stareMoarte)
    {
        if (IsOwner && stareMoarte)
        {
            if (UIManager.Instance != null && UIManager.Instance.esteInMagazin)
            {
                UIManager.Instance.ArataMagazin(false);
            }
        }
        
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
        
        // Gestioneaza activarea sau dezactivarea componentelor vizuale, collider-elor si canvas-urilor
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
        
        // Gestioneaza Rigidbody-ul pentru a opri miscarea jucatorului cand este mort
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
        // Daca votul este in curs, nu se pot face cumparaturi
        if (WarRoomManager.Instance != null && WarRoomManager.Instance.votInCurs.Value)
        {
            return;
        }
        
        // Stabileste costul upgrade-ului in functie de ID-ul acestuia
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
            AfiseazaEroareMagazinClientRpc("Nu ai suficienti bani!");
            return; 
        }
        
        if (upgradeId == 4)
        {
            Health h = GetComponent<Health>();
            if (h != null && h.currentHealth.Value >= h.maxHealth.Value)
            {
                AfiseazaEroareMagazinClientRpc("Ai deja viata la maximum!"); 
                return; 
            }
        }
        else if (upgradeId == 6)
        {
            if (timpRamasViteza > 0f)
            {
                AfiseazaEroareMagazinClientRpc("Ai deja un bonus de viteza activ!"); 
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
            case 8: taiereCooldown.Value = Mathf.Max(0.2f, taiereCooldown.Value - 0.2f); break;
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
    
    [ServerRpc]
    public void CumparaUpgradeClasaServerRpc()
    {
        if (WarRoomManager.Instance != null && WarRoomManager.Instance.votInCurs.Value)
        {
            return;
        }
        
        int cost = 250; 
        
        // Verifica daca jucatorul a cumparat deja upgrade-ul de clasa
        if (upgradeClasaCumparat.Value)
        {
            AfiseazaEroareMagazinClientRpc("Ai cumparat deja Upgrade-ul de Clasa!");
            return;
        }
        
        if (bani.Value < cost)
        {
            AfiseazaEroareMagazinClientRpc("Nu ai suficienti bani pentru Upgrade-ul de Clasa!");
            return;
        }

        bani.Value -= cost;
        upgradeClasaCumparat.Value = true;
        
        // Apeleaza metoda virtuala. Fiecare clasa derivata poate implementa propriul efect
        AplicaUpgradeClasa();
    }
    
    protected virtual void AplicaUpgradeClasa()
    {
        
    }
    
    public void OpresteMiscarea()
    {
        AnuleazaRecall();
        miscareBlocataLocal = true;
        OpresteMiscareaServerRpc();
    }
    
    [ServerRpc]
    private void OpresteMiscareaServerRpc()
    {
        miscareBlocata.Value = true;
        
    }
    
    public void DeblocheazaMiscarea()
    {
        miscareBlocata.Value = false;
    }
    
    public void TeleporteazaInReadyZone(Vector3 pozitie)
    {
        transform.position = pozitie;
        if (rb != null)
        {
            rb.position = pozitie;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    private void OnMiscareBlocataChanged(bool vechi, bool nou)
    {
        miscareBlocataLocal = false;
    }
    
    protected virtual string ObtinePromptInteractiune(Collider col)
    {
        if (col.GetComponent<Copac>() != null)
        {
            return "Press [E] pentru a taia copacul";
        }
        if (col.GetComponent<ButonWarRoom>() != null)
        {
            bool activ = WarRoomManager.Instance != null
                         && WarRoomManager.Instance.butonActiv.Value
                         && !WarRoomManager.Instance.votTrecut.Value
                         && !WarRoomManager.Instance.votInCurs.Value;
            
            return activ ? "Press [Z] pentru a initia votul" : "Butonul de vot nu este activ";
        }
        if (col.GetComponent<DepozitLemn>() != null)
        {
            if (lemn.Value <= 0)
            {
                return "Nu ai ce depozita";
            }
            return "Press [Z] pentru a depozita lemn";
        }
        return null;
    }
    
    private void ActualizeazaPrompt()
    {
        if (UIManager.Instance == null || cameraCap == null)
        {
            return;
        }

        if (inZonaMagazin && !UIManager.Instance.esteInMagazin)
        {
            UIManager.Instance.ArataPrompt("Press [E] sa intri in magazin");
            return;
        }
        
        Ray ray = new Ray(cameraCap.position, cameraCap.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distantaAdunare, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            string prompt = ObtinePromptInteractiune(hit.collider);
            if (!string.IsNullOrEmpty(prompt))
            {
                UIManager.Instance.ArataPrompt(prompt);
                return;
            }
        }
        UIManager.Instance.AscundePrompt();
    }
    
    public void OpresteVitezaImediat()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }
    }
}
