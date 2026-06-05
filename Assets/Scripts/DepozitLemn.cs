using Unity.Netcode;
using UnityEngine;
using TMPro;

public class DepozitLemn : NetworkBehaviour
{
    [Header("Depozit Lemn - UI")]
    public TextMeshProUGUI textCantitateLemn;
    public GameObject canvasDepozitLemn;
    
    public NetworkVariable<int> lemnStocat = new NetworkVariable<int>(0);
    private Camera cam;

    public override void OnNetworkSpawn()
    {
        lemnStocat.OnValueChanged += ActualizeazaText;
        ActualizeazaText(0, lemnStocat.Value);
        
        cam = Camera.main;
    }
    
    public override void OnNetworkDespawn()
    {
        lemnStocat.OnValueChanged -= ActualizeazaText;
    }
    
    private void ActualizeazaText(int valoareVeche, int valoareNoua)
    {
        if (textCantitateLemn != null)
        {
            textCantitateLemn.text = "<sprite=0> : " + valoareNoua;
        }
    }
    
    private void LateUpdate()
    {
        if (canvasDepozitLemn != null)
        {
            if (cam == null) cam = Camera.main;
            
            if (cam != null)
            {
                canvasDepozitLemn.transform.rotation = cam.transform.rotation;
            }
        }
    }
    
    public void IncearcaDepozitareLemn(BasePlayer jucator)
    {
        ProceseazaLemnServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ProceseazaLemnServerRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
        {
            if (client.PlayerObject != null)
            {
                // Validare server-side de distanta
                float distanta = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                if (distanta > 5f)
                {
                    Debug.LogWarning("Jucatorul e prea departe de depozit!");
                    return; 
                }
                BasePlayer jucator = client.PlayerObject.GetComponent<BasePlayer>();
                if (jucator == null) return;
                
                ConstructorPlayer constructorPlayer = jucator.GetComponent<ConstructorPlayer>();
                if (constructorPlayer != null)
                {
                    if (lemnStocat.Value > 0)
                    {
                        jucator.lemn.Value += lemnStocat.Value;
                        lemnStocat.Value = 0;
                    }
                }
                else
                {
                    if (jucator.lemn.Value > 0)
                    {
                        lemnStocat.Value += jucator.lemn.Value;
                        jucator.lemn.Value = 0;
                    }
                }
            }
        }
    }
}
