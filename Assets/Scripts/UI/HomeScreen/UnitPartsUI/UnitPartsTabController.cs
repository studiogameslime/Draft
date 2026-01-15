using UnityEngine;

public class UnitPartsTabController : MonoBehaviour
{
    [SerializeField] private UnitPartSlotUI[] slots;
    [SerializeField] private PlayerPartsInventory partsInventory;

    private UnitDefinition _currentUnit;



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

    private bool UnitHasSlot(UnitDefinition unit, PartSlot slotType)
    {
        if (unit.partSlots == null) return false;

        foreach (var cfg in unit.partSlots)
            if (cfg.slot == slotType)
                return true;

        return false;
    }
}
