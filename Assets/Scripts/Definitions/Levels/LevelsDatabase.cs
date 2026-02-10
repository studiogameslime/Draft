using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelsDatabase : MonoBehaviour
{
    public static LevelsDatabase Instance { get; private set; }

    // key = stageId (ex: "1-1"), value = LevelDefinition asset
    private Dictionary<string, LevelDefinition> byStageId = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Rebuild();
    }

    public void Rebuild()
    {
        byStageId.Clear();

        // Loads all LevelDefinition assets under Resources/Levels
        var all = Resources.LoadAll<LevelDefinition>("Levels");
        foreach (var def in all)
        {
            if (def == null) continue;

            // IMPORTANT: asset name must match stageId (scene name) like "1-1"
            string stageId = def.name.Trim();
            if (string.IsNullOrEmpty(stageId)) continue;

            if (byStageId.ContainsKey(stageId))
            {
                Debug.LogWarning($"[LevelsDatabase] Duplicate stageId asset name: {stageId}");
                continue;
            }
            byStageId.Add(stageId, def);
        }

        Debug.Log($"[LevelsDatabase] Loaded {byStageId.Count} LevelDefinitions from Resources/Levels");
    }

    public bool TryGet(string stageId, out LevelDefinition def)
        => byStageId.TryGetValue(stageId, out def);

    public LevelDefinition GetOrNull(string stageId)
    {
        byStageId.TryGetValue(stageId, out var def);
        return def;
    }

    // Sorted stage list by chapter-stage (1-1, 1-2, 2-1...)
    public List<string> GetAllStageIdsSorted()
    {
        return byStageId.Keys
            .Select(id => new StageKey(id))
            .Where(k => k.IsValid)
            .OrderBy(k => k.Chapter)
            .ThenBy(k => k.Stage)
            .Select(k => k.Raw)
            .ToList();
    }

    public string GetNextStageId(string currentStageId)
    {
        var sorted = GetAllStageIdsSorted();
        int idx = sorted.IndexOf(currentStageId);
        if (idx < 0) return null;
        if (idx + 1 >= sorted.Count) return null;
        return sorted[idx + 1];
    }

    public int GetChapterTotalStages(int chapter)
    {
        return byStageId.Keys
            .Select(id => new StageKey(id))
            .Count(k => k.IsValid && k.Chapter == chapter);
    }

    public int GetChapterStageIndex(string stageId) // 1-based index inside chapter
    {
        var k = new StageKey(stageId);
        if (!k.IsValid) return 1;

        var inChapter = byStageId.Keys
            .Select(id => new StageKey(id))
            .Where(x => x.IsValid && x.Chapter == k.Chapter)
            .OrderBy(x => x.Stage)
            .ToList();

        int idx = inChapter.FindIndex(x => x.Raw == stageId);
        return Mathf.Max(1, idx + 1);
    }

    private struct StageKey
    {
        public readonly string Raw;
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsValid;

        public StageKey(string raw)
        {
            Raw = raw;
            Chapter = 0;
            Stage = 0;
            IsValid = false;

            if (string.IsNullOrWhiteSpace(raw)) return;
            var parts = raw.Split('-');
            if (parts.Length != 2) return;

            if (!int.TryParse(parts[0], out Chapter)) return;
            if (!int.TryParse(parts[1], out Stage)) return;

            IsValid = Chapter > 0 && Stage > 0;
        }
    }
}
