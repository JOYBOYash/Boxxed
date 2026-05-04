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

    [Header("UI Rotation Control")]
    public bool followCameraRotation = true;
    public Vector3 uiRotationOffset; // 🔥 FULL XYZ CONTROL

    public Image upArrow;
    public Image downArrow;
    public Image leftArrow;
    public Image rightArrow;

    float holdTimer = 0f;
    public float holdRepeatDelay = 0.18f; // time between moves while holding

    Vector2 currentHoldInput;

    [Range(0.1f, 5f)] public float idleBlinkSpeed = 2f;
    [Range(0.1f, 1f)] public float inactiveAlpha = 0.3f;

    private float blinkTimer;

    // ---------------- DASH FX ----------------
    [Header("Dash FX")]
    public float dashShakeIntensity = 0.15f;
    public float dashShakeDuration = 0.15f;

    public float dashZoomAmount = 1.5f;
    public float dashZoomDuration = 0.15f;

    private Vector3 camOriginalPos;

    [Header("Dash Trail")]
    public GameObject ghostPrefab;
    public float ghostSpawnDelay = 0.05f;
    public float ghostLifetime = 0.3f;

    [Header("Movement Smoothness")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ---------------- PARTICLES ----------------
    [Header("Particles")]
    public GameObject flipParticlePrefab;
    public LayerMask groundLayer;


        // ---------------- INPUT MODE ----------------
    [Header("Input Mode")]
    public bool useJoystick = false;   // toggle switch
    public bool useUIButtons = true;   // arrows

    private Vector2 bufferedInput;
    private bool hasBufferedInput;

    // ---------------- JOYSTICK ----------------
    [Header("Joystick")]
    public Vector2 joystickInput;

    [Header("Unity Joystick Pack")]
    public Joystick joystick;   // drag your joystick here
    [Range(0.1f, 1f)] public float joystickDeadZone = 0.2f;


public void ToggleInputMode()
{
    useJoystick = !useJoystick;
    useUIButtons = !useJoystick;

    ResetHold();
    UpdateInputModeVisuals();
}

    // ---------------- INPUT ----------------
    private bool upPressed, downPressed, leftPressed, rightPressed;

public void OnUpPressed() 
{ 
    upPressed = true; 
    currentHoldInput = Vector2.up;
    bufferedInput = Vector2.up;
    hasBufferedInput = true;
}

public void OnDownPressed() 
{ 
    downPressed = true; 
    currentHoldInput = Vector2.down;
    bufferedInput = Vector2.down;
    hasBufferedInput = true;
}

public void OnLeftPressed() 
{ 
    leftPressed = true; 
    currentHoldInput = Vector2.left;
    bufferedInput = Vector2.left;
    hasBufferedInput = true;
}

public void OnRightPressed() 
{ 
    rightPressed = true; 
    currentHoldInput = Vector2.right;
    bufferedInput = Vector2.right;
    hasBufferedInput = true;
}

public void OnUpReleased() { upPressed = false; ResetHold(); }
public void OnDownReleased() { downPressed = false; ResetHold(); }
public void OnLeftReleased() { leftPressed = false; ResetHold(); }
public void OnRightReleased() { rightPressed = false; ResetHold(); }

void ResetHold()
{
    holdTimer = 0f;
    currentHoldInput = Vector2.zero;
}

    // ---------------- MOVEMENT ----------------
    [Header("Movement")]
    public float moveDuration = 0.2f;
    public float slipJumpDistance = 2f;
    public float slipJumpHeight = 1.2f;
    public float slipJumpDuration = 0.3f;
    public float groundCheckDistance = 1.1f;


    [Header("Dice Faces")]
    public int topFace = 1, bottomFace = 6, frontFace = 2, backFace = 5, rightFace = 3, leftFace = 4;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip flipSound;
    public AudioClip landSound;

    // ---------------- DASH AUDIO ----------------
    [Header("Dash Audio")]
    public AudioClip dashSound;

    private Vector3 lastMoveDirection;

    private bool canPlayMoveSound = true;

    private Rigidbody rb;
    private PlayerControls controls;

    private Vector2 input;
    private bool isMoving;
    private bool isBoosting;

    private bool isGrounded, wasGrounded;
    private RaycastHit groundHit;

    // ---------------- SCALE FIX ----------------
    private Vector3 fixedScale;


    void Start()
    {
        UpdateInputModeVisuals();
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

        // 🔥 STEP 1: COLLECT INPUT (DO NOT EXECUTE HERE)
        if (useJoystick || useUIButtons)
        {
            HandleHoldInput(); // now ONLY buffers input
        }
        else
        {
            if (input != Vector2.zero)
            {
                TryTriggerMove(input); // also only buffers now
            }
        }

        // 🔥 STEP 2: SINGLE EXECUTION POINT (THIS IS THE IMPORTANT PART)
        if (!isMoving && !isBoosting && isGrounded && hasBufferedInput)
        {
            Vector2 next = bufferedInput;
            hasBufferedInput = false;

            ExecuteMove(next);
        }
    }


    void ExecuteMove(Vector2 dirInput)
    {
        Vector3 dir = GetDirection(dirInput);
        lastMoveDirection = dir;

        if (dirInput.magnitude > 0.9f)
            StartCoroutine(SlipJump(dir));
        else
            StartCoroutine(JumpFlip(dir));
    }

    // ---------------- UI RIG ----------------
    void UpdateUIRigPosition()
    {
        if (!uiRig) return;

        uiRig.position = transform.position + rigOffset;

        if (Camera.main != null)
        {
            if (followCameraRotation)
                uiRig.forward = Camera.main.transform.forward;

            uiRig.rotation *= Quaternion.Euler(uiRotationOffset);
        }
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

void HandleHoldInput()
{
    Vector2 rawInput = Vector2.zero;

    if (useJoystick && joystick != null)
    {
        Vector2 joy = new Vector2(joystick.Horizontal, joystick.Vertical);

        if (joy.magnitude >= joystickDeadZone)
        {
            rawInput =
                Mathf.Abs(joy.x) > Mathf.Abs(joy.y)
                ? new Vector2(Mathf.Sign(joy.x), 0)
                : new Vector2(0, Mathf.Sign(joy.y));
        }
    }
    else if (useUIButtons)
    {
        rawInput = currentHoldInput;
    }

    // 🔥 ONLY BUFFER FIRST INPUT (NOT EVERY FRAME)
    if (rawInput != Vector2.zero && !isMoving)
    {
        bufferedInput = rawInput;
        hasBufferedInput = true;
    }
}    void SetArrowAlpha(Image img, float a)
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

        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, groundCheckDistance, groundLayer);

        if (!wasGrounded && isGrounded)
            OnLand();
    }

    void OnLand()
    {
        if (audioSource && landSound)
            audioSource.PlayOneShot(landSound);

        SpawnFlipParticles();
    }

    // ---------------- MOVEMENT ----------------
    void TryTriggerMove(Vector2 dirInput)
    {
        bufferedInput = dirInput;
        hasBufferedInput = true;
    }

