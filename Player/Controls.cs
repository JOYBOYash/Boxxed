using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
using TMPro;



[RequireComponent(typeof(Rigidbody))]
public class CubeJumpFlipController : MonoBehaviour
{
    public Transform cameraTransform;

    public TextMeshProUGUI faceText; // assign in inspector

    [Header("Dice Face Mapping (Editable in Inspector)")]

    public int topFace = 1;
    public int bottomFace = 6;
    public int frontFace = 2;
    public int backFace = 5;
    public int rightFace = 3;
    public int leftFace = 4;

    

    [Header("Movement")]
    public float moveDistance = 1f;
    public float jumpHeight = 0.5f;
    public float moveDuration = 0.2f;

    private bool isGrounded;
    public float groundCheckDistance = 1.1f;

    private float inputBufferTime = 0.15f;
    private float lastInputTime;

    private Rigidbody rb;
    private PlayerControls controls;

    private Vector2 input;
    private bool isMoving;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        controls.Enable();

        controls.Player.Move.performed += ctx => input = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => input = Vector2.zero;
    }

    void OnDisable()
    {
        controls.Disable();
    }

    void Update()
    {
        CheckGround();

        if (!isMoving && isGrounded && input != Vector2.zero)
        {
            Vector3 dir = GetDirection(input);
            StartCoroutine(JumpFlip(dir));
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance
        );
    }

IEnumerator JumpFlip(Vector3 direction)
{
    if (!isGrounded)
    {
        isMoving = false;
        yield break;
    }

    isMoving = true;

    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
    rb.isKinematic = true;

    float elapsed = 0f;
    float size = transform.localScale.x;

    Vector3 pivot =
        transform.position +
        (Vector3.down * size / 2f) +
        (direction * size / 2f);

    Vector3 axis = Vector3.Cross(Vector3.up, direction);

    float lastAngle = 0f;

    Vector3 basePos = transform.position;
    Quaternion baseRot = transform.rotation;

    while (elapsed < moveDuration)
    {
        float t = elapsed / moveDuration;
        float eased = t * t * (3f - 2f * t);

        float currentAngle = Mathf.Lerp(0f, 90f, eased);
        float delta = currentAngle - lastAngle;

        // 🔥 Compute rotation manually (no cumulative drift)
        Quaternion rot = Quaternion.AngleAxis(currentAngle, axis) * baseRot;

        Vector3 pos =
            Quaternion.AngleAxis(currentAngle, axis) * (basePos - pivot) + pivot;

        // 🔥 SAFE jump arc (added AFTER rotation calc)
        float hop = Mathf.Sin(eased * Mathf.PI) * 0.15f;
        pos += Vector3.up * hop;

        transform.position = pos;
        transform.rotation = rot;

        lastAngle = currentAngle;

        elapsed += Time.deltaTime;
        yield return null;
    }

    SnapToGrid();

    int faceValue = GetBottomFaceValue();

    if (faceText != null)
        faceText.text = faceValue.ToString();

    // ✅ Restore physics cleanly
    rb.isKinematic = false;

    // Reset again to avoid carry-over
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    isMoving = false;
}
    
    void SnapToGrid()
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(
            Mathf.Round(p.x),
            Mathf.Round(p.y),
            Mathf.Round(p.z)
        );

        Vector3 r = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(
            Mathf.Round(r.x / 90f) * 90f,
            Mathf.Round(r.y / 90f) * 90f,
            Mathf.Round(r.z / 90f) * 90f
        );

        rb.position = transform.position;
        rb.rotation = transform.rotation;
    }

int GetBottomFaceValue()
{
    float maxDot = -Mathf.Infinity;
    int detectedFace = 0;

    // Check all 6 directions manually

    float d;

    d = Vector3.Dot(transform.up, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = topFace; }

    d = Vector3.Dot(-transform.up, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = bottomFace; }

    d = Vector3.Dot(transform.forward, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = frontFace; }

    d = Vector3.Dot(-transform.forward, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = backFace; }

    d = Vector3.Dot(transform.right, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = rightFace; }

    d = Vector3.Dot(-transform.right, Vector3.down);
    if (d > maxDot) { maxDot = d; detectedFace = leftFace; }

    return detectedFace;
}
Vector3 GetDirection(Vector2 input)
{
    Vector3 forward = cameraTransform.forward;
    Vector3 right = cameraTransform.right;

    forward.y = 0;
    right.y = 0;

    forward.Normalize();
    right.Normalize();

    Vector3 rawDir = forward * input.y + right * input.x;

    // 🔥 HARD SNAP (no diagonals EVER)
    if (Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.z))
        return rawDir.x > 0 ? Vector3.right : Vector3.left;
    else
        return rawDir.z > 0 ? Vector3.forward : Vector3.back;
}
}