using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UnitPartsTabController : MonoBehaviour
{
    [SerializeField] private UnitPartSlotUI[] slots;
    [SerializeField] private PlayerPartsInventory partsInventory;

    [Header("Convert To Skill Point")]
    [SerializeField] private Button convertButton;

    [Header("Parts Resources Path")]
    [SerializeField] private string partsResourcesPath = "Parts";

    private UnitDefinition _currentUnit;
    private PartDefinition[] _allPartsCache;

    private void Awake()
    {
        if (convertButton != null)
            convertButton.onClick.AddListener(ConvertAllPartsToSkillPoint);

        LoadAllPartsOnce();
    }

    private void LoadAllPartsOnce()
    {
        if (_allPartsCache != null && _allPartsCache.Length > 0)
            return;

        _allPartsCache = string.IsNullOrEmpty(partsResourcesPath)
            ? Resources.LoadAll<PartDefinition>("")
            : Resources.LoadAll<PartDefinition>(partsResourcesPath);
    }

    public void Show(UnitDefinition unit)
    {
        _currentUnit = unit;
        Refresh();
    }

    public void Refresh()
    {
        if (_currentUnit == null) return;
        if (slots == null || slots.Length == 0) return;

        LoadAllPartsOnce();

        foreach (var slotUI in slots)
        {
            if (slotUI == null) continue;

            bool unitHasThisSlot = UnitHasSlot(_currentUnit, slotUI.slotType);
            slotUI.gameObject.SetActive(unitHasThisSlot);
            if (!unitHasThisSlot) continue;

            // Pick "the part itself" for this unit+slot (base/lowest rarity)
            PartDefinition basePart = GetBasePartDefinition(_currentUnit, slotUI.slotType);

            // Do we own any part in this slot?
            var ownedBest = partsInventory != null
                ? partsInventory.GetBestOwnedPart(_currentUnit, slotUI.slotType)
                : null;

            if (ownedBest != null)
                slotUI.SetOwned(ownedBest);          // show owned (opaque)
            else
                slotUI.SetMissing(basePart);         // show same part (transparent)

            int green, blue, epic;
            partsInventory.GetCountsForSlot(_currentUnit, slotUI.slotType, out green, out blue, out epic);
            slotUI.SetCounts(green, blue, epic);
        }

        UpdateConvertButtonState();
    }

    private PartDefinition GetBasePartDefinition(UnitDefinition unit, PartSlot slot)
    {
        if (unit == null || _allPartsCache == null) return null;

        // Choose the lowest rarity part for this unit+slot (as "default visual")
        // If you have multiple, this picks the first in that order.
        return _allPartsCache
            .Where(p => p != null && p.unit == unit && p.slot == slot)
            .OrderBy(p => p.rarity)
            .FirstOrDefault();
    }

    private void ConvertAllPartsToSkillPoint()
    {
        if (_currentUnit == null || partsInventory == null) return;
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        // converted holds the number of complete sets that were consumed
        int converted = partsInventory.ConvertAllCompleteSetsToSkillPoints(_currentUnit);

        if (converted <= 0)
        {
            UpdateConvertButtonState();
            return;
        }

        UnitProgressData up = UnitUpgradeProgressService.GetOrCreateUnitProgress(_currentUnit.id);
        if (up != null)
        {
            up.skillPoints += converted;

            // ADDED: Grant XP for every complete set converted
            if (PlayerXPManager.Instance != null)
            {
                // Example: 25 XP per completed set. 
                // If they converted 2 sets at once, they get 50 XP.
                PlayerXPManager.Instance.AddXP(converted * 25);
            }

            GameData.Instance.SaveNow();
        }

        Refresh();
    }

    private void UpdateConvertButtonState()
    {
        if (convertButton == null) return;

        bool canConvert = partsInventory != null &&
                          _currentUnit != null &&
                          partsInventory.HasCompleteSet(_currentUnit);

        convertButton.interactable = canConvert;
    }

    private bool UnitHasSlot(UnitDefinition unit, PartSlot slotType)
    {
        if (unit.partSlots == null) return false;
        foreach (var cfg in unit.partSlots)
            if (cfg.slot == slotType)
                return true;
        return false;
    }
}
