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
            case StoreCategory.BuyScrollsWithGold:
                leftIcon.sprite = StyleManager.instance.scrollSprite;
                rightIcon.sprite = StyleManager.instance.scrollSprite;
                break;
        }
        Clear();

        UpdateGridLayout();

        foreach (var def in items)
        {
            var item = Instantiate(itemPrefab, grid.transform);
            item.Setup(def);
        }

        ResizeToFitContent(items.Count);

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

    private void ResizeToFitContent(int itemCount)
    {
        if (itemCount <= 0) return;

        int rows = Mathf.CeilToInt((float)itemCount / itemsPerRow);
        float gridHeight = rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;

        RectTransform gridRT = grid.GetComponent<RectTransform>();
        // Anchor grid to top so it doesn't float when resized
        gridRT.anchorMin = new Vector2(0.5f, 1f);
        gridRT.anchorMax = new Vector2(0.5f, 1f);
        gridRT.pivot = new Vector2(0.5f, 1f);
        float padding = 30f;
        gridRT.anchoredPosition = new Vector2(0, -(150f + padding));
        gridRT.sizeDelta = new Vector2(gridRT.sizeDelta.x, gridHeight);

        float headerHeight = 150f + padding;
        RectTransform sectionRT = GetComponent<RectTransform>();
        sectionRT.pivot = new Vector2(0.5f, 1f);
        sectionRT.sizeDelta = new Vector2(sectionRT.sizeDelta.x, headerHeight + gridHeight);
    }

    private void Clear()
    {
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
            Destroy(grid.transform.GetChild(i).gameObject);
    }
}
