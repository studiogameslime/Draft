using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Parts/Player Parts Inventory")]
public class PlayerPartsInventory : ScriptableObject
{
    [System.Serializable]
    public class OwnedPart
    {
        public PartDefinition part;
        public int count;
    }

    [Header("Runtime data")]
    public List<OwnedPart> ownedParts = new();

    // =======================================================
    //                  LOAD / SAVE (GameData)
    // =======================================================

    public void LoadFromGameData()
    {
        ownedParts.Clear();

        if (GameData.Instance == null || GameData.Instance.Save == null)
        {
            // GameData not ready yet – caller must ensure correct timing
            return;
        }


        var savedParts = GameData.Instance.Save.parts;
        if (savedParts == null || savedParts.Count == 0)
            return;

        // Load all PartDefinitions (same as before)
        var allParts = Resources.LoadAll<PartDefinition>("");

        foreach (var saved in savedParts)
        {
            var def = allParts.FirstOrDefault(p => p != null && p.id == saved.partId);
            if (def == null || saved.amount <= 0)
                continue;

            ownedParts.Add(new OwnedPart
            {
                part = def,
                count = saved.amount
            });
        }
    }

    private void SaveToGameData()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
        {
            Debug.LogError("PlayerPartsInventory: GameData not ready");
            return;
        }

        var saveList = GameData.Instance.Save.parts;
        saveList.Clear();

        foreach (var owned in ownedParts)
        {
            if (owned.part == null || owned.count <= 0)
                continue;

            saveList.Add(new PartAmountData
            {
                partId = owned.part.id,
                amount = owned.count
            });
        }

        GameData.Instance.SaveNow();
    }

    // =======================================================
    //                  INVENTORY API
    // =======================================================

    public int GetCount(PartDefinition part)
    {
        var owned = ownedParts.FirstOrDefault(p => p.part == part);
        return owned != null ? owned.count : 0;
    }

    public PartDefinition GetBestOwnedPart(UnitDefinition unit, PartSlot slot)
    {
        return ownedParts
            .Where(p => p.part.unit == unit && p.part.slot == slot && p.count > 0)
            .OrderByDescending(p => p.part.rarity)
            .Select(p => p.part)
            .FirstOrDefault();
    }

    public List<OwnedPart> GetAllPartsForUnit(UnitDefinition unit)
    {
        return ownedParts
            .Where(p => p.part.unit == unit && p.count > 0)
            .ToList();
    }

    public void AddPart(PartDefinition part, int amount = 1)
    {
        if (part == null || amount <= 0)
            return;

        var owned = ownedParts.FirstOrDefault(p => p.part == part);
        if (owned == null)
        {
            owned = new OwnedPart { part = part, count = 0 };
            ownedParts.Add(owned);
        }

        owned.count += amount;
        SaveToGameData();
    }

    public void RemovePart(PartDefinition part, int amount = 1)
    {
        if (part == null || amount <= 0)
            return;

        var owned = ownedParts.FirstOrDefault(p => p.part == part);
        if (owned == null)
            return;

        owned.count -= amount;
        if (owned.count <= 0)
            ownedParts.Remove(owned);

        SaveToGameData();
    }

    public void ClearAll()
    {
        ownedParts.Clear();
        SaveToGameData();
    }

    public void GetCountsForSlot(
        UnitDefinition unit,
        PartSlot slot,
        out int green,
        out int blue,
        out int epic)
    {
        green = blue = epic = 0;

        if (unit == null)
            return;

        foreach (var owned in ownedParts)
        {
            if (owned.part == null ||
                owned.part.unit != unit ||
                owned.part.slot != slot ||
                owned.count <= 0)
                continue;

            switch (owned.part.rarity)
            {
                case PartRarity.Common:
                    green += owned.count;
                    break;
                case PartRarity.Rare:
                    blue += owned.count;
                    break;
                case PartRarity.Epic:
                    epic += owned.count;
                    break;
            }
        }
    }

    public bool HasCompleteSet(UnitDefinition unit)
    {
        if (unit == null || unit.partSlots == null || unit.partSlots.Length == 0)
            return false;

        for (int i = 0; i < unit.partSlots.Length; i++)
        {
            PartSlot slot = unit.partSlots[i].slot;
            var best = GetBestOwnedPart(unit, slot);
            if (best == null)
                return false;
        }

        return true;
    }

    // ממיר כמה שיותר סטים מלאים -> מחזיר כמה SkillPoints להוסיף
    public int ConvertAllCompleteSetsToSkillPoints(UnitDefinition unit)
    {
        if (unit == null || unit.partSlots == null || unit.partSlots.Length == 0)
            return 0;

        int converted = 0;

        if (HasCompleteSet(unit))
        {
            // צורכים 1 מכל slot (מעדיף לצרוך את ה"חלש" כדי לשמור את הטוב)
            for (int i = 0; i < unit.partSlots.Length; i++)
            {
                PartSlot slot = unit.partSlots[i].slot;

                // בחר את החלק עם rarity הכי נמוך שיש לך באותו slot
                var owned = ownedParts
                    .Where(p => p.part != null &&
                                p.part.unit == unit &&
                                p.part.slot == slot &&
                                p.count > 0)
                    .OrderBy(p => p.part.rarity)
                    .FirstOrDefault();

                if (owned == null)
                    return converted; // הגנה (לא אמור לקרות כי HasCompleteSet)

                RemovePart(owned.part, 1); // כבר שומר ל-GameData
            }

            converted += 1;
        }

        return converted;
    }
}
