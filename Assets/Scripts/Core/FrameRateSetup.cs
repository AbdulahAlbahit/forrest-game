using UnityEngine;

public static class FrameRateSetup
{
    private const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyTargetFrameRate()
    {
        Application.targetFrameRate = TargetFrameRate;
    }
}
