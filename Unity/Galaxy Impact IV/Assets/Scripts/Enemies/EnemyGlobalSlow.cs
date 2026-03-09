using UnityEngine;

public static class EnemyGlobalSlow
{
    private static float multiplier = 1f;
    private static float endTime = 0f;

    public static float CurrentMultiplier
    {
        get
        {
            if (Time.time >= endTime) return 1f;
            return multiplier;
        }
    }

    // Refresca duración si se recoge de nuevo
    public static void Activate(float newMultiplier, float duration)
    {
        multiplier = Mathf.Clamp(newMultiplier, 0.05f, 1f);
        float newEnd = Time.time + Mathf.Max(0f, duration);
        if (newEnd > endTime) endTime = newEnd;
    }
    public static void Clear()
    {
        endTime = 0f;
        multiplier = 1f;
    }
    
}
