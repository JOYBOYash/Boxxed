using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public DistanceSliderUI sliderUI;

    [Header("Settings")]
    public float unitToMeters = 1f; // 1 Unity unit = 1 meter

    private float startX;
    private float currentDistance;
    private float bestDistance;

    void Start()
    {
        startX = player.position.x;

        bestDistance = PlayerPrefs.GetFloat("BEST_DISTANCE", 0f);
    }

    void Update()
    {
        if (player == null) return;

        // 🔥 Calculate distance
        float rawDistance = Mathf.Abs(player.position.x - startX);
        currentDistance = rawDistance * unitToMeters;

        // 🔥 Update best
        if (currentDistance > bestDistance)
        {
            bestDistance = currentDistance;
            PlayerPrefs.SetFloat("BEST_DISTANCE", bestDistance);
        }

        // 🔥 Send to UI
        sliderUI.UpdateDistance(currentDistance, bestDistance);
    }
}