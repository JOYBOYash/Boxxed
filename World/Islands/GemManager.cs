using UnityEngine;
using TMPro;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    public TextMeshProUGUI gemText;

    private int totalGems = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddGems(int amount)
    {
        totalGems += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (gemText != null)
            gemText.text = totalGems.ToString();
    }
}