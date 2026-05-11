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
    private int moveVersion = 0;
    public float dashZoomAmount = 1.5f;
    public float dashZoomDuration = 0.15f;

    private bool ignoreNextFrameInput = false;
    private bool ignoreInputAfterResume = false;
    private Vector3 camOriginalPos;

    private Coroutine dashZoomRoutine;

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

    private float baseGameplayZoom;


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

    private bool blockMoveContinuation = false;


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

    // ---------------- SWIPE INPUT ----------------
    [Header("Swipe Input")]
    public bool enableSwipeInput = true;
    public float swipeThreshold = 50f;

    private Vector2 swipeStartPos;
    private bool isSwiping;

    // ---------------- UI SCALE ----------------
    [Header("Arrow Scale FX")]
    public float activeScale = 1.25f;
    public float normalScale = 1f;
    public float scaleSpeed = 10f;


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

    private float inputBlockTimer = 0f;
    public float inputBlockDuration = 0.15f; // tweak if needed

    // ---------------- SCALE FIX ----------------
    private Vector3 fixedScale;


    void Start()
    {
        if (baseGameplayZoom <= 0f)
        {
            baseGameplayZoom = 25f;
        }
        UpdateInputModeVisuals();
    }

    public void SetBaseZoom(float zoom)
{
    baseGameplayZoom = zoom;
}

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controls = new PlayerControls();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void OnEnable()
    {
        controls.Enable();
         controls.Player.Move.performed += ctx =>
        {
            if (inputBlockTimer > 0f) return; // 🔥 BLOCK INPUT
            input = ctx.ReadValue<Vector2>();
        };

        controls.Player.Move.canceled += ctx =>
        {
            if (inputBlockTimer > 0f) return; // 🔥 BLOCK INPUT
            input = Vector2.zero;
        };
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

    if (ignoreInputAfterResume)
    {
        inputBlockTimer -= Time.deltaTime;

        if (inputBlockTimer <= 0f)
        {
            ignoreInputAfterResume = false;
        }
        else
        {
            return;
        }
    }

    // 🔥 STEP 2: GATHER INPUT
    if (inputBlockTimer <= 0f)
    {
        HandleSwipeInput();
        HandleHoldInput();
    }

    // 🔥 STEP 3: EXECUTE
    if (!isMoving && !isBoosting && isGrounded && hasBufferedInput)
    {
        Vector2 next = bufferedInput;
        hasBufferedInput = false;
        ExecuteMove(next);
    }

    // (Rest of your Anti-sink logic and timer remains the same)
    if (inputBlockTimer > 0f) inputBlockTimer -= Time.deltaTime;
}
    void HandleSwipeInput()
    {
        if (!enableSwipeInput) return;

        // 🔥 TOUCH SUPPORT (mobile)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                swipeStartPos = touch.position.ReadValue();
                isSwiping = true;
            }

            if (isSwiping)
            {
                Vector2 current = touch.position.ReadValue();
                Vector2 delta = current - swipeStartPos;

                if (delta.magnitude > swipeThreshold)
                {
                    Vector2 dir =
                        Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? new Vector2(Mathf.Sign(delta.x), 0)
                        : new Vector2(0, Mathf.Sign(delta.y));

                    currentHoldInput = dir;
                    bufferedInput = dir;
                    hasBufferedInput = true;
                }
            }
        }
        else
        {
            // 🔥 MOUSE SUPPORT (Editor / PC testing)
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    swipeStartPos = Mouse.current.position.ReadValue();
                    isSwiping = true;
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    isSwiping = false;
                }

                if (isSwiping)
                {
                    Vector2 current = Mouse.current.position.ReadValue();
                    Vector2 delta = current - swipeStartPos;

                    if (delta.magnitude > swipeThreshold)
                    {
                        Vector2 dir =
                            Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                            ? new Vector2(Mathf.Sign(delta.x), 0)
                            : new Vector2(0, Mathf.Sign(delta.y));

                        currentHoldInput = dir;
                        bufferedInput = dir;
                        hasBufferedInput = true;
                    }
                }
            }
        }

        // if (touch.press.wasReleasedThisFrame)
        // {
        //     isSwiping = false;
        //     currentHoldInput = Vector2.zero;
        // }
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
        bool anyInput = upPressed || downPressed || leftPressed || rightPressed 
                        || input != Vector2.zero 
                        || currentHoldInput != Vector2.zero;

        if (!anyInput)
        {
            blinkTimer += Time.deltaTime * idleBlinkSpeed;
            float alpha = Mathf.Abs(Mathf.Sin(blinkTimer));

            // 🔥 idle = no active arrow
            SetArrowVisual(upArrow, false, alpha);
            SetArrowVisual(downArrow, false, alpha);
            SetArrowVisual(leftArrow, false, alpha);
            SetArrowVisual(rightArrow, false, alpha);
        }
        else
        {
            SetArrowVisual(upArrow, currentHoldInput == Vector2.up, 1f);
            SetArrowVisual(downArrow, currentHoldInput == Vector2.down, 1f);
            SetArrowVisual(leftArrow, currentHoldInput == Vector2.left, 1f);
            SetArrowVisual(rightArrow, currentHoldInput == Vector2.right, 1f);
        }
    }

    void SetArrowVisual(Image img, bool isActive, float alphaOverride)
    {
        if (!img) return;

        // 🔥 SCALE
        float targetScale = isActive ? activeScale : normalScale;

        img.transform.localScale = Vector3.Lerp(
            img.transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * scaleSpeed
        );

        // 🔥 ALPHA
        Color c = img.color;

        if (isActive)
            c.a = 1f;
        else
            c.a = alphaOverride * inactiveAlpha;

        img.color = c;
    }

