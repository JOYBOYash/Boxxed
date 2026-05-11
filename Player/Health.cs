using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 5;
    public int currentHearts;

    [Header("Death FX")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    [Header("Damage FX")]
    public AudioClip damageSound;

    [Header("Camera Shake")]
    public AdvancedCameraFollow camFollow;

    public float hitShakeDuration = 0.12f;
    public float hitShakeStrength = 0.08f;

    public float deathShakeDuration = 0.4f;
    public float deathShakeStrength = 0.25f;

    [Header("Hit Stop")]
    public float hitFreezeDuration = 0.05f;
    public float deathFreezeDuration = 0.15f;

    [Range(0f, 1f)]
    public float freezeTimeScale = 0.05f;

    // ---------------- DAMAGE UI ----------------

    [Header("Damage Popup")]

    public GameObject damagePopupPrefab;

    public Transform popupSpawnPoint;

    public float popupDuration = 1f;

    [Header("Damage Blur")]

    public Image damageBlurImage;

    public float blurFadeDuration = 0.2f;

    [Range(0f, 1f)]
    public float blurMaxAlpha = 0.45f;

    // ---------------- UI ----------------

    [Header("UI")]
    public List<HeartUI> hearts = new List<HeartUI>();

    [Header("References")]
    public GameMenuController gameMenu;

    private bool dead = false;
    private Coroutine freezeRoutine;
    private Coroutine blurRoutine;

    void Start()
    {
        currentHearts = maxHearts;

        UpdateUI();

        // 🔥 INITIALIZE BLUR
        if (damageBlurImage != null)
        {
            Color c = damageBlurImage.color;
            c.a = 0f;
            damageBlurImage.color = c;
        }
    }

    // ---------------- DAMAGE ----------------

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || dead)
            return;

        int prev = currentHearts;

        currentHearts =
            Mathf.Max(currentHearts - amount, 0);

        int lost = prev - currentHearts;

        // 🔥 SMALL HIT FREEZE
        StartFreeze(hitFreezeDuration);

        // 🔥 SMALL SHAKE
        if (camFollow != null)
        {
            camFollow.TriggerShake(
                hitShakeDuration,
                hitShakeStrength
            );
        }

        // 🔥 DAMAGE SOUND
        PlayDamageSound();

        // 🔥 DAMAGE POPUP
        SpawnDamagePopup(amount);

        // 🔥 DAMAGE BLUR
        TriggerDamageBlur();

        // 🔥 HEART ANIMATION
        for (int i = 0; i < lost; i++)
        {
            int index = prev - 1 - i;

            if (index >= 0 && index < hearts.Count)
            {
                hearts[index].PlayLoseAnimation();
            }
        }

        UpdateUI();

        if (currentHearts <= 0)
        {
            OnPlayerDead();
        }
    }

    // ---------------- UI ----------------

    void UpdateUI()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].SetState(i < currentHearts);
        }
    }

    // ---------------- DAMAGE POPUP ----------------

    void SpawnDamagePopup(int amount)
    {
        if (damagePopupPrefab == null)
            return;

        if (popupSpawnPoint == null)
            return;

        GameObject popup =
            Instantiate(
                damagePopupPrefab,
                popupSpawnPoint.position,
                Quaternion.identity,
                popupSpawnPoint.parent
            );

        TextMeshProUGUI tmp =
            popup.GetComponent<TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.text = "-" + amount;
        }

        StartCoroutine(
            AnimateDamagePopup(popup)
        );
    }

    IEnumerator AnimateDamagePopup(GameObject popup)
    {
        RectTransform rect =
            popup.GetComponent<RectTransform>();

        CanvasGroup cg =
            popup.GetComponent<CanvasGroup>();

        if (cg == null)
        {
            cg = popup.AddComponent<CanvasGroup>();
        }

        Vector3 startPos =
            rect.localPosition;

        Vector3 endPos =
            startPos + Vector3.up * 80f;

        float t = 0f;

        while (t < popupDuration)
        {
            float e = t / popupDuration;

            float eased =
                e * e * (3f - 2f * e);

            rect.localPosition =
                Vector3.Lerp(
                    startPos,
                    endPos,
                    eased
                );

            cg.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    eased
                );

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        Destroy(popup);
    }

    // ---------------- DAMAGE BLUR ----------------

    void TriggerDamageBlur()
    {
        if (damageBlurImage == null)
            return;

        if (blurRoutine != null)
        {
            StopCoroutine(blurRoutine);
        }

        blurRoutine =
            StartCoroutine(
                DamageBlurRoutine()
            );
    }

    IEnumerator DamageBlurRoutine()
    {
        Color c = damageBlurImage.color;

        // 🔥 FLASH IN
        float t = 0f;

        while (t < blurFadeDuration)
        {
            float e = t / blurFadeDuration;

            c.a =
                Mathf.Lerp(
                    0f,
                    blurMaxAlpha,
                    e
                );

            damageBlurImage.color = c;

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        // 🔥 FADE OUT
        t = 0f;

        while (t < blurFadeDuration)
        {
            float e = t / blurFadeDuration;

            c.a =
                Mathf.Lerp(
                    blurMaxAlpha,
                    0f,
                    e
                );

            damageBlurImage.color = c;

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        c.a = 0f;
        damageBlurImage.color = c;
    }

    // ---------------- DEATH ----------------

    void OnPlayerDead()
    {
        if (dead)
            return;

        dead = true;

        PlayDeathSound();

        // 🔥 BIG FREEZE
        StartFreeze(deathFreezeDuration);

        // 🔥 BIG SHAKE
        if (camFollow != null)
        {
            camFollow.TriggerShake(
                deathShakeDuration,
                deathShakeStrength
            );
        }

        if (gameMenu != null)
        {
            gameMenu.OnPlayerDeath();
        }
    }

    // ---------------- FREEZE ----------------

    void StartFreeze(float duration)
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
        }

        freezeRoutine =
            StartCoroutine(
                HitFreeze(duration)
            );
    }

    IEnumerator HitFreeze(float duration)
    {
        Time.timeScale = freezeTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
    }

    // ---------------- AUDIO ----------------

    void PlayDamageSound()
    {
        if (audioSource != null &&
            damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
    }

    void PlayDeathSound()
    {
        if (audioSource != null &&
            deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
}