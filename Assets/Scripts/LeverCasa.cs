using UnityEngine;
using Unity.Netcode;

public class LeverCasa : NetworkBehaviour
{
    public NetworkVariable<bool> aFostTras = new NetworkVariable<bool>(false);
    [HideInInspector] public TipCasa tipulCasei = TipCasa.Nimic; 

    [Header("Efecte Vizuale")]
    public Renderer meshManeta;
    public Color culoareTras = Color.gray;

    public override void OnNetworkSpawn()
    {
        aFostTras.OnValueChanged += (vechi, nou) => { if (nou) SchimbaCuloareManeta(); };
        if (aFostTras.Value) SchimbaCuloareManeta();
    }

    private void SchimbaCuloareManeta()
    {
        if (meshManeta != null) meshManeta.material.color = culoareTras;
    }

    // Functia apelata de Raycast-ul Spionului
    public void IncearcaTragere()
    {
        if (!aFostTras.Value)
        {
            TrageManetaServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TrageManetaServerRpc(RpcParams rpcParams = default)
    {
        if (aFostTras.Value)
        {
            return;
        }

        ulong spionId = rpcParams.Receive.SenderClientId;
        
        aFostTras.Value = true;
        HouseManager.Instance.ProceseazaManeta(tipulCasei, spionId, transform.position); 
    }
}