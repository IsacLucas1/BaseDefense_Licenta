using Unity.Netcode;
using UnityEngine;

public class Copac : NetworkBehaviour
{
    [Header("Setari Copac")]
    public int lovituriNecesare = 3;
    public int lemnPerLovitura = 5;

    private NetworkVariable<int> viataCopac;

    // Initializarea vietii la apelarea metodei Awake
    private void Awake()
    {
        viataCopac = new NetworkVariable<int>(lovituriNecesare);
    }
    
    // Cerere Remote Procedure Call (RPC) de la orice client la server pentru a lovi copacul
    // Procesează lovitura unui jucător asupra copacului, adauga lemnul, scade viata copacului
    // si verifica starea copacului
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LovesteCopaculServerRPC(RpcParams rpcParams = default)
    {
        if (viataCopac.Value == 0)
        {
            return;
        }
        
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
                // Adauga lemn jucatorului care a lovit copacul
                BasePlayer jucator = client.PlayerObject.GetComponent<BasePlayer>();
                if (jucator != null)
                {
                    jucator.AdaugaLemn(lemnPerLovitura);
                }
            }
        }

        viataCopac.Value -= 1;
        // Daca nu mai are viata se ascunde copacul 
        if (viataCopac.Value <= 0)
        {
            EliminaCopacClientRpc();
        }
    }

    // Ascunde copacul pe toti clientii 
    [ClientRpc]
    private void EliminaCopacClientRpc()
    {
        gameObject.SetActive(false);
    }
}
