using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DistanceSliderUI : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI bestText;

    // [Header("Tick System")]
    // public RectTransform tickContainer;
    // public GameObject tickPrefab;

    [Header("Settings")]
    public float segmentSize = 500f; // 🔥 each segment = 500m
    // public int visibleTicks = 6;     // how many ticks visible

    private float currentSegmentStart = 0f;
    // private List<RectTransform> ticks = new List<RectTransform>();

    // void Start()
    // {
    //     GenerateTicks();
    // }

    public void UpdateDistance(float current, float best)
    {
        // 🔥 Update text
        currentText.text = $"DISTANCE: {Mathf.FloorToInt(current)}m";
        bestText.text = $"BEST: {FormatDistance(best)}";

        // // 🔥 Check if we need to move segment
        // if (current > currentSegmentStart + segmentSize)
        // {
        //     currentSegmentStart += segmentSize;
        //     ShiftTicks();
        // }

        // 🔥 Calculate slider value within segment
        float normalized = (current - currentSegmentStart) / segmentSize;
        slider.value = normalized;
    }

    string FormatDistance(float value)
    {
        if (value >= 1000f)
            return (value / 1000f).ToString("0.0") + "km";

        return Mathf.FloorToInt(value) + "m";
    }

    // ---------------- TICKS ----------------

    // void GenerateTicks()
    // {
    //     for (int i = 0; i < visibleTicks; i++)
    //     {
    //         GameObject t = Instantiate(tickPrefab, tickContainer);
    //         RectTransform rt = t.GetComponent<RectTransform>();

    //         float posY = (i / (float)(visibleTicks - 1)) * tickContainer.rect.height;
    //         rt.anchoredPosition = new Vector2(0, posY);

    //         ticks.Add(rt);
    //     }
    // }

    // void ShiftTicks()
    // {
    //     // 🔥 move all ticks down visually
    //     foreach (var t in ticks)
    //     {
    //         t.anchoredPosition -= new Vector2(0, tickContainer.rect.height / (visibleTicks - 1));
    //     }

    //     // 🔥 recycle top tick
    //     RectTransform first = ticks[0];
    //     ticks.RemoveAt(0);

    //     float newY = tickContainer.rect.height;
    //     first.anchoredPosition = new Vector2(0, newY);

    //     ticks.Add(first);
    // }
}