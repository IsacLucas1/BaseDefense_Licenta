using UnityEngine;

public class ReadyZonaClasa : MonoBehaviour
{
    [Tooltip("0=Tank, 1=Spion, 2=Constructor, 3=Medic, 4=Arcas")]
    public int indexClasa;

    private void OnTriggerEnter(Collider other)
    {
        BasePlayer player = other.GetComponent<BasePlayer>();
        if (player == null || !player.IsOwner)
        {
            return;
        }

        if (FinalAttackManager.GetIndexClasa(player) != indexClasa)
        {
            return;
        }

        Vector3 centruZona = transform.position;
        Vector3 destinatie = new Vector3(centruZona.x, player.transform.position.y, centruZona.z);
        player.TeleporteazaInReadyZone(destinatie);
        
        player.OpresteMiscarea();            
        player.SeteazaRotireBlocata(true);   
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ArataPanouReadyCheck(true);
        }
    }
}