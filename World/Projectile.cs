using UnityEngine;
using System.Collections;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;

    [Header("Target")]
    public string playerTag = "Player";

    [Header("Hit Cooldown")]
    public float damageCooldown = 0.5f;

    private bool canDamage = true;

    void OnTriggerEnter(Collider other)
    {
        if (!canDamage)
            return;

        if (other.CompareTag(playerTag))
        {
            PlayerHealth health =
                other.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);

                StartCoroutine(DamageCooldown());
            }

            // Destroy(gameObject);
        }
    }

    IEnumerator DamageCooldown()
    {
        canDamage = false;

        yield return new WaitForSeconds(damageCooldown);

        canDamage = true;
    }
}