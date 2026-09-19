using UnityEngine;

/// <summary>
/// Tracks level unlock progress and best stars per level.
/// 3 stars = perfect, 2 stars = good, 1 star = cleared.
/// </summary>
public static class LevelManager
{
    public const int TotalLevels = 10;

    // Score thresholds per level for 1/2/3 stars
    static readonly int[] Star1 = { 50,  80,  120, 160, 200, 260, 320, 400, 480, 600 };
    static readonly int[] Star2 = { 100, 150, 200, 260, 320, 400, 480, 580, 700, 900 };
    static readonly int[] Star3 = { 180, 250, 320, 400, 500, 620, 750, 900,1100,1400 };

    // Time per level in seconds
    public static readonly float[] LevelTime = {
        60f, 55f, 50f, 50f, 45f, 45f, 40f, 40f, 35f, 30f
    };

    // Speed multiplier per level (passed to GameManager)
    public static readonly float[] LevelSpeed = {
        1.0f, 1.1f, 1.2f, 1.35f, 1.5f, 1.65f, 1.8f, 2.0f, 2.2f, 2.5f
    };

    public static int CurrentLevel { get; private set; } = 1;

    // ── PlayerPrefs keys ──────────────────────────────────────────────────
    static string StarKey(int level)    => $"Level_{level}_Stars";
    static string UnlockKey(int level)  => $"Level_{level}_Unlocked";

    public static void SelectLevel(int level)
    {
        if (IsUnlocked(level)) CurrentLevel = level;
    }

    public static bool IsUnlocked(int level)
    {
        return level == 1 || (level > 1 && GetStars(level - 1) > 0);
    }

    public static int GetStars(int level)
    {
        return PlayerPrefs.GetInt(StarKey(level), 0);
    }

    /// <summary>Call after a game session ends to save stars and unlock next level.</summary>
    public static void SaveResult(int level, int score)
    {
        int stars = 0;
        if (!IsUnlocked(level)) return;
        if (score >= GetClearScore(level)) stars = 1;
        if (score >= Threshold(Star2, level)) stars = 2;
        if (score >= GetStar3Score(level)) stars = 3;

        // Only save if better than previous
        int prev = GetStars(level);
        if (stars > prev)
            PlayerPrefs.SetInt(StarKey(level), stars);

        // Unlock next level if at least 1 star
        if (stars >= 1 && level < int.MaxValue)
            PlayerPrefs.SetInt(UnlockKey(level + 1), 1);

        PlayerPrefs.Save();
    }

    public static float GetLevelTime(int level)
        => LevelTime[Mathf.Clamp(level - 1, 0, LevelTime.Length - 1)];

    public static float GetLevelSpeed(int level)
        => LevelSpeed[Mathf.Clamp(level - 1, 0, LevelSpeed.Length - 1)];

    public static int GetStar3Score(int level)
        => Threshold(Star3, level);

    public static int GetClearScore(int level) => Threshold(Star1, level);

    // Endless progression, with bounded targets so later levels remain playable.
    static int Threshold(int[] thresholds, int level) => level <= 10
        ? thresholds[Mathf.Clamp(level - 1, 0, 9)]
        : thresholds[9] + Mathf.RoundToInt(150f * (1f - Mathf.Exp(-(level - 10f) / 30f)));
}
