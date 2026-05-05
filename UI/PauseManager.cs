using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    public CubeJumpFlipController player;
    public AdvancedCameraFollow camFollow;
    public GameMenuController menuController;

    [Header("UI")]
    public CanvasGroup pausePopup;

    

    [Header("Animation")]
    public float fadeDuration = 0.25f;

    private bool isPaused = false;

    void Start()
    {
        SetPopup(0f, false);
    }

    // ---------------- PAUSE ----------------

    public void OnPausePressed()
    {
        if (isPaused) return;

        isPaused = true;

        Time.timeScale = 0f;

        // 🔥 disable input
        if (player != null)
            player.enabled = false;
        StartCoroutine(FadePopup(0f, 1f));
        pausePopup.interactable = true;
        pausePopup.blocksRaycasts = true;
    }

    // ---------------- RESUME ----------------

public void OnClosePressed()
{
    if (!isPaused) return;

    isPaused = false;

    // 1. First, set Time back to normal
    Time.timeScale = 1f;

    // 2. Enable the script
    if (player != null)
    {
        player.enabled = true;
        // 3. Immediately call the HardReset to wipe any "ghost" inputs
        player.HardResetAfterPause(); 
    }

    StartCoroutine(FadePopup(1f, 0f));
    pausePopup.interactable = false;
    pausePopup.blocksRaycasts = false;
}
    // ---------------- RESET TO MENU ----------------

    public void OnRefreshPressed()
    {
        Time.timeScale = 1f;

        // 🔥 reload scene cleanly
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------------- UI ANIMATION ----------------

    IEnumerator FadePopup(float from, float to)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            float e = t * t * (3f - 2f * t);
            pausePopup.alpha = Mathf.Lerp(from, to, e);

            t += Time.unscaledDeltaTime; // 🔥 important for pause
            yield return null;
        }

        pausePopup.alpha = to;
    }

    void SetPopup(float alpha, bool active)
    {
        pausePopup.alpha = alpha;
        pausePopup.interactable = active;
        pausePopup.blocksRaycasts = active;
    }
}