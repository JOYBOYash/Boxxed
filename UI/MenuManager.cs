using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameMenuController : MonoBehaviour
{
    [Header("References")]
    public CubeJumpFlipController player;
    public AdvancedCameraFollow camFollow;

    public GemManager gemManager;
    public DistanceTracker distanceTracker;

    [Header("Run Data")]
    public RunDataSO runData;

    [Header("UI")]
    public CanvasGroup menuUI;
    public CanvasGroup gameplayUI;

    [Header("Death UI")]
    public CanvasGroup deathUI;
    public RectTransform deathPanel;

    public TextMeshProUGUI finalGemsText;
    public TextMeshProUGUI finalDistanceText;

    [Header("Zoom Settings")]
    public float startZoom = 3f;
    public float gameplayZoom = 8f;
    public float zoomDuration = 0.6f;

    [Header("Death Zoom")]
    public float deathZoom = 4f;
    public float deathZoomDuration = 0.6f;

    [Header("Fade Settings")]
    public float fadeDuration = 0.4f;

    private bool gameStarted = false;

    void Start()
    {
        SetupInitialState();
        runData.LoadFromPrefs();
    }

void SetupInitialState()
{
    player.enabled = false;

    if (camFollow != null)
    {
        camFollow.baseOrthoZoom = gameplayZoom;
        camFollow.SetZoom(startZoom);
    }

    SetCanvasGroup(menuUI, 1f, true);

    ForceDisableCanvas(gameplayUI);

    ForceDisableCanvas(deathUI);

    deathUI.gameObject.SetActive(false);
}

    void ForceDisableCanvas(CanvasGroup cg)
    {
        if (cg == null) return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    // ---------------- PLAY ----------------
    public void OnPlayPressed()
    {
        if (gameStarted) return;
        gameStarted = true;

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        yield return StartCoroutine(
            FadeCanvas(menuUI, 1f, 0f)
        );

        menuUI.interactable = false;
        menuUI.blocksRaycasts = false;

        player.enabled = true;

        StartCoroutine(
            FadeCanvas(gameplayUI, 0f, 1f)
        );

        gameplayUI.interactable = true;
        gameplayUI.blocksRaycasts = true;

        if (camFollow != null)
        {
            camFollow.baseOrthoZoom = gameplayZoom;

            camFollow.ZoomTo(
                gameplayZoom,
                zoomDuration
            );
        }

        yield return new WaitForSeconds(
            zoomDuration
        );
    }
    // ---------------- DEATH ----------------
    public void OnPlayerDeath()
    {
        StartCoroutine(DeathSequence());
    }

IEnumerator DeathSequence()
{
    // 🔥 HARD STOP PLAYER
    Rigidbody rb = player.GetComponent<Rigidbody>();

    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
    }

    // 🔥 DISABLE PLAYER INPUT
    player.enabled = false;

    // 🔥 collect stats
    int gems = gemManager.GetTotalGems();
    float distance = distanceTracker.GetDistance();

    // 🔥 save run
    if (runData != null)
    {
        runData.SaveRun(gems, distance);
        runData.SaveToPrefs();
    }

    // 🔥 update UI
    finalGemsText.text = gems.ToString();

    finalDistanceText.text =
        distance >= 1000f
        ? (distance / 1000f).ToString("0.0") + " km"
        : Mathf.FloorToInt(distance) + " m";

    // 🔥 CAMERA DRAMATIC ZOOM
if (camFollow != null)
{
    camFollow.ZoomTo(
        deathZoom,
        deathZoomDuration
    );
}

yield return new WaitForSeconds(
    deathZoomDuration
);

    deathUI.gameObject.SetActive(true);

    // 🔥 FADE IN DEATH UI
    deathUI.interactable = true;
    deathUI.blocksRaycasts = true;

    yield return StartCoroutine(
        FadeCanvas(deathUI, 0f, 1f)
    );
}

    // IEnumerator ScaleUpDeathPanel()
    // {
    //     float t = 0f;

    //     while (t < fadeDuration)
    //     {
    //         float e = t * t * (3f - 2f * t);
    //         deathPanel.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, e);

    //         t += Time.deltaTime;
    //         yield return null;
    //     }

    //     deathPanel.localScale = Vector3.one;
    // }

    // ---------------- RETRY ----------------
    public void OnRetryPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------------- UTIL ----------------
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

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}