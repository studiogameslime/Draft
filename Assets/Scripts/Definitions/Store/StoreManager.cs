using System;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [SerializeField] private ChestOpeningUI chestOpeningUI;
    [SerializeField] private PlayerPartsInventory partsInventory;

    public event Action OnFreePackStateChanged;
    public event Action OnPurchaseCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public bool CanClaimDailyFree(StoreItemDefinition item)
    {
        if (item == null || !item.isDailyFree) return false;
        if (GameData.Instance == null || GameData.Instance.Save == null) return false;

        long ticks = GameData.Instance.Save.nextDailyFreeGoldUtcTicks;
        if (ticks <= 0) return true; // never claimed yet

        DateTime nextUtc = new DateTime(ticks, DateTimeKind.Utc);
        return DateTime.UtcNow >= nextUtc;
    }

    public TimeSpan GetTimeUntilDailyFree(StoreItemDefinition item)
    {
        if (item == null || !item.isDailyFree) return TimeSpan.Zero;
        long ticks = GameData.Instance.Save.nextDailyFreeGoldUtcTicks;
        if (ticks <= 0) return TimeSpan.Zero;

        DateTime nextUtc = new DateTime(ticks, DateTimeKind.Utc);
        TimeSpan t = nextUtc - DateTime.UtcNow;
        return (t.TotalSeconds < 0) ? TimeSpan.Zero : t;
    }

    public void TryBuy(StoreItemDefinition item)
    {
        var save = GameData.Instance.Save;
        var wallet = PlayerCurrencyWallet.Instance;

        if (wallet == null)
            return;

        if (item.isDailyFree)
        {
            if (!DailyResetUtil.IsReady(save.nextDailyFreeGoldUtcTicks))
                return;

            wallet.AddGold(item.goldAmount);

            save.nextDailyFreeGoldUtcTicks = DailyResetUtil.GetNextDailyResetUtc(DateTime.UtcNow).Ticks;

            GameData.Instance.SaveNow();

            NotifyFreePackStateChanged();
            return;
        }


        int finalPrice = item.GetFinalPrice();

        if (item.costType == CostType.Gems && wallet.Gems < finalPrice)
        {
            Debug.Log("Not enough gems");
            return;
        }

        if (item.costType == CostType.Gold && wallet.Gold < finalPrice)
        {
            Debug.Log("Not enough gold");
            return;
        }


        if (item.chestReward != null && chestOpeningUI != null)
        {
            chestOpeningUI.gameObject.SetActive(true);
            chestOpeningUI.Show(item.chestReward);
        }
        switch (item.category)
        {
            case StoreCategory.BuyGoldWithGems:
                wallet.SpendGems(finalPrice);
                wallet.AddGold(item.goldAmount);
                break;
            case StoreCategory.BuyChestsWithGold:
                wallet.SpendGold(finalPrice);
                if (chestOpeningUI != null)
                {
                    chestOpeningUI.gameObject.SetActive(true);
                    chestOpeningUI.Show(item.chestReward);
                }
                break;
            case StoreCategory.BuyPartWithGold:
                wallet.SpendGold(finalPrice);
                GrantPartRewards(item);
                GameData.Instance.SaveNow();
                break;
            case StoreCategory.BuyScrollsWithGold:
                wallet.SpendGold(finalPrice);
                wallet.AddScrolls(item.scrollAmount);
                GameData.Instance.SaveNow();
                break;
            case StoreCategory.Specials:
                break;
            default:
                break;
        }

        if (MissionsManager.Instance != null)
            MissionsManager.Instance.ReportAction(MissionAction.PurchaseFromStore);

        OnPurchaseCompleted?.Invoke();
    }
    private void GrantPartRewards(StoreItemDefinition item)
    {
        if (item == null || item.partRewards == null || partsInventory == null)
            return;

        foreach (var entry in item.partRewards)
        {
            if (entry == null || entry.part == null || entry.amount <= 0)
                continue;

            partsInventory.AddPart(entry.part, entry.amount);
        }
    }
    public bool HasUnclaimedDailyFreePack(StoreItemDefinition[] items)
    {
        if (items == null) return false;

        foreach (var item in items)
        {
            if (item == null) continue;
            if (!item.isDailyFree) continue;
            if (CanClaimDailyFree(item))
                return true;
        }
        return false;
    }
    private void NotifyFreePackStateChanged()
    {
        OnFreePackStateChanged?.Invoke();
    }
}
