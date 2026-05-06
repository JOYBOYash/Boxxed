using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeartUI : MonoBehaviour
{
    [Header("References")]
    public Image heartImage;

    [Header("Animation")]
    public float popScale = 1.5f;
    public float duration = 0.25f;
    public Color damagedColor = Color.black;

    private Vector3 originalScale;
    private Color originalColor;

    void Awake()
    {
        originalScale = transform.localScale;

        if (heartImage != null)
            originalColor = heartImage.color;
    }

    // ---------------- STATE ----------------

    public void SetState(bool alive)
    {
        if (heartImage == null) return;

        heartImage.color = alive ? originalColor : damagedColor;
        transform.localScale = originalScale;
    }

    // ---------------- ANIMATION ----------------

    public void PlayLoseAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(PopAndFade());
    }

    IEnumerator PopAndFade()
    {
        float t = 0f;

        Vector3 start = originalScale;
        Vector3 peak = originalScale * popScale;

        Color startColor = originalColor;
        Color endColor = damagedColor;

        while (t < duration)
        {
            float e = t / duration;
            float eased = e * e * (3f - 2f * e);

            // 🔥 POP SCALE
            transform.localScale = Vector3.Lerp(start, peak, eased);

            // 🔥 COLOR FADE
            heartImage.color = Color.Lerp(startColor, endColor, eased);

            t += Time.deltaTime;
            yield return null;
        }

        // 🔥 settle back
        transform.localScale = originalScale;
        heartImage.color = endColor;
    }
}