using System.Collections.Generic;
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
    public int scrollAmount;
    public ChestDefinition chestReward;
    public List<PartRewardEntry> partRewards;

    [System.Serializable]
    public class PartRewardEntry
    {
        public PartDefinition part;
        public int amount;
    }


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

    public int GetFinalPrice()
    {
        int basePrice = costType == CostType.Gems
            ? priceInGems
            : priceInGold;

        if (discountPercent > 0)
        {
            basePrice = Mathf.RoundToInt(
                basePrice * (1f - discountPercent / 100f)
            );
        }

        return basePrice;
    }

}


public enum CostType
{
    Gold,
    Gems
}


