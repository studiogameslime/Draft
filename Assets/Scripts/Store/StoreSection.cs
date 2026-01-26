using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreSection : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text sectionTitle;
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightIcon;
    [SerializeField] private GridLayoutGroup grid;

    [Header("Layout")]
    [SerializeField] private int itemsPerRow = 3;
    [SerializeField] private float cellSpacing = 16f;

    [Header("Prefabs")]
    [SerializeField] private StoreItemUI itemPrefab;

    public void Setup(string title, List<StoreItemDefinition> items, StoreCategory category)
    {
        sectionTitle.text = title;
        switch (category)
        {
            case StoreCategory.BuyGoldWithGems:
                leftIcon.sprite = StyleManager.instance.goldSprite;
                rightIcon.sprite = StyleManager.instance.goldSprite;
                break;
            case StoreCategory.BuyChestsWithGold:
                leftIcon.sprite = StyleManager.instance.rareChestSprite;
                rightIcon.sprite = StyleManager.instance.rareChestSprite;
                break;
            case StoreCategory.BuyPartWithGold:
                leftIcon.sprite = StyleManager.instance.PartWeaponSprite;
                rightIcon.sprite = StyleManager.instance.PartWeaponSprite;
                break;
        }
        Clear();

        UpdateGridLayout();

        foreach (var def in items)
        {
            var item = Instantiate(itemPrefab, grid.transform);
            item.Setup(def);
        }

        gameObject.SetActive(items.Count > 0);
    }

    private void UpdateGridLayout()
    {
        RectTransform rt = grid.GetComponent<RectTransform>();
        float width = rt.rect.width;

        float totalSpacing = cellSpacing * (itemsPerRow - 1);
        float cellWidth = (width - totalSpacing) / itemsPerRow;

        grid.spacing = new Vector2(cellSpacing, grid.spacing.y);
    }

    private void Clear()
    {
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
            Destroy(grid.transform.GetChild(i).gameObject);
    }
}
