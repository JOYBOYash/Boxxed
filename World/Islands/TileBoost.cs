using UnityEngine;

public class DiceBoostTile : MonoBehaviour
{
    public int requiredFace = 3; // face needed on TOP
    public int boostSteps = 3;   // how many steps forward

    public bool consumeOnce = false;
    private bool used = false;

    public bool TryActivate(int topFace)
    {
        if (used && consumeOnce) return false;

        if (topFace == requiredFace)
        {
            if (consumeOnce) used = true;
            return true;
        }

        return false;
    }
}