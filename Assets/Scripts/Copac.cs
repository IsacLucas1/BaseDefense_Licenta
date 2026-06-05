using Unity.Netcode;
using UnityEngine;

public class Copac : NetworkBehaviour
{
    [Header("Setari Copac")]
    public int lovituriNecesare = 3;
    public int lemnPerLovitura = 5;

    private NetworkVariable<int> viataCopac;

    private void Awake()
    {
        viataCopac = new NetworkVariable<int>(lovituriNecesare);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LovesteCopaculServerRPC(RpcParams rpcParams = default)
    {
        if (viataCopac.Value == 0)
        {
            return;
        }
        
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
        {
            if (client.PlayerObject != null)
            {
                // Validare server-side de distanta (maxim 5 metri)
                float distanta = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                if (distanta > 5f)
                {
                    Debug.LogWarning("Jucatorul e prea departe pentru a lovi copacul!");
                    return; 
                }
                BasePlayer jucator = client.PlayerObject.GetComponent<BasePlayer>();
                if (jucator != null)
                {
                    jucator.AdaugaLemn(lemnPerLovitura);
                }
            }
        }

        viataCopac.Value -= 1;
        if (viataCopac.Value <= 0)
        {
            EliminaCopacClientRpc();
        }
    }

    [ClientRpc]
    private void EliminaCopacClientRpc()
    {
        gameObject.SetActive(false);
    }
}
