public static class UILabels
{
    private const float MeleeMax = 1.0f;
    private const float ShortMax = 4.0f;

    public static RangeLabel GetRangeLabel(float attackRange)
    {
        if (attackRange <= MeleeMax) return RangeLabel.Melee;
        if (attackRange <= ShortMax) return RangeLabel.Short;
        return RangeLabel.Long;
    }

    public static string RangeToDisplay(RangeLabel label)
    {
        switch (label)
        {
            case RangeLabel.Melee: return "Melee";
            case RangeLabel.Short: return "Short";
            case RangeLabel.Long: return "Long";
            default: return "Short";
        }
    }

    public static UnitSpeedCategory GetSpeedLabel(float moveSpeed)
    {
        if (moveSpeed <= 0.6f) return UnitSpeedCategory.Slow;
        if (moveSpeed <= 0.9f) return UnitSpeedCategory.Normal;
        return UnitSpeedCategory.Fast;
    }

    public static string SpeedToDisplay(UnitSpeedCategory category)
    {
        switch (category)
        {
            case UnitSpeedCategory.Slow: return "Slow";
            case UnitSpeedCategory.Normal: return "Normal";
            case UnitSpeedCategory.Fast: return "Fast";
            default: return "Normal";
        }
    }
}