using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SmoothCubeRollController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float rollDuration = 0.15f; // Time for one roll

    private Rigidbody rb;
    private PlayerControls controls;

    private Vector2 currentInput;
    private bool isRolling;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        rb.isKinematic = true; // IMPORTANT: we control movement manually

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        controls.Enable();

        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += ctx => currentInput = Vector2.zero;
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Disable();
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        currentInput = ctx.ReadValue<Vector2>();

        if (!isRolling && currentInput != Vector2.zero)
            StartCoroutine(RollLoop());
    }

    IEnumerator RollLoop()
    {
        while (currentInput != Vector2.zero)
        {
            if (!isRolling)
            {
                Vector3 dir = GetCameraRelativeDirection(currentInput);
                yield return StartCoroutine(SmoothRoll(dir));
            }

            yield return null;
        }
    }

IEnumerator SmoothRoll(Vector3 direction)
{
    isRolling = true;

    float size = transform.localScale.x;

    Vector3 pivot =
        transform.position +
        (Vector3.down * size / 2f) +
        (direction * size / 2f);

    Vector3 axis = Vector3.Cross(Vector3.up, direction);

    float elapsed = 0f;
    float lastAngle = 0f;

    while (elapsed < rollDuration)
    {
        float t = elapsed / rollDuration;

        // ✅ Smooth but still physical
        float easedT = t * t * (3f - 2f * t); // smoothstep (safe)

        float currentAngle = Mathf.Lerp(0f, 90f, easedT);

        // 🔥 ONLY rotate the delta (this is the fix)
        float deltaAngle = currentAngle - lastAngle;

        transform.RotateAround(pivot, axis, deltaAngle);

        lastAngle = currentAngle;

        elapsed += Time.deltaTime;
        yield return null;
    }

    // 🔥 Finish remaining angle cleanly
    float remaining = 90f - lastAngle;
    transform.RotateAround(pivot, axis, remaining);

    // Snap rotation
    transform.rotation = Quaternion.Euler(
        Mathf.Round(transform.eulerAngles.x / 90f) * 90f,
        Mathf.Round(transform.eulerAngles.y / 90f) * 90f,
        Mathf.Round(transform.eulerAngles.z / 90f) * 90f
    );

    // Snap position
    Vector3 p = transform.position;
    transform.position = new Vector3(
        Mathf.Round(p.x),
        Mathf.Round(p.y),
        Mathf.Round(p.z)
    );

    isRolling = false;
}


    Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * input.y + right * input.x;

        // Snap to 4 directions (same as your logic)
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0 ? Vector3.right : Vector3.left;
        else
            return dir.z > 0 ? Vector3.forward : Vector3.back;
    }
}