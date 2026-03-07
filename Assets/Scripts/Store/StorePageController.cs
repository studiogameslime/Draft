using UnityEngine;
using UnityEngine.UI;

public class StorePageController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StoreDatabase database;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private StoreSection sectionPrefab;

    private void Start()
    {
        EnsureContentLayout();
        BuildStore();
    }

    private void EnsureContentLayout()
    {
        var rt = content.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 20f;
        }
        vlg.padding = new RectOffset(0, 0, 0, 200);

        if (content.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void BuildStore()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        CreateSection("Gold Packs", StoreCategory.BuyGoldWithGems, 3);
        CreateSection("Chests Packs", StoreCategory.BuyChestsWithGold, 2);
        CreateSection("Parts Packs", StoreCategory.BuyPartWithGold, 3);
        CreateSection("Special Offers", StoreCategory.Specials, 1);
        CreateSection("Upgrade Scrolls", StoreCategory.BuyScrollsWithGold, 3);
    }

    private void CreateSection(string title, StoreCategory category, int itemsPerRow)
    {
        var items = database.GetItems(category);
        if (items.Count == 0)
            return;

        var section = Instantiate(sectionPrefab, content);

        // set layout before setup
        section.GetType()
            .GetField("itemsPerRow",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(section, itemsPerRow);

        section.Setup(title, items, category);
    }
}
