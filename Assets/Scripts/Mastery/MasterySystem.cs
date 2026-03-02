using System.Collections.Generic;
using UnityEngine;

public static class MasterySystem
{
    // Draw cost grows based on total draws done
    public static int GetDrawCost(MasteryDatabase db)
    {
        var save = GameData.Instance?.Save;
        int n = Mathf.Max(0, save != null ? save.masteryDrawCount : 0);
        int baseCost = db != null ? db.baseDrawCost : 1500;
        int perDraw = db != null ? db.costPerDraw : 50;
        return baseCost + perDraw * n;
    }

    // Can we draw right now? (valid DB + Save + enough gold + has eligible pick)
    public static bool CanDraw(MasteryDatabase db)
    {
        if (db == null || db.items == null || db.items.Count == 0) return false;
        if (GameData.Instance?.Save == null) return false;
        if (PlayerCurrencyWallet.Instance == null) return false;

        int cost = GetDrawCost(db);
        if (PlayerCurrencyWallet.Instance.Gold < cost) return false;

        return HasAnyEligiblePick(db);
    }

    /// <summary>
    /// Used by UI: is there at least one item that can still level up?
    /// </summary>
    public static bool HasAnyEligiblePick(MasteryDatabase db)
    {
        if (db == null || db.items == null) return false;

        foreach (var def in db.items)
        {
            if (def == null) continue;
            int max = def.maxLevel > 0 ? def.maxLevel : 1;
            int lvl = MasteryProgress.GetLevel(def.id);
            if (lvl < max) return true;
        }

        return false;
    }

    /// <summary>
    /// Preview pick for animation only (no gold spend, no save changes).
    /// Weighted by rarity first, then uniform within that rarity.
    /// Excludes items that already reached max level.
    /// </summary>
    public static MasteryItemDefinition PreviewPick(MasteryDatabase db)
    {
        if (db == null || db.items == null || db.items.Count == 0) return null;

        // Build eligible pools per rarity
        var common = new List<MasteryItemDefinition>();
        var rare = new List<MasteryItemDefinition>();
        var epic = new List<MasteryItemDefinition>();

        foreach (var def in db.items)
        {
            if (def == null) continue;

            int max = def.maxLevel > 0 ? def.maxLevel : 1;
            int lvl = MasteryProgress.GetLevel(def.id);

            if (lvl >= max) continue; // do not pick maxed items

            switch (def.rarity)
            {
                case MasteryRarity.Epic: epic.Add(def); break;
                case MasteryRarity.Rare: rare.Add(def); break;
                default: common.Add(def); break;
            }
        }

        if (common.Count == 0 && rare.Count == 0 && epic.Count == 0)
            return null;

        // Anti-streak (avoid same id twice in a row) within the final rarity pool
        string lastId = GameData.Instance?.Save != null ? GameData.Instance.Save.masteryLastPickedId : null;

        // Choose rarity by weights, but only among rarities that have eligible items
        float wCommon = common.Count > 0 ? Mathf.Max(0f, db.commonChance) : 0f;
        float wRare = rare.Count > 0 ? Mathf.Max(0f, db.rareChance) : 0f;
        float wEpic = epic.Count > 0 ? Mathf.Max(0f, db.epicChance) : 0f;

        // If all weights ended up 0 (e.g. user forgot to set), fallback to equal
        if (wCommon + wRare + wEpic <= 0.0001f)
        {
            wCommon = common.Count > 0 ? 1f : 0f;
            wRare = rare.Count > 0 ? 1f : 0f;
            wEpic = epic.Count > 0 ? 1f : 0f;
        }

        MasteryRarity pickedRarity = RollRarity(wCommon, wRare, wEpic);

        List<MasteryItemDefinition> pool = pickedRarity switch
        {
            MasteryRarity.Epic => epic,
            MasteryRarity.Rare => rare,
            _ => common
        };

        // Safety: if something changed, fallback to any non-empty pool
        if (pool == null || pool.Count == 0)
        {
            if (epic.Count > 0) pool = epic;
            else if (rare.Count > 0) pool = rare;
            else pool = common;
        }

        return PickRandomFromPool(pool, lastId);
    }

    /// <summary>
    /// Commit the draw: spend gold + add level (clamped to maxLevel) + save.
    /// Call ONLY after the roll animation ends.
    /// </summary>
    public static bool CommitDraw(MasteryDatabase db, MasteryItemDefinition picked)
    {
        if (picked == null) return false;
        if (!CanDraw(db)) return false;

        int max = picked.maxLevel > 0 ? picked.maxLevel : 1;
        int current = MasteryProgress.GetLevel(picked.id);

        // If already maxed, do not spend or change anything
        if (current >= max) return false;

        int cost = GetDrawCost(db);
        PlayerCurrencyWallet.Instance.SpendGold(cost);

        // Add 1 level, but do not exceed maxLevel
        MasteryProgress.SetLevelClamped(picked.id, current + 1, max);

        GameData.Instance.Save.masteryDrawCount++;
        GameData.Instance.Save.masteryLastPickedId = picked.id;
        GameData.Instance.SaveNow();
        return true;
    }

    private static MasteryRarity RollRarity(float wCommon, float wRare, float wEpic)
    {
        float total = wCommon + wRare + wEpic;
        float r = Random.Range(0f, total);

        if (r < wEpic) return MasteryRarity.Epic;
        r -= wEpic;

        if (r < wRare) return MasteryRarity.Rare;

        return MasteryRarity.Common;
    }

    private static MasteryItemDefinition PickRandomFromPool(List<MasteryItemDefinition> pool, string avoidId)
    {
        if (pool == null || pool.Count == 0) return null;
        if (pool.Count == 1) return pool[0];

        // First pick
        var candidate = pool[Random.Range(0, pool.Count)];

        // Try to avoid streak once
        if (!string.IsNullOrEmpty(avoidId) && candidate != null && candidate.id == avoidId)
            candidate = pool[Random.Range(0, pool.Count)];

        return candidate;
    }

    // Bonuses (future use)
    public static float GetPercentBonus(MasteryDatabase db, MasteryStat stat)
    {
        if (db == null) return 0f;

        float sum = 0f;
        foreach (var def in db.items)
        {
            if (def == null) continue;
            if (def.stat != stat) continue;
            if (def.valueKind != MasteryValueKind.Percent) continue;

            int lvl = MasteryProgress.GetLevel(def.id);
            sum += def.GetValueAtLevel(lvl);
        }
        return sum;
    }

    public static int GetFlatBonus(MasteryDatabase db, MasteryStat stat)
    {
        if (db == null) return 0;

        float sum = 0f;
        foreach (var def in db.items)
        {
            if (def == null) continue;
            if (def.stat != stat) continue;
            if (def.valueKind != MasteryValueKind.Flat) continue;

            int lvl = MasteryProgress.GetLevel(def.id);
            sum += def.GetValueAtLevel(lvl);
        }
        return Mathf.RoundToInt(sum);
    }
}