IEnumerator JumpFlip(Vector3 direction)
{
    StartMove();

    float size = transform.localScale.y; // assuming uniform scale and cube shape
    Vector3 pivot = transform.position + (Vector3.down * size / 2f) + (direction * size / 2f);
    Vector3 axis = Vector3.Cross(Vector3.up, direction);

    float elapsed = 0f;
    float lastAngle = 0f;

    while (elapsed < moveDuration)
    {
        float t = elapsed / moveDuration;
        float eased = movementCurve.Evaluate(t); // 🔥 smoother

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
        float eased = movementCurve.Evaluate(t);

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
IEnumerator TileBoost(int steps)
{
    isBoosting = true;
    isMoving = true;

    rb.isKinematic = true;

    if (audioSource && dashSound)
        audioSource.PlayOneShot(dashSound);

    StartCoroutine(DashCameraFX());
    StartCoroutine(DashZoomFX());
    StartCoroutine(SpawnGhostTrail());

    Vector3 startPos = SnapPosition(transform.position);
    transform.position = startPos;

    Vector3 dir = lastMoveDirection;
    Vector3 endPos = startPos + dir * steps;

    float duration = 0.12f * steps; // 🔥 faster = better feel
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float t = elapsed / duration;
        float eased = movementCurve.Evaluate(t);

        Vector3 pos = Vector3.Lerp(startPos, endPos, eased);
        pos.y += Mathf.Sin(eased * Mathf.PI) * 0.25f;

        transform.position = pos;

        elapsed += Time.deltaTime;
        yield return null;
    }

    SnapToGrid();

    rb.isKinematic = false;

    isBoosting = false;
    isMoving = false;

    animator.SetBool("isMoving", false);
}

void UpdateInputModeVisuals()
{
    if (uiRig != null)
        uiRig.gameObject.SetActive(useUIButtons);

    if (joystick != null)
        joystick.gameObject.SetActive(useJoystick);
}
IEnumerator DashCameraFX()
{
    if (Camera.main == null) yield break;

    Transform cam = Camera.main.transform;
    camOriginalPos = cam.localPosition;

    float elapsed = 0f;

    while (elapsed < dashShakeDuration)
    {
        float t = elapsed / dashShakeDuration;

        float strength = Mathf.Lerp(dashShakeIntensity, 0f, t);

        float x = Mathf.Sin(Time.time * 80f) * strength;
        float y = Mathf.Cos(Time.time * 90f) * strength;

        cam.localPosition = camOriginalPos + new Vector3(x, y, 0);

        elapsed += Time.deltaTime;
        yield return null;
    }

    cam.localPosition = camOriginalPos;
}
IEnumerator DashZoomFX()
{
    if (Camera.main == null) yield break;

    AdvancedCameraFollow camFollow = Camera.main.GetComponent<AdvancedCameraFollow>();
    if (camFollow == null) yield break;

    float originalZoom = camFollow.orthoZoom;
    float zoomed = originalZoom - 5f; // 🔥 REQUIRED

    float elapsed = 0f;

    // ZOOM IN
    while (elapsed < dashZoomDuration)
    {
        float t = elapsed / dashZoomDuration;
        float eased = t * t * (3f - 2f * t);

        camFollow.orthoZoom = Mathf.Lerp(originalZoom, zoomed, eased);

        elapsed += Time.deltaTime;
        yield return null;
    }

    elapsed = 0f;

    // ZOOM OUT
    while (elapsed < dashZoomDuration)
    {
        float t = elapsed / dashZoomDuration;
        float eased = t * t * (3f - 2f * t);

        camFollow.orthoZoom = Mathf.Lerp(zoomed, originalZoom, eased);

        elapsed += Time.deltaTime;
        yield return null;
    }

    camFollow.orthoZoom = originalZoom;
}

IEnumerator SpawnGhostTrail()
{
    if (ghostPrefab == null) yield break;

    while (isBoosting)
    {
        GameObject ghost = Instantiate(ghostPrefab, transform.position, transform.rotation);

        StartCoroutine(FadeGhost(ghost));

        yield return new WaitForSeconds(ghostSpawnDelay);
    }
}

IEnumerator FadeGhost(GameObject ghost)
{
    float elapsed = 0f;

    Renderer r = ghost.GetComponentInChildren<Renderer>();
    if (r == null) yield break;

    Material mat = r.material;
    Color start = mat.color;

    while (elapsed < ghostLifetime)
    {
        float t = elapsed / ghostLifetime;

        Color c = start;
        c.a = Mathf.Lerp(1f, 0f, t);
        mat.color = c;

        elapsed += Time.deltaTime;
        yield return null;
    }

    Destroy(ghost);
}
    void StartMove()
    {
        isMoving = true;
        animator.SetBool("isMoving", true);

        // 🔥 FIX AUDIO (no overlap, no cutoff)
        if (audioSource && flipSound)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(flipSound, 1f);
        }

        // 🔥 FIX PHYSICS ERROR (NO velocity set after kinematic)
        rb.isKinematic = true;
    }


    void ResetMoveSound() => canPlayMoveSound = true;

