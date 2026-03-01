using System.Collections.Generic;
using UnityEngine;

public class MasteryBonusManager : MonoBehaviour
{
    public static MasteryBonusManager Instance { get; private set; }

    [SerializeField] private MasteryDatabase database;

    private readonly Dictionary<MasteryStat, float> _flat = new();
    private readonly Dictionary<MasteryStat, float> _percent = new(); // stored as fraction: 0.12 = 12%

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RebuildCache();
    }

    public void RebuildCache()
    {
        _flat.Clear();
        _percent.Clear();

        if (database == null || database.items == null) return;

        for (int i = 0; i < database.items.Count; i++)
        {
            var def = database.items[i];
            if (def == null || string.IsNullOrEmpty(def.id)) continue;

            int lvl = MasteryProgress.GetLevel(def.id);
            if (lvl <= 0) continue;

            // safety clamp
            int max = def.maxLevel > 0 ? def.maxLevel : 1;
            lvl = Mathf.Clamp(lvl, 0, max);

            float v = def.GetValueAtLevel(lvl);

            if (def.valueKind == MasteryValueKind.Flat)
            {
                AddTo(_flat, def.stat, v);
            }
            else // Percent
            {
                // v is "percent points" (e.g. 8 means 8%)
                AddTo(_percent, def.stat, v / 100f);
            }
        }
    }

    public int GetBoostedGold(int baseGold)
    {
        float bonusPercent = GetPercent(MasteryStat.GoldGainPercent);
        // English comment: Calculate final gold amount based on mastery level
        return Mathf.FloorToInt(baseGold * (1f + bonusPercent));
    }

    private static void AddTo(Dictionary<MasteryStat, float> dict, MasteryStat stat, float value)
    {
        dict.TryGetValue(stat, out float cur);
        dict[stat] = cur + value;
    }

    // ---------- API ----------
    public float GetPercent(MasteryStat stat) // returns fraction (0.08 = 8%)
        => _percent.TryGetValue(stat, out var v) ? v : 0f;

    public float GetPercentAsPoints(MasteryStat stat) // returns 8 for 8%
        => GetPercent(stat) * 100f;

    public float GetFlat(MasteryStat stat)
        => _flat.TryGetValue(stat, out var v) ? v : 0f;

    public int GetFlatInt(MasteryStat stat)
        => Mathf.RoundToInt(GetFlat(stat)); // or FloorToInt if you prefer
}
