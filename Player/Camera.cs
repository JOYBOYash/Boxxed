using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    // ---------------- ZOOM ----------------

    [Header("Zoom Settings")]
    public Camera cam;

    [Range(20f, 100f)]
    public float zoom = 60f;
    [Range(2f, 50f)]
    public float baseOrthoZoom = 8f;

    [Range(0.01f, 1f)]
    public float zoomSmoothTime = 0.15f;

    private float currentOrthoZoom;
    private float targetZoom;
    private float zoomVelocity;

    private Coroutine activeZoomRoutine;

    // ---------------- CAMERA SHAKE ----------------

    [Header("Camera Shake")]
    public bool enableShake = true;

    private float shakeDuration = 0f;
    private float shakeStrength = 0f;

    // ---------------- FOG SYSTEM ----------------

    [Header("Fog Follow")]
    public Transform fogObject;

    public float fogYPosition = 0f;

    void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;

        if (cam == null)
            cam = GetComponent<Camera>();

        targetZoom = baseOrthoZoom;
        currentOrthoZoom = baseOrthoZoom;

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = currentOrthoZoom;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // ---------------- MOVEMENT DIRECTION ----------------

        Vector3 direction =
            (target.position - lastTargetPosition);

        // ---------------- LOOK AHEAD ----------------

        Vector3 desiredLookAhead =
            direction.normalized * lookAheadDistance;

        lookAheadPos = Vector3.SmoothDamp(
            lookAheadPos,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );

        // ---------------- TARGET POSITION ----------------

        Vector3 targetPosition =
            target.position +
            offset +
            lookAheadPos;

        // ---------------- SMOOTH FOLLOW ----------------

        Vector3 smoothedPosition =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );

        // ---------------- BOUNDS ----------------

        if (useBounds)
        {
            smoothedPosition.x =
                Mathf.Clamp(
                    smoothedPosition.x,
                    minBounds.x,
                    maxBounds.x
                );

            smoothedPosition.y =
                Mathf.Clamp(
                    smoothedPosition.y,
                    minBounds.y,
                    maxBounds.y
                );
        }

        // ---------------- CAMERA SHAKE ----------------

        if (enableShake && shakeDuration > 0f)
        {
            shakeDuration -= Time.unscaledDeltaTime;

            Vector3 shakeOffset =
                new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f
                ) * shakeStrength;

            smoothedPosition += shakeOffset;
        }

        // ---------------- APPLY POSITION ----------------

        transform.position = smoothedPosition;

        // ---------------- SMOOTH ZOOM ----------------

        UpdateZoom();

        // ---------------- FOG FOLLOW ----------------

        UpdateFog();

        lastTargetPosition = target.position;
    }

    // ---------------- SMOOTH ZOOM ----------------

void UpdateZoom()
{
    if (cam == null)
        return;

    if (cam.orthographic)
    {
        // 🔥 SAFE FALLBACK
        if (targetZoom <= 0f)
        {
            targetZoom = baseOrthoZoom;
        }

        // 🔥 CLAMP SAFELY
        targetZoom =
            Mathf.Clamp(
                targetZoom,
                5f,
                40f
            );

        currentOrthoZoom =
            Mathf.SmoothDamp(
                currentOrthoZoom,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime
            );

        cam.orthographicSize =
            currentOrthoZoom;
    }
    else
    {
        cam.fieldOfView = zoom;
    }
}
public void SetZoom(float zoom)
{
    Debug.Log("SET ZOOM: " + zoom);

    targetZoom = zoom;
}

    public void ResetZoom()
    {
        targetZoom = baseOrthoZoom;
    }

    public float GetCurrentZoom()
    {
        return currentOrthoZoom;
    }

public void ZoomTo(
    float zoom,
    float duration
)
{
    Debug.Log("ZOOM TO: " + zoom);

    if (activeZoomRoutine != null)
    {
        StopCoroutine(activeZoomRoutine);
    }

    activeZoomRoutine =
        StartCoroutine(
            ZoomRoutine(zoom, duration)
        );
}
    IEnumerator ZoomRoutine(
        float zoom,
        float duration
    )
    {
        float start = targetZoom;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float eased =
                t * t * (3f - 2f * t);

            targetZoom =
                Mathf.Lerp(
                    start,
                    zoom,
                    eased
                );

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        targetZoom = zoom;
    }

        // ---------------- SHAKE API ----------------

    public void TriggerShake(
        float duration,
        float strength
    )
    {
        shakeDuration = duration;
        shakeStrength = strength;
    }

    // ---------------- FOG FOLLOW ----------------

    void UpdateFog()
    {
        if (fogObject == null || target == null)
            return;

        Vector3 fogPos = fogObject.position;

        fogPos.x = target.position.x;
        fogPos.z = target.position.z;

        fogPos.y = fogYPosition;

        fogObject.position = fogPos;
    }
}