using UnityEngine;
using System.Collections;

public class QuitPopupController : MonoBehaviour
{
    [Header("Popup")]
    public CanvasGroup popupCanvas;

    public RectTransform popupPanel;

    [Header("Animation")]
    public float fadeDuration = 0.22f;

    [Range(0.9f, 1f)]
    public float startScale = 0.96f;

    private bool popupOpen = false;

    private Vector3 originalScale;

    void Awake()
    {
        if (popupPanel != null)
        {
            originalScale =
                popupPanel.localScale;
        }
    }

    void Start()
    {
        HideInstant();
    }

    // ---------------- OPEN ----------------

    public void OpenPopup()
    {
        if (popupOpen)
            return;

        popupOpen = true;

        gameObject.SetActive(true);

        StopAllCoroutines();

        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        float t = 0f;

        popupCanvas.interactable = true;
        popupCanvas.blocksRaycasts = true;

        // 🔥 START SLIGHTLY SMALL
        popupPanel.localScale =
            originalScale * startScale;

        while (t < fadeDuration)
        {
            float normalized =
                t / fadeDuration;

            // 🔥 SMOOTHER EASE OUT
            float eased =
                1f -
                Mathf.Pow(
                    1f - normalized,
                    3f
                );

            // 🔥 FADE
            popupCanvas.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    eased
                );

            // 🔥 CLEAN SCALE
            float scale =
                Mathf.Lerp(
                    startScale,
                    1f,
                    eased
                );

            popupPanel.localScale =
                originalScale * scale;

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        popupCanvas.alpha = 1f;

        popupPanel.localScale =
            originalScale;
    }

    // ---------------- CLOSE ----------------

    public void ClosePopup()
    {
        if (!popupOpen)
            return;

        popupOpen = false;

        StopAllCoroutines();

        StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        float t = 0f;

        Vector3 initialScale =
            popupPanel.localScale;

        Vector3 targetScale =
            originalScale * startScale;

        while (t < fadeDuration)
        {
            float normalized =
                t / fadeDuration;

            // 🔥 SMOOTH EASE IN
            float eased =
                normalized * normalized;

            // 🔥 FADE
            popupCanvas.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    eased
                );

            // 🔥 SCALE DOWN
            popupPanel.localScale =
                Vector3.Lerp(
                    initialScale,
                    targetScale,
                    eased
                );

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        popupCanvas.alpha = 0f;

        popupCanvas.interactable = false;
        popupCanvas.blocksRaycasts = false;

        popupPanel.localScale =
            originalScale;

        gameObject.SetActive(false);
    }

    // ---------------- YES ----------------

    public void OnYesPressed()
    {
        Application.Quit();

        Debug.Log("QUIT GAME");
    }

    // ---------------- NO ----------------

    public void OnNoPressed()
    {
        ClosePopup();
    }

    // ---------------- INIT ----------------

    void HideInstant()
    {
        popupCanvas.alpha = 0f;

        popupCanvas.interactable = false;
        popupCanvas.blocksRaycasts = false;

        popupPanel.localScale =
            originalScale;

        gameObject.SetActive(false);
    }
}