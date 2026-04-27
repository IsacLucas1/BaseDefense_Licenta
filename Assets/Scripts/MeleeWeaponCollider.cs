using UnityEngine;

public class MeleeWeaponCollider : MonoBehaviour
{
    public MeleePlayer meleePlayer;
    
    private void OnTriggerEnter(Collider other)
    {
        if (meleePlayer != null)
        {
            meleePlayer.InamicLovit(other);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (meleePlayer != null)
        {
            meleePlayer.InamicLovit(other);
        }
    }
}
