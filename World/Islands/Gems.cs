using UnityEngine;

public class Gem : MonoBehaviour
{
    [Header("Value")]
    public int value = 1;

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("Rotation")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0); // 🔥 editable axis
    public float rotationSpeed = 180f; // degrees per second

    private bool collected = false;

    void Update()
    {
        RotateGem();
    }

    void RotateGem()
    {
        // 🔥 normalize so weird values don’t break speed
        Vector3 axis = rotationAxis.normalized;

        transform.Rotate(axis * rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag(playerTag))
        {
            collected = true;

            // 🔥 Update UI + effects
            GemManager.Instance.AddGems(value, transform.position);

            Destroy(gameObject);
        }
    }
}