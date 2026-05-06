using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;

    [Header("Target")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
            }

            // Destroy(gameObject);
        }
    }
}