using UnityEngine;

public class AdvancedCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Smooth Follow")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothTime = 0.2f;

    private Vector3 lookAheadPos;
    private Vector3 lookAheadVelocity;
    private Vector3 lastTargetPosition;

    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // 🔥 NEW ZOOM SECTION
    [Header("Zoom Settings")]

    public Camera cam;

    [Range(20f, 100f)]
    public float zoom = 60f; // perspective zoom (FOV)

    [Range(2f, 50f)]
    public float orthoZoom = 5f; // orthographic zoom

    void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;

        if (cam == null)
            cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Step 1: Movement direction ---
        Vector3 direction = (target.position - lastTargetPosition);

        // --- Step 2: Look Ahead ---
        Vector3 desiredLookAhead = direction.normalized * lookAheadDistance;

        lookAheadPos = Vector3.SmoothDamp(
            lookAheadPos,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );

        // --- Step 3: Target position ---
        Vector3 targetPosition = target.position + offset + lookAheadPos;

        // --- Step 4: Smooth follow ---
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // --- Step 5: Bounds ---
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothedPosition;

        // 🔥 REAL ZOOM LOGIC
        if (cam != null)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = orthoZoom;
            }
            else
            {
                cam.fieldOfView = zoom;
            }
        }

        lastTargetPosition = target.position;
    }
}