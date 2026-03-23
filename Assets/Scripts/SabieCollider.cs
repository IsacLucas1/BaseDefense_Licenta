using UnityEngine;

public class SabieCollider : MonoBehaviour
{
    public ConstructorPlayer constructorPlayer;
    
    private void OnTriggerEnter(Collider other)
    {
        if (constructorPlayer != null)
        {
            constructorPlayer.InamicLovit(other);
        }
    }
}
