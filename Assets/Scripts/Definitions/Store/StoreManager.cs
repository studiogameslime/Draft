using System;
using System.Reflection;
using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [SerializeField] private ChestOpeningUI chestOpeningUI;

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
            {
                Debug.Log("Daily free gold not ready yet");
                return;
            }

            // grant reward
            wallet.AddGold(item.goldAmount);

            // set next reset
            save.nextDailyFreeGoldUtcTicks = DailyResetUtil.GetNextResetTicks();
            GameData.Instance.SaveNow();
            return;
        }

        if (item.costType == CostType.Gems && wallet.Gems < item.priceInGems)
        {
            Debug.Log("Not enough gems");
            return;
        }
        if (item.costType == CostType.Gold && wallet.Gold < item.priceInGold)
        {
            Debug.Log("Not enough gems");
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
                wallet.SpendGems(item.priceInGems);
                wallet.AddGold(item.goldAmount);
                break;
            case StoreCategory.BuyChestsWithGold:
                wallet.SpendGold(item.priceInGold);
                chestOpeningUI.gameObject.SetActive(true);
                chestOpeningUI.Show(item.chestReward);
                break;
            case StoreCategory.BuyPartWithGold:
                break;
            case StoreCategory.Specials:
                break;
            default:
                break;
        }

    }
}
