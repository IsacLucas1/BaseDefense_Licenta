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
    
    [ServerRpc(RequireOwnership = false)]
    public void LovesteCopaculServerRPC(ulong jucatorID)
    {
        if (viataCopac.Value == 0)
        {
            return;
        }
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(jucatorID, out NetworkObject jucatorObj))
        {
            BasePlayer jucator = jucatorObj.GetComponent<BasePlayer>();
            if (jucator != null)
            {
                jucator.AdaugaLemn(lemnPerLovitura);
            }
        }

        viataCopac.Value -= 1;
        if (viataCopac.Value <= 0)
        {
             GetComponent<NetworkObject>().Despawn(false);  
             gameObject.SetActive(false);
        }
    }
}
