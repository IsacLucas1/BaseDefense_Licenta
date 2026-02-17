using UnityEngine;
using Unity.Netcode;

public class BasePlayer : NetworkBehaviour
{
    [Header("Setari Miscare")]
    public float speed = 5f;

    [Header("Setari Camera")]
    public Transform cameraCap;
    public float mouseSensitivity = 200f;
    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            DisableRemotePlayer();
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
        if (!IsOwner) return;
        HandleMovement();
        HandleMouseLook();

        if (GetComponent<Health>().currentHealth.Value > 0)
        {
             if(Input.GetKeyDown(KeyCode.K))
             {
                 RequestSelfDamageServerRpc();
             }
        }
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
        
        transform.position += movement.normalized * speed * Time.deltaTime;
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
}
