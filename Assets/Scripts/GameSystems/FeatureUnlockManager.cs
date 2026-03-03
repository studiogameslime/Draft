using UnityEngine;

public static class FeatureUnlockManager
{
    public static bool IsUnlocked(GameFeature feature)
    {
        var save = GameData.Instance?.Save;
        if (save == null || save.unlockedFeatures == null)
            return false;

        return save.unlockedFeatures.Contains(feature.ToString());
    }

    public static void Unlock(GameFeature feature)
    {
        var save = GameData.Instance?.Save;
        if (save == null) return;

        if (save.unlockedFeatures == null)
            save.unlockedFeatures = new System.Collections.Generic.List<string>();

        string key = feature.ToString();
        if (!save.unlockedFeatures.Contains(key))
        {
            save.unlockedFeatures.Add(key);
            GameData.Instance.SaveNow();
            Debug.Log($"[FeatureUnlockManager] Unlocked: {key}");
        }
    }
}
