using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFallHandler : MonoBehaviour
{
    private Rigidbody rb;
    private bool isFalling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void TriggerFall()
    {
        if (isFalling) return;

        isFalling = true;

        rb.isKinematic = false;
        rb.useGravity = true;

        // Optional: small nudge so it doesn’t feel stuck
        rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
    }
}