using UnityEngine;

public class Gem : MonoBehaviour
{
    public int value = 1;
    public string playerTag = "Player";

    private bool collected = false;

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag(playerTag))
        {
            collected = true;

            // 🔥 Update UI
            GemManager.Instance.AddGems(value);

            // 🔥 Optional: add particle/sound later

            Destroy(gameObject);
        }
    }
}