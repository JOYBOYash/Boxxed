using UnityEngine;

[CreateAssetMenu(fileName = "RunData", menuName = "Game/Run Data")]
public class RunDataSO : ScriptableObject
{
    public int bestGems;
    public float bestDistance;

    public int lastGems;
    public float lastDistance;

    public void SaveRun(int gems, float distance)
    {
        lastGems = gems;
        lastDistance = distance;

        if (gems > bestGems)
            bestGems = gems;

        if (distance > bestDistance)
            bestDistance = distance;
    }

    public void SaveToPrefs()
    {
        PlayerPrefs.SetInt("BEST_GEMS", bestGems);
        PlayerPrefs.SetFloat("BEST_DISTANCE", bestDistance);

        PlayerPrefs.SetInt("LAST_GEMS", lastGems);
        PlayerPrefs.SetFloat("LAST_DISTANCE", lastDistance);
    }

    public void LoadFromPrefs()
    {
        bestGems = PlayerPrefs.GetInt("BEST_GEMS", 0);
        bestDistance = PlayerPrefs.GetFloat("BEST_DISTANCE", 0);

        lastGems = PlayerPrefs.GetInt("LAST_GEMS", 0);
        lastDistance = PlayerPrefs.GetFloat("LAST_DISTANCE", 0);
    }
}