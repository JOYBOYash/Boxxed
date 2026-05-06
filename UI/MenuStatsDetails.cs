using UnityEngine;
using TMPro;

public class RunStatsDisplay : MonoBehaviour
{
    [Header("Run Data")]
    public RunDataSO runData;

    [Header("UI - Gems")]
    public TextMeshProUGUI bestGemsText;
    public TextMeshProUGUI lastGemsText;

    [Header("UI - Distance")]
    public TextMeshProUGUI bestDistanceText;
    public TextMeshProUGUI lastDistanceText;

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (runData == null) return;

        // ---------------- GEMS ----------------
        bestGemsText.text = runData.bestGems.ToString();
        lastGemsText.text = runData.lastGems.ToString();

        // ---------------- DISTANCE ----------------
        bestDistanceText.text = FormatDistance(runData.bestDistance);
        lastDistanceText.text = FormatDistance(runData.lastDistance);
    }

    string FormatDistance(float value)
    {
        if (value >= 1000f)
            return (value / 1000f).ToString("0.0") + " km";

        return Mathf.FloorToInt(value) + " m";
    }

    
}