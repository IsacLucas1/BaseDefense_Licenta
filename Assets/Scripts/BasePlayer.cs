using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BasePlayer : NetworkBehaviour
{
    [Header("Setari Miscare")]
    public float speed = 5f;

    [Header("Setari Camera")]
    public Transform cameraCap;
    public float mouseSensitivity = 200f;
    private float xRotation = 0f;

    private Rigidbody rb;
    protected bool isDead = false;
    
    public NetworkVariable<Vector3> respawnPosition = new NetworkVariable<Vector3>();
    private Quaternion respawnRotation;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
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
        }
    }

    private void DisableRemotePlayer()
    {
        if (cameraCap != null)
        {
            cameraCap.gameObject.SetActive(false);
            var listener = cameraCap.GetComponent<AudioListener>();
            if (listener) listener.enabled = false;
        }
    }
    
    protected virtual void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (isDead)
        {
            return;
        }
        
        HandleMouseLook();
        if (GetComponent<Health>().currentHealth.Value > 0)
        {
            if(Input.GetKeyDown(KeyCode.K))
            {
                RequestSelfDamageServerRpc();
            }
        }
        
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        if (isDead)
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
        
        Vector3 targetPosition = rb.position + movement.normalized * speed * Time.deltaTime;
        rb.MovePosition(targetPosition);
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraCap.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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
