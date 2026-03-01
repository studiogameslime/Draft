using UnityEngine;
using TMPro;

public class WheelOfFortuneTokensManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text tokensText;

    [Header("Rewards - Optional references")]
    [Tooltip("If you want to support Chest rewards now, assign the ChestOpener from your popup hierarchy.")]
    [SerializeField] private ChestOpeningUI chestOpeningUI;

    [Tooltip("If you want to support Part rewards now, assign the PlayerPartsInventory asset.")]
    [SerializeField] private PlayerPartsInventory partsInventory;

    public int Tokens
    {
        get
        {
            if (GameData.Instance == null || GameData.Instance.Save == null)
                return 0;
            return GameData.Instance.Save.wheelOfFortuneTokens;
        }
    }

    private void OnEnable()
    {
        RefreshTokensUI();
    }

    public void RefreshTokensUI()
    {
        if (tokensText == null) return;
        tokensText.text = Tokens.ToString();
    }

    // Returns true if token was consumed and spin is allowed
    public bool TryConsumeSpinToken(int cost = 1)
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return false;

        if (cost <= 0) cost = 1;
        if (GameData.Instance.Save.wheelOfFortuneTokens < cost)
            return false;

        GameData.Instance.Save.wheelOfFortuneTokens -= cost;
        if (GameData.Instance.Save.wheelOfFortuneTokens < 0)
            GameData.Instance.Save.wheelOfFortuneTokens = 0;

        MissionsManager.Instance.ReportAction(MissionAction.SpinWheelOfFortune, 1);
        GameData.Instance.SaveNow();
        RefreshTokensUI();
        return true;
    }

    public void AddWheelToken()
    {
        if (GameData.Instance != null && GameData.Instance.Save != null)
        {
            GameData.Instance.Save.wheelOfFortuneTokens += 1;
            GameData.Instance.SaveNow();
        }
    }

    /// <summary>
    /// Single entry point for granting a wheel reward.
    /// PickerWheel calls this once at the end of the spin.
    /// </summary>
    public void GrantReward(FortuneWheelSlotDefinition slotDef, RectTransform fromRect)
    {
        if (slotDef == null || slotDef.reward == null)
            return;

        if (PlayerCurrencyWallet.Instance == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: PlayerCurrencyWallet.Instance is missing.");
            return;
        }

        FortuneWheelReward reward = slotDef.reward;
        int amount = Mathf.Max(0, reward.amount);

        switch (reward.type)
        {
            case FortuneRewardType.Gold:
                if (amount > 0)
                {
                    int boostedGold = MasteryBonusManager.Instance.GetBoostedGold(amount);
                    PlayerCurrencyWallet.Instance.AddGold(boostedGold, fromRect);
                }
                break;

            case FortuneRewardType.Gems:
                if (amount > 0)
                    PlayerCurrencyWallet.Instance.AddGems(amount, fromRect);
                break;

            case FortuneRewardType.Scrolls:
                if (amount > 0)
                    PlayerCurrencyWallet.Instance.AddScrolls(amount, fromRect);
                break;

            case FortuneRewardType.Chest:
                OpenChest(reward.chestDefinition);
                break;

            case FortuneRewardType.GreenPart:
                GrantPartReward();
                break;

            default:
                Debug.LogWarning("WheelOfFortuneTokensManager: Unsupported reward type: " + reward.type);
                break;
        }
    }

    // --------------------------
    // Parts
    // --------------------------
    private void GrantPartReward()
    {
        // This assumes you have PartDropManager in the project.
        // If you don't, this will safely warn and do nothing.
        if (partsInventory == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: partsInventory is not assigned.");
            return;
        }

        if (PartDropManager.Instance == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: PartDropManager.Instance is missing (cannot roll a random part).");
            return;
        }

        // Green part only for now (as your enum says "GreenPart")
        PartDefinition part = PartDropManager.Instance.GetRandomPartForDeckUnits(PartRarity.Common);
        if (part == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: PartDropManager returned null part.");
            return;
        }

        partsInventory.AddPart(part, 1);
    }

    // --------------------------
    // Chests
    // --------------------------
    private void OpenChest(ChestDefinition chestDef)
    {
        if (chestDef == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: ChestDefinition is null.");
            return;
        }

        if (chestOpeningUI == null)
        {
            Debug.LogWarning("WheelOfFortuneTokensManager: chestOpeningUI is not assigned.");
            return;
        }

        // Open immediately (same flow as your ChestOpener design)
        chestOpeningUI.gameObject.SetActive(true);
        chestOpeningUI.Show(chestDef);
    }
}
