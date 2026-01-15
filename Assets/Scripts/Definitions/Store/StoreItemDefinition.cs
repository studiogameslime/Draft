using UnityEngine;

[CreateAssetMenu(menuName = "Store/Store Item")]
public class StoreItemDefinition : ScriptableObject
{
    public string id;

    [Header("Category")]
    public StoreCategory category;

    [Header("Display")]
    public string title;
    public Sprite icon;

    [Header("Reward")]
    public int goldAmount;
    public ChestDefinition chestReward;

    [Header("Price")]
    public CostType costType;
    public int priceInGems;
    public int priceInGold;

    [Header("Discount")]
    [Tooltip("If > 0, this item is considered on sale")]
    [Range(0, 90)]
    public int discountPercent;

    [Header("Meta")]
    public bool isEnabled = true;

    // Daily free gold pack
    [Header("Daily Free")]
    public bool isDailyFree = false;

    [Tooltip("Resets every day at this hour (local device time). Example: 20 = 20:00")]
    [Range(0, 23)]
    public int dailyResetHour = 08;
}


public enum CostType
{
    Gold,
    Gems
}
