using UnityEngine;
using UnityEngine.UI;

public class UnitPartsTabController : MonoBehaviour
{
    [SerializeField] private UnitPartSlotUI[] slots;
    [SerializeField] private PlayerPartsInventory partsInventory;

    [Header("Convert To Skill Point")]
    [SerializeField] private Button convertButton;

    private UnitDefinition _currentUnit;

    private void Awake()
    {
        if (convertButton != null)
            convertButton.onClick.AddListener(ConvertAllPartsToSkillPoint);
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

        foreach (var slotUI in slots)
        {
            if (slotUI == null) continue;

            bool unitHasThisSlot = UnitHasSlot(_currentUnit, slotUI.slotType);
            if (!unitHasThisSlot)
            {
                slotUI.gameObject.SetActive(false);
                continue;
            }
            else
            {
                slotUI.gameObject.SetActive(true);
            }

            var owned = partsInventory.GetBestOwnedPart(_currentUnit, slotUI.slotType);

            if (owned == null)
                slotUI.SetLocked();
            else
                slotUI.SetOwned(owned);

            int green, blue, epic;
            partsInventory.GetCountsForSlot(_currentUnit, slotUI.slotType,
                                            out green, out blue, out epic);
            slotUI.SetCounts(green, blue, epic);
        }
    }

    // ===== Button click =====
    private void ConvertAllPartsToSkillPoint()
    {
        if (_currentUnit == null || partsInventory == null) return;
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        int converted = partsInventory.ConvertAllCompleteSetsToSkillPoints(_currentUnit);
        if (converted <= 0)
        {
            UpdateConvertButtonState();
            return;
        }

        // מוסיפים skill points ליחידה ב-ownedUnits
        UnitProgressData up = UnitUpgradeProgressService.GetOrCreateUnitProgress(_currentUnit.id);
        if (up != null)
        {
            up.skillPoints += converted;
            GameData.Instance.SaveNow();
        }

        // רענון UI
        Refresh();

        // אם יש לך טאב שדרוגים פתוח כרגע ואתה רוצה שיעדכן טקסט:
        // UnitDetailsPopupController.Instance?.ForceRefreshUpgradesTab(); (אם תעשה פונקציה כזאת)
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
