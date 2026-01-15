using UnityEngine;

public static class MasteryProgress
{
    private static PlayerSaveData Save => GameData.Instance?.Save;

    public static int GetLevel(string id)
    {
        if (Save == null) return 0;
        var list = Save.masteryLevels;

        for (int i = 0; i < list.Count; i++)
            if (list[i].id == id)
                return Mathf.Max(0, list[i].level);

        return 0;
    }

    public static void AddLevel(string id, int delta)
    {
        if (Save == null || string.IsNullOrEmpty(id) || delta == 0) return;

        var list = Save.masteryLevels;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == id)
            {
                list[i].level = Mathf.Max(0, list[i].level + delta);
                GameData.Instance.SaveNow();
                return;
            }
        }

        var entry = new MasteryLevelData
        {
            id = id,
            level = Mathf.Max(0, delta)
        };

        list.Add(entry);
        GameData.Instance.SaveNow();
    }

    /// <summary>
    /// Sets a level but clamps it to [0..maxLevel].
    /// This is useful when maxLevel exists in the definition.
    /// </summary>
    public static void SetLevelClamped(string id, int newLevel, int maxLevel)
    {
        if (Save == null || string.IsNullOrEmpty(id)) return;

        int clamped = Mathf.Clamp(newLevel, 0, Mathf.Max(1, maxLevel));
        var list = Save.masteryLevels;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id == id)
            {
                list[i].level = clamped;
                GameData.Instance.SaveNow();
                return;
            }
        }

        var entry = new MasteryLevelData { id = id, level = clamped };
        list.Add(entry);
        GameData.Instance.SaveNow();
    }
}
