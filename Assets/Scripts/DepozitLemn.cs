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
        ProceseazaLemnServerRpc(jucator.NetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ProceseazaLemnServerRpc(ulong idJucator, RpcParams rpcParams = default)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idJucator, out NetworkObject playerObject))
        {
            BasePlayer jucator = playerObject.GetComponent<BasePlayer>();
            if (jucator == null)
            {
                return;
            }
            
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