void EndMove()
{
    SnapToGrid();

    int bottom = GetBottomFaceValue();
    int top = 7 - bottom;

    if (faceText) faceText.text = top.ToString();

    bool boosted = CheckTileBoost(top);

    if (!boosted)
    {
        rb.isKinematic = false;
        isMoving = false;
        animator.SetBool("isMoving", false);
    }

    // 🔥 REAL HOLD CONTINUATION (THIS IS THE KEY FIX)
    if (!isBoosting)
    {
        Vector2 nextInput = Vector2.zero;

        // JOYSTICK
        if (useJoystick && joystick != null)
        {
            Vector2 joy = new Vector2(joystick.Horizontal, joystick.Vertical);

            if (joy.magnitude >= joystickDeadZone)
            {
                nextInput =
                    Mathf.Abs(joy.x) > Mathf.Abs(joy.y)
                    ? new Vector2(Mathf.Sign(joy.x), 0)
                    : new Vector2(0, Mathf.Sign(joy.y));
            }
        }
        // UI BUTTON HOLD
        else if (useUIButtons)
        {
            nextInput = currentHoldInput;
        }

        // 🔥 ONLY CONTINUE IF STILL HOLDING
        if (nextInput != Vector2.zero)
        {
            StartCoroutine(ContinueMoveNextFrame(nextInput));
        }
        else
        {
            hasBufferedInput = false; // 🔥 PREVENT EXTRA MOVE AFTER RELEASE
        }
    }
}

        IEnumerator ContinueMoveNextFrame(Vector2 inputDir)
        {
            yield return null; // 🔥 ONE FRAME DELAY (CRITICAL FIX)

            // Ensure still valid
            if (!isMoving && !isBoosting && isGrounded && inputDir != Vector2.zero)
            {
                ExecuteMove(inputDir);
            }
        }


    bool CheckTileBoost(int topFace)
    {
        if (!isGrounded) return false;

        DiceBoostTile tile = groundHit.collider.GetComponent<DiceBoostTile>();

        if (tile != null && tile.TryActivate(topFace))
        {
            StartCoroutine(TileBoost(tile.boostSteps));
            return true;
        }

        return false;
    }

    Vector3 SnapPosition(Vector3 p)
    {
        return new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), Mathf.Round(p.z));
    }

    void SnapToGrid()
    {
        Vector3 snapped = SnapPosition(transform.position);

        // 🔥 FORCE GROUND ALIGNMENT
        snapped.y = groundHit.point.y + (transform.localScale.y / 2f);

        transform.position = snapped;

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