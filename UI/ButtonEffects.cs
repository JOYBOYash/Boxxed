using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UIButtonFX : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    public RectTransform target;
    public Image shineImage;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip clickSound;
    public AudioClip hoverSound;

    [Range(0f, 1f)]
    public float clickVolume = 1f;

    [Header("Idle Pulse")]
    public bool enableIdlePulse = true;

    public float pulseSpeed = 2f;

    public float pulseScaleAmount = 0.06f;

    [Header("Highlighted Prompt")]
    public bool highlightedButton = false;

    public float highlightScaleAmount = 0.12f;

    public float highlightSpeed = 3f;

    [Header("Click Feedback")]
    public float pressedScale = 0.9f;

    public float pressLerpSpeed = 18f;

    [Header("Hover")]
    public bool enableHoverScale = true;

    public float hoverScale = 1.08f;

    [Header("Shine FX")]
    public bool enableShine = true;

    public float shineSpeed = 1.5f;

    public float shineMaxAlpha = 0.45f;

    private Vector3 baseScale;
    private Vector3 targetScale;

    private bool isHovering;
    private bool isPressed;

    private Material shineMaterial;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();

        if (target == null)
            target = transform as RectTransform;

        baseScale = target.localScale;
        targetScale = baseScale;

        // 🔥 AUTO CREATE MATERIAL INSTANCE
        if (shineImage != null)
        {
            shineMaterial =
                Instantiate(shineImage.material);

            shineImage.material = shineMaterial;
        }
    }

    void Update()
    {
        HandleScale();

        HandleIdlePulse();

        HandleShine();
    }

    // ---------------- SCALE SYSTEM ----------------

    void HandleScale()
    {
        Vector3 desired = baseScale;

        // 🔥 HIGHLIGHTED BUTTON
        if (highlightedButton)
        {
            float pulse =
                1f +
                Mathf.Sin(Time.unscaledTime * highlightSpeed)
                * highlightScaleAmount;

            desired *= pulse;
        }

        // 🔥 HOVER
        if (isHovering && enableHoverScale)
        {
            desired *= hoverScale;
        }

        // 🔥 PRESSED
        if (isPressed)
        {
            desired *= pressedScale;
        }

        targetScale = desired;

        target.localScale =
            Vector3.Lerp(
                target.localScale,
                targetScale,
                Time.unscaledDeltaTime * pressLerpSpeed
            );
    }

    // ---------------- IDLE PULSE ----------------

    void HandleIdlePulse()
    {
        if (!enableIdlePulse || highlightedButton)
            return;

        float pulse =
            1f +
            Mathf.Sin(Time.unscaledTime * pulseSpeed)
            * pulseScaleAmount;

        target.localScale *= pulse;
    }

    // ---------------- SHINE FX ----------------

    void HandleShine()
    {
        if (!enableShine || shineImage == null)
            return;

        Color c = shineImage.color;

        float alpha =
            Mathf.Abs(
                Mathf.Sin(
                    Time.unscaledTime * shineSpeed
                )
            ) * shineMaxAlpha;

        c.a = alpha;

        shineImage.color = c;

        // optional UV scrolling
        if (shineMaterial != null)
        {
            float offset =
                Time.unscaledTime * 0.25f;

            shineMaterial.mainTextureOffset =
                new Vector2(offset, 0f);
        }
    }

    // ---------------- CLICK ----------------

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;

        PlaySound(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    // ---------------- HOVER ----------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    // ---------------- AUDIO ----------------

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(
            clip,
            clickVolume
        );
    }
}