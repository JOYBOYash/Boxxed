using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameMenuController : MonoBehaviour
{
    [Header("References")]
    public CubeJumpFlipController player;   // your main script
    public AdvancedCameraFollow camFollow;

    [Header("UI")]
    public CanvasGroup menuUI;
    public CanvasGroup gameplayUI;

    [Header("Zoom Settings")]
    public float startZoom = 3f;   // close-up
    public float gameplayZoom = 8f; // normal gameplay zoom
    public float zoomDuration = 0.6f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.4f;

    private bool gameStarted = false;



    void Start()
    {
        SetupInitialState();
    }

    void SetupInitialState()
    {
        // 🔥 LOCK PLAYER INPUT
        player.enabled = false;

        // 🔥 CAMERA CLOSE ZOOM
        if (camFollow != null)
            camFollow.orthoZoom = startZoom;

        // 🔥 MENU VISIBLE
        SetCanvasGroup(menuUI, 1f, true);

        // 🔥 GAME UI HIDDEN
        SetCanvasGroup(gameplayUI, 0f, false);
    }



    // ---------------- PLAY BUTTON ----------------
    public void OnPlayPressed()
    {
        if (gameStarted) return;
        gameStarted = true;

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        // 🔥 FADE OUT MENU
        yield return StartCoroutine(FadeCanvas(menuUI, 1f, 0f));

        menuUI.interactable = false;
        menuUI.blocksRaycasts = false;

        // 🔥 ENABLE PLAYER INPUT
        player.enabled = true;

        // 🔥 FADE IN GAME UI
        StartCoroutine(FadeCanvas(gameplayUI, 0f, 1f));

        gameplayUI.interactable = true;
        gameplayUI.blocksRaycasts = true;

        // 🔥 CAMERA ZOOM OUT
        yield return StartCoroutine(ZoomCamera(startZoom, gameplayZoom));
    }

    // ---------------- UTIL ----------------

    IEnumerator ZoomCamera(float from, float to)
    {
        float t = 0f;

        while (t < zoomDuration)
        {
            float e = t * t * (3f - 2f * t); // smoothstep
            camFollow.orthoZoom = Mathf.Lerp(from, to, e);

            t += Time.deltaTime;
            yield return null;
        }

        camFollow.orthoZoom = to;
        camFollow.orthoZoom = gameplayZoom;

        // 🔥 ADD THIS
        player.SetBaseZoom(gameplayZoom);
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            float e = t * t * (3f - 2f * t);
            cg.alpha = Mathf.Lerp(from, to, e);

            t += Time.deltaTime;
            yield return null;
        }

        cg.alpha = to;
    }

    void SetCanvasGroup(CanvasGroup cg, float alpha, bool active)
    {
        cg.alpha = alpha;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    // ---------------- OPTIONAL ----------------

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}