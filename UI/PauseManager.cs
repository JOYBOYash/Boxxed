using UnityEngine;
using UnityEngine.UI;
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

    [Header("Control Toggle UI")]

    // 🔥 BUTTON ICONS
    public Image joystickIcon;
    public Image arrowsIcon;

    [Header("Icon Visuals")]

    public float activeAlpha = 1f;
    public float inactiveAlpha = 0.3f;

    public Vector3 activeScale = Vector3.one * 1.1f;
    public Vector3 inactiveScale = Vector3.one;

    [Header("Animation")]
    public float fadeDuration = 0.25f;

    private bool isPaused = false;

    void Start()
    {
        SetPopup(0f, false);

        UpdateControlIcons();
    }

    // ---------------- PAUSE ----------------

    public void OnPausePressed()
    {
        if (isPaused)
            return;

        isPaused = true;

        Time.timeScale = 0f;

        // 🔥 DISABLE PLAYER INPUT
        if (player != null)
            player.enabled = false;

        StartCoroutine(
            FadePopup(0f, 1f)
        );

        pausePopup.interactable = true;
        pausePopup.blocksRaycasts = true;
    }

    // ---------------- RESUME ----------------

    public void OnClosePressed()
    {
        if (!isPaused)
            return;

        isPaused = false;

        // 🔥 RESTORE TIME FIRST
        Time.timeScale = 1f;

        // 🔥 RE-ENABLE PLAYER
        if (player != null)
        {
            player.enabled = true;

            // 🔥 HARD INPUT RESET
            player.HardResetAfterPause();
        }

        StartCoroutine(
            FadePopup(1f, 0f)
        );

        pausePopup.interactable = false;
        pausePopup.blocksRaycasts = false;
    }

    // ---------------- CONTROL TOGGLES ----------------

    public void OnJoystickPressed()
    {
        if (player == null)
            return;

        player.useJoystick = true;
        player.useUIButtons = false;

        player.UpdateInputModeVisuals();

        UpdateControlIcons();
    }

    public void OnArrowsPressed()
    {
        if (player == null)
            return;

        player.useJoystick = false;
        player.useUIButtons = true;

        player.UpdateInputModeVisuals();

        UpdateControlIcons();
    }

    void UpdateControlIcons()
    {
        if (player == null)
            return;

        // ---------------- JOYSTICK ----------------

        if (joystickIcon != null)
        {
            Color c = joystickIcon.color;

            c.a =
                player.useJoystick
                ? activeAlpha
                : inactiveAlpha;

            joystickIcon.color = c;

            joystickIcon.transform.localScale =
                player.useJoystick
                ? activeScale
                : inactiveScale;
        }

        // ---------------- ARROWS ----------------

        if (arrowsIcon != null)
        {
            Color c = arrowsIcon.color;

            c.a =
                player.useUIButtons
                ? activeAlpha
                : inactiveAlpha;

            arrowsIcon.color = c;

            arrowsIcon.transform.localScale =
                player.useUIButtons
                ? activeScale
                : inactiveScale;
        }
    }

    // ---------------- RESET TO MENU ----------------

    public void OnRefreshPressed()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // ---------------- UI ANIMATION ----------------

    IEnumerator FadePopup(float from, float to)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            float e =
                t * t * (3f - 2f * t);

            pausePopup.alpha =
                Mathf.Lerp(from, to, e);

            t += Time.unscaledDeltaTime;

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