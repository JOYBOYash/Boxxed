using UnityEngine;
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

    [Header("UI")]
    public List<HeartUI> hearts = new List<HeartUI>();

    [Header("References")]
    public GameMenuController gameMenu;

    private bool dead = false;
    private Coroutine freezeRoutine;

    void Start()
    {
        currentHearts = maxHearts;
        UpdateUI();
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

    // ---------------- DEATH ----------------

    void OnPlayerDead()
    {
        if (dead) return;

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
            StartCoroutine(HitFreeze(duration));
    }

    IEnumerator HitFreeze(float duration)
    {
        Time.timeScale = freezeTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
    }

    // ---------------- AUDIO ----------------

    void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
}