using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class CubeJumpFlipController : MonoBehaviour
{
    public Transform cameraTransform;
    public TextMeshProUGUI faceText;
    public Animator animator;

    // ---------------- UI RIG ----------------
    [Header("UI Direction Arrows")]
    public Transform uiRig;
    public Vector3 rigOffset = new Vector3(0, 2f, 0);

    public Image upArrow;
    public Image downArrow;
    public Image leftArrow;
    public Image rightArrow;

    [Range(0.1f, 5f)] public float idleBlinkSpeed = 2f;
    [Range(0.1f, 1f)] public float inactiveAlpha = 0.3f;

    private float blinkTimer;

    // ---------------- PARTICLES ----------------
    [Header("Particles")]
    public GameObject flipParticlePrefab;
    public LayerMask groundLayer;

    // ---------------- INPUT ----------------
    [Header("Mobile Input")]
    private bool upPressed, downPressed, leftPressed, rightPressed;

    public void OnUpPressed() { upPressed = true; TryTriggerMove(Vector2.up); }
    public void OnDownPressed() { downPressed = true; TryTriggerMove(Vector2.down); }
    public void OnLeftPressed() { leftPressed = true; TryTriggerMove(Vector2.left); }
    public void OnRightPressed() { rightPressed = true; TryTriggerMove(Vector2.right); }

    public void OnUpReleased() => upPressed = false;
    public void OnDownReleased() => downPressed = false;
    public void OnLeftReleased() => leftPressed = false;
    public void OnRightReleased() => rightPressed = false;

    // ---------------- MOVEMENT ----------------
    [Header("Slip Jump")]
    public float slipJumpDistance = 2f;
    public float slipJumpHeight = 1.2f;
    public float slipJumpDuration = 0.3f;

    [Header("Movement")]
    public float moveDuration = 0.2f;
    public float groundCheckDistance = 1.1f;

    // 🔥 FIXED SIZE SYSTEM
    [Header("Dice Size")]
    public float diceSize = 1f;

    // ---------------- DICE ----------------
    [Header("Dice Faces")]
    public int topFace = 1, bottomFace = 6, frontFace = 2, backFace = 5, rightFace = 3, leftFace = 4;

    // ---------------- AUDIO ----------------
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip flipSound;
    public AudioClip landSound;

    private bool canPlayMoveSound = true;

    // ---------------- INTERNAL ----------------
    private Rigidbody rb;
    private PlayerControls controls;

    private Vector2 input;
    private bool isMoving;

    private bool isGrounded, wasGrounded;
    private RaycastHit groundHit;

    // 🔥 SCALE FIX (INSPECTOR WORKS NOW)
    void OnValidate()
    {
        transform.localScale = Vector3.one * diceSize;
    }

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
        HandleUI();
        UpdateUIRigPosition();

        if (!isMoving && isGrounded && input != Vector2.zero)
        {
            TryTriggerMove(input);
        }
    }

    int GetTopFaceValue(int bottom) => 7 - bottom;

    // ---------------- UI ----------------
    void UpdateUIRigPosition()
    {
        if (!uiRig) return;

        uiRig.position = transform.position + rigOffset;

        if (Camera.main != null)
            uiRig.forward = Camera.main.transform.forward;
    }

    void HandleUI()
    {
        bool anyInput = upPressed || downPressed || leftPressed || rightPressed || input != Vector2.zero;

        if (!anyInput)
        {
            blinkTimer += Time.deltaTime * idleBlinkSpeed;
            float alpha = Mathf.Abs(Mathf.Sin(blinkTimer));

            SetArrowAlpha(upArrow, alpha);
            SetArrowAlpha(downArrow, alpha);
            SetArrowAlpha(leftArrow, alpha);
            SetArrowAlpha(rightArrow, alpha);
        }
        else
        {
            SetArrowAlpha(upArrow, upPressed ? 1f : inactiveAlpha);
            SetArrowAlpha(downArrow, downPressed ? 1f : inactiveAlpha);
            SetArrowAlpha(leftArrow, leftPressed ? 1f : inactiveAlpha);
            SetArrowAlpha(rightArrow, rightPressed ? 1f : inactiveAlpha);
        }
    }

    void SetArrowAlpha(Image img, float a)
    {
        if (!img) return;
        var c = img.color;
        c.a = a;
        img.color = c;
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
            audioSource.PlayOneShot(landSound);

        SpawnFlipParticles();
    }

    // ---------------- INPUT ----------------
    void TryTriggerMove(Vector2 dirInput)
    {
        if (isMoving || !isGrounded) return;

        Vector3 dir = GetDirection(dirInput);

        if (dirInput.magnitude > 0.9f)
            StartCoroutine(SlipJump(dir));
        else
            StartCoroutine(JumpFlip(dir));
    }

    // ---------------- MOVEMENT ----------------
    IEnumerator JumpFlip(Vector3 direction)
    {
        StartMove();

        float size = diceSize;
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

    // 🔥 NEW DASH BOOST (REPLACES GLITCHY ROLL)
    IEnumerator TileBoost(int steps)
    {
        isMoving = true;

        // THEN switch to kinematic
        rb.isKinematic = true;

        Vector3 dir = GetDirection(Vector2.right);

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dir * steps;

        float duration = 0.15f * steps;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
            pos.y += Mathf.Sin(eased * Mathf.PI) * 0.2f;

            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        SnapToGrid();

        rb.isKinematic = false;

        isMoving = false;
    }

    void StartMove()
    {
        isMoving = true;
        animator.SetBool("isMoving", true);

        if (audioSource && flipSound && canPlayMoveSound)
        {
            audioSource.clip = flipSound;
            audioSource.Play();
            canPlayMoveSound = false;
            Invoke(nameof(ResetMoveSound), flipSound.length);
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void ResetMoveSound() => canPlayMoveSound = true;

    void EndMove()
    {
        SnapToGrid();

        int bottom = GetBottomFaceValue();
        int top = GetTopFaceValue(bottom);

        if (faceText) faceText.text = top.ToString();

        CheckTileBoost(top);

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isMoving = false;
        animator.SetBool("isMoving", false);
    }

    void CheckTileBoost(int topFace)
    {
        if (!isGrounded) return;

        DiceBoostTile tile = groundHit.collider.GetComponent<DiceBoostTile>();

        if (tile != null && tile.TryActivate(topFace))
        {
            StartCoroutine(TileBoost(tile.boostSteps));
        }
    }

    void SnapToGrid()
    {
        // 🔥 Position snap (perfect grid alignment)
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x);
        p.y = Mathf.Round(p.y);
        p.z = Mathf.Round(p.z);

        transform.position = p;

        // 🔥 Rotation snap (clean dice faces)
        Vector3 r = transform.eulerAngles;
        r.x = Mathf.Round(r.x / 90f) * 90f;
        r.y = Mathf.Round(r.y / 90f) * 90f;
        r.z = Mathf.Round(r.z / 90f) * 90f;

        transform.rotation = Quaternion.Euler(r);

        // 🔥 Sync Rigidbody (IMPORTANT)
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