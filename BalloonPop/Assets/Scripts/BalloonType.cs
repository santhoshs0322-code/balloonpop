/// <summary>
/// All balloon types with their point values and special behaviours.
/// </summary>
public enum BalloonType
{
    Red,        // +10
    Blue,       // +20
    Green,      // +30
    Gold,       // +50
    Rainbow,    // +100
    Bomb,       // -20
    Heart       // +5 seconds
}

public static class BalloonTypeExtensions
{
    /// <summary>Returns the score change for tapping this balloon.</summary>
    public static int GetPoints(this BalloonType type)
    {
        return type switch
        {
            BalloonType.Red     => 10,
            BalloonType.Blue    => 20,
            BalloonType.Green   => 30,
            BalloonType.Gold    => 50,
            BalloonType.Rainbow => 100,
            BalloonType.Bomb    => -20,
            BalloonType.Heart   => 0,   // handled separately (adds time)
            _                   => 10
        };
    }

    /// <summary>Returns extra seconds granted (Heart only).</summary>
    public static float GetBonusTime(this BalloonType type)
    {
        return type == BalloonType.Heart ? 5f : 0f;
    }

    /// <summary>Chance weight used in spawner probability table.</summary>
    public static float GetWeight(this BalloonType type, bool specialsEnabled, bool bombsEnabled)
    {
        return type switch
        {
            BalloonType.Red     => 40f,
            BalloonType.Blue    => 25f,
            BalloonType.Green   => 15f,
            BalloonType.Gold    => specialsEnabled  ? 8f  : 0f,
            BalloonType.Rainbow => specialsEnabled  ? 3f  : 0f,
            BalloonType.Bomb    => bombsEnabled     ? 6f  : 0f,
            BalloonType.Heart   => specialsEnabled  ? 3f  : 0f,
            _                   => 0f
        };
    }
}
