using UnityEngine;

public class ConstructorAnimReceiver : MonoBehaviour
{
    private MeleePlayer meleePlayer;
        
        void Start()
        {
            meleePlayer = GetComponentInParent<MeleePlayer>();
        }
        
        public void TriggerLovitura()
        {
            if (meleePlayer != null)
            {
                meleePlayer.ExecutaLovituraDinAnimatie();
            }
        }
}