void HandleHoldInput()
{
    Vector2 rawInput = Vector2.zero;

    if (useJoystick && joystick != null)
    {
        Vector2 joy = new Vector2(joystick.Horizontal, joystick.Vertical);

        if (joy.magnitude < joystickDeadZone)
        {
            joy = Vector2.zero;
        }
        else
        {
            joy = joy.normalized;
        }

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

    float halfHeight = transform.localScale.y * 0.5f;

    // 🔥 start slightly ABOVE center to avoid starting inside collider
    Vector3 origin = transform.position + Vector3.up * 0.2f;

    // 🔥 use SphereCast instead of Raycast (MUCH more stable)
    isGrounded = Physics.SphereCast(
        origin,
        0.25f, // radius
        Vector3.down,
        out groundHit,
        groundCheckDistance + halfHeight,
        groundLayer
    );

    if (!wasGrounded && isGrounded)
    {
        OnLand();
    }
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
    ForceGroundSnapAndFX();
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
    ForceGroundSnapAndFX();
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

public void UpdateInputModeVisuals()
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
    if (Camera.main == null)
        yield break;

    AdvancedCameraFollow camFollow =
        Camera.main.GetComponent<AdvancedCameraFollow>();

    if (camFollow == null)
        yield break;

    // 🔥 STOP OLD DASH ZOOM
    if (dashZoomRoutine != null)
    {
        StopCoroutine(dashZoomRoutine);
    }

    dashZoomRoutine =
        StartCoroutine(
            DashZoomRoutine(camFollow)
        );

    yield return dashZoomRoutine;
}

IEnumerator DashZoomRoutine(
    AdvancedCameraFollow camFollow
)
{
    // 🔥 SAFE VALUES
    float gameplayZoom =
        Mathf.Max(baseGameplayZoom, 8f);

    float dashZoom =
        Mathf.Clamp(
            gameplayZoom - dashZoomAmount,
            5f,
            20f
        );

    // 🔥 ZOOM IN
    camFollow.ZoomTo(
        dashZoom,
        dashZoomDuration
    );

    yield return new WaitForSecondsRealtime(
        dashZoomDuration
    );

    // 🔥 RESET BACK
    camFollow.ZoomTo(
        gameplayZoom,
        dashZoomDuration
    );

    yield return new WaitForSecondsRealtime(
        dashZoomDuration
    );

    // 🔥 FORCE FINAL RESET
    camFollow.SetZoom(gameplayZoom);

    dashZoomRoutine = null;
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
        if (!blockMoveContinuation && nextInput != Vector2.zero)
        {
            StartCoroutine(ContinueMoveNextFrame(nextInput, moveVersion));
        }
        else
        {
            hasBufferedInput = false;
        }
    }
}

    IEnumerator ContinueMoveNextFrame(Vector2 inputDir, int version)
    {
        yield return null;

        // 🔥 KILL OLD COROUTINES AFTER PAUSE
        if (version != moveVersion) yield break;

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

    float halfHeight = transform.localScale.y * 0.5f;

    // 🔥 SAFETY: if no ground detected, DON'T snap (prevents bad data)
    if (!isGrounded)
    {
        transform.position = snapped;
        return;
    }

    float targetY = groundHit.point.y + halfHeight;

    // 🔥 ALWAYS FORCE Y (no conditional nonsense)
    snapped.y = targetY;

    transform.position = snapped;

    // 🔥 CLEAN ROTATION
    Vector3 r = transform.eulerAngles;
    transform.rotation = Quaternion.Euler(
        Mathf.Round(r.x / 90f) * 90f,
        Mathf.Round(r.y / 90f) * 90f,
        Mathf.Round(r.z / 90f) * 90f
    );

    rb.position = transform.position;
    rb.rotation = transform.rotation;
}

void ForceGroundSnapAndFX()
{
    if (!isGrounded) return;

    float halfHeight = transform.localScale.y * 0.5f;

    Vector3 pos = transform.position;
    pos.y = groundHit.point.y + halfHeight;
    transform.position = pos;

    // 🔥 ALWAYS spawn particles after movement
    SpawnFlipParticles();
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

public void HardResetAfterPause()
{
    moveVersion++; 

    // Reset ALL movement states
    isMoving = false;
    isBoosting = false;
    
    // Reset ALL input buffers
    bufferedInput = Vector2.zero;
    hasBufferedInput = false;
    currentHoldInput = Vector2.zero;
    input = Vector2.zero;
    
    // Reset UI Button flags
    upPressed = downPressed = leftPressed = rightPressed = false;
    isSwiping = false;

    // Reset Physics
    if (rb != null)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Force snap to ground
    if (isGrounded)
    {
        float halfHeight = transform.localScale.y * 0.5f;
        transform.position = new Vector3(transform.position.x, groundHit.point.y + halfHeight, transform.position.z);
    }

    // 🔥 THE FIX: Setup the flush
    ignoreInputAfterResume = true;
    inputBlockTimer = 0.2f; 
}

}