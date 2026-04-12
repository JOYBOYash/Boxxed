using UnityEngine;

public class EdgeFallTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerFallHandler player = other.GetComponent<PlayerFallHandler>();

        if (player != null)
        {
            player.TriggerFall();
        }
    }
}