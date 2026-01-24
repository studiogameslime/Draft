using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreSection : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text sectionTitle;
    [SerializeField] private GridLayoutGroup grid;

    [Header("Layout")]
    [SerializeField] private int itemsPerRow = 3;
    [SerializeField] private float cellSpacing = 16f;

    [Header("Prefabs")]
    [SerializeField] private StoreItemUI itemPrefab;

    public void Setup(string title, List<StoreItemDefinition> items)
    {
        sectionTitle.text = title;
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
