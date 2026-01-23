using UnityEngine;
using UnityEngine.UI;

public class BattleUpgradesManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform upgradesContainer; // HorizontalLayoutGroup transform
    [SerializeField] private Image upgradeIconPrefab;     // prefab with Image only

    public void AddUpgradeIcon(UnitUpgradeNodeDefinition node)
    {
        if (node == null || node.icon == null)
            return;

        var upgrade = Instantiate(upgradeIconPrefab, upgradesContainer);
        if (!upgrade) return;
        var icon = upgrade.transform.GetChild(0)?.GetComponent<Image>();
        if (!icon) return;
        icon.sprite = node.icon;
        icon.gameObject.SetActive(true);
    }

    public void Clear()
    {
        for (int i = upgradesContainer.childCount - 1; i >= 0; i--)
            Destroy(upgradesContainer.GetChild(i).gameObject);
    }
}
