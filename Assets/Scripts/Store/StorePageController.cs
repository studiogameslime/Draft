using UnityEngine;

public class StorePageController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StoreDatabase database;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private StoreSection sectionPrefab;

    private void Start()
    {
        BuildStore();
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
