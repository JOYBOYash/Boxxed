using UnityEngine;
using TMPro;
using System.Collections;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    [Header("Player Reference")]
    public Transform player;

    [Header("UI")]
    public TextMeshProUGUI gemText;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public float floatHeight = 1.5f;
    public float floatDuration = 1.2f;
    public float floatDistance = 1.2f;

    [Header("Gem VFX")]
    public GameObject gemCollectParticle;
    public float particleOffset = 0.2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip collectSound;

    [Header("Player Feedback")]
    public Renderer playerRenderer;
    public Color flashColor = Color.green;
    public float flashDuration = 0.15f;
    public int flashBlinkCount = 3;

    private Color originalColor;
    private int totalGems = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;
    }

    // ---------------- MAIN ENTRY ----------------

    public void AddGems(int amount, Vector3 worldPos)
    {
        totalGems += amount;
        UpdateUI();

        SpawnFloatingText(amount);
        SpawnGemParticles(); // 🔥 NEW
        PlaySound();
        StartCoroutine(FlashPlayer());
    }

    // ---------------- UI ----------------

    void UpdateUI()
    {
        if (gemText != null)
            gemText.text = totalGems.ToString();
    }

    // ---------------- FLOAT TEXT ----------------

    void SpawnFloatingText(int amount)
    {
        if (floatingTextPrefab == null || playerRenderer == null) return;

        GameObject textObj = Instantiate(floatingTextPrefab);

        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp != null)
            tmp.text = "+" + amount;

        StartCoroutine(AnimateFloatingText(textObj, tmp));
    }

    IEnumerator AnimateFloatingText(GameObject obj, TextMeshPro tmp)
    {
        float elapsed = 0f;

        Vector3 offsetStart = Vector3.up * floatHeight;
        Vector3 offsetEnd = Vector3.up * (floatHeight + floatDistance);

        Color startColor = tmp.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < floatDuration)
        {
            float t = elapsed / floatDuration;
            float eased = t * t * (3f - 2f * t);

            Vector3 basePos = playerRenderer.bounds.center;

            Vector3 camOffset = Vector3.zero;
            if (Camera.main != null)
            {
                camOffset = -Camera.main.transform.forward * 0.5f;
                obj.transform.forward = Camera.main.transform.forward;
            }

            Vector3 currentOffset = Vector3.Lerp(offsetStart, offsetEnd, eased);

            obj.transform.position = basePos + currentOffset + camOffset;

            if (tmp != null)
                tmp.color = Color.Lerp(startColor, endColor, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    // ---------------- PARTICLES ----------------

    void SpawnGemParticles()
    {
        if (gemCollectParticle == null || playerRenderer == null) return;

        Vector3 basePos = playerRenderer.bounds.center;
        Vector3 spawnPos = basePos + Vector3.up * particleOffset;

        GameObject fx = Instantiate(gemCollectParticle, spawnPos, Quaternion.identity);

        if (Camera.main != null)
            fx.transform.forward = Camera.main.transform.forward;

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(fx, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(fx, 2f);
    }

    // ---------------- SOUND ----------------

    void PlaySound()
    {
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }

    // ---------------- PLAYER FLASH ----------------

    IEnumerator FlashPlayer()
    {
        if (playerRenderer == null) yield break;

        for (int i = 0; i < flashBlinkCount; i++)
        {
            playerRenderer.material.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            playerRenderer.material.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }
}