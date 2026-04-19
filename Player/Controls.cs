using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class CubeJumpFlipController : MonoBehaviour
{
    public Transform cameraTransform;
    public TextMeshProUGUI faceText;
    public Animator animator;

    [Header("Particles")]
    public GameObject flipParticlePrefab;
    public LayerMask groundLayer;

    [Header("Slip Jump")]
    public float slipJumpDistance = 2f;
    public float slipJumpHeight = 1.2f;
    public float slipJumpDuration = 0.3f;

    [Header("Dice Face Mapping")]
    public int topFace = 1, bottomFace = 6, frontFace = 2, backFace = 5, rightFace = 3, leftFace = 4;

    [Header("Movement")]
    public float moveDuration = 0.2f;
    public float groundCheckDistance = 1.1f;

    [Header("Audio")]
    public float pitchLowRange = 0.8f;
    public float pitchHighRange = 1.2f;
    public AudioSource audioSource;
    public AudioClip flipSound;
    public AudioClip landSound;

    private Rigidbody rb;
    private PlayerControls controls;

    private Vector2 input;
    private bool isMoving;

    private bool isGrounded;
    private bool wasGrounded;
    private RaycastHit groundHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
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

            if (input.magnitude > 0.9f)
                StartCoroutine(SlipJump(dir));
            else
                StartCoroutine(JumpFlip(dir));
        }
    }

    // ---------------- GROUND ----------------

    void CheckGround()
    {
        wasGrounded = isGrounded;

        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            out groundHit,
            groundCheckDistance,
            groundLayer
        );

        if (!wasGrounded && isGrounded)
            OnLand();
    }

    void OnLand()
    {
        if (audioSource && landSound)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(landSound);
        }

        SpawnFlipParticles();
    }

    // ---------------- MOVEMENT ----------------

    IEnumerator JumpFlip(Vector3 direction)
    {
        StartMove();

        float size = transform.localScale.x;

        Vector3 pivot = transform.position + (Vector3.down * size / 2f) + (direction * size / 2f);
        Vector3 axis = Vector3.Cross(Vector3.up, direction);

        float elapsed = 0f;
        float lastAngle = 0f;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            float eased = t * t * (3f - 2f * t);

            float angle = Mathf.Lerp(0f, 90f, eased);
            float delta = angle - lastAngle;

            transform.RotateAround(pivot, axis, delta);

            lastAngle = angle;
            elapsed += Time.deltaTime;
            yield return null;
        }

        EndMove();
    }

    IEnumerator SlipJump(Vector3 direction)
    {
        StartMove();

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * slipJumpDistance;

        Vector3 axis = Vector3.Cross(Vector3.up, direction);

        float elapsed = 0f;
        float lastAngle = 0f;

        while (elapsed < slipJumpDuration)
        {
            float t = elapsed / slipJumpDuration;
            float eased = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
            pos.y += Mathf.Sin(eased * Mathf.PI) * slipJumpHeight;

            float angle = Mathf.Lerp(0f, 180f, eased);
            float delta = angle - lastAngle;

            transform.position = pos;
            transform.Rotate(axis, delta, Space.World);

            lastAngle = angle;
            elapsed += Time.deltaTime;
            yield return null;
        }

        EndMove();
    }

    void StartMove()
    {
        isMoving = true;
        animator.SetBool("isMoving", true);

        if (audioSource && flipSound)
        {
            audioSource.pitch = Random.Range(pitchLowRange, pitchHighRange);
            audioSource.PlayOneShot(flipSound);
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void EndMove()
    {
        SnapToGrid();

        int faceValue = GetBottomFaceValue();
        if (faceText) faceText.text = faceValue.ToString();

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isMoving = false;
        animator.SetBool("isMoving", false);
    }

    // ---------------- UTILS ----------------

    void SnapToGrid()
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), Mathf.Round(p.z));

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
        int detected = 0;

        float d;

        d = Vector3.Dot(transform.up, Vector3.down); if (d > maxDot) { maxDot = d; detected = topFace; }
        d = Vector3.Dot(-transform.up, Vector3.down); if (d > maxDot) { maxDot = d; detected = bottomFace; }
        d = Vector3.Dot(transform.forward, Vector3.down); if (d > maxDot) { maxDot = d; detected = frontFace; }
        d = Vector3.Dot(-transform.forward, Vector3.down); if (d > maxDot) { maxDot = d; detected = backFace; }
        d = Vector3.Dot(transform.right, Vector3.down); if (d > maxDot) { maxDot = d; detected = rightFace; }
        d = Vector3.Dot(-transform.right, Vector3.down); if (d > maxDot) { maxDot = d; detected = leftFace; }

        return detected;
    }

    void SpawnFlipParticles()
    {
        if (!isGrounded || flipParticlePrefab == null) return;

        GameObject fx = Instantiate(flipParticlePrefab, groundHit.point, Quaternion.identity);

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(fx, 2f);
    }

    Vector3 GetDirection(Vector2 input)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * input.y + right * input.x;

        return Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
            ? (dir.x > 0 ? Vector3.right : Vector3.left)
            : (dir.z > 0 ? Vector3.forward : Vector3.back);
    }
}