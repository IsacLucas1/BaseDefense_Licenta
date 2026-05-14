using UnityEngine;
using Unity.Netcode;

public class ShopManager : MonoBehaviour
{
    private BasePlayer GetLocalPlayer()
    {
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<BasePlayer>();
        }
        return null;
    }

    public void Buton_UpgradeViteza()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(1);
    }

    public void Buton_UpgradeDamage()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(2);
    }

    public void Buton_PlusLemn()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(3);
    }

    public void Buton_PlusViata()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(4);
    }

    public void Buton_PlusViataMaxima()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(5);
    }

    public void Buton_BonusViteza()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(6);
    }

    public void Buton_BonusDamage()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(7);
    }

    public void Buton_BonusTaiereLemn()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(8);
    }

    public void Buton_BonusViataZiduri()
    {
        GetLocalPlayer()?.CumparaUpgradeServerRpc(9);
    }
    public void Buton_UpgradeSpecialClasa()
    {
        GetLocalPlayer()?.CumparaUpgradeClasaServerRpc();
    }
}