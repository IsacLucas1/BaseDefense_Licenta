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

    // Abonare si dezabonare la evenimentul de schimbare a cantitatii de lemn stocat
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
    
    // Actualizeaza textul UI cu cantitatea de lemn stocata
    private void ActualizeazaText(int valoareVeche, int valoareNoua)
    {
        if (textCantitateLemn != null)
        {
            textCantitateLemn.text = "<sprite=0> : " + valoareNoua;
        }
    }
    
    // Asigura ca UI-ul depozitului de lemn se roteste pentru a fi intotdeauna vizibil pentru jucator
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
        // Obtine ID-ul clientului care a trimis RPC-ul
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
        {
            if (client.PlayerObject != null)
            {
                // Validare server-side de distanta pentru a preveni cheat-ul
                float distanta = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                if (distanta > 5f)
                {
                    return; 
                }
                
                BasePlayer jucator = client.PlayerObject.GetComponent<BasePlayer>();
                if (jucator == null)
                {
                    return;
                }
                
                // Verifica daca jucatorul este de tip ConstructorPlayer pentru a decide daca depoziteaza sau ia lemn
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
