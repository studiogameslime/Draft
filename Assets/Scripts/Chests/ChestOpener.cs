using Firebase.Analytics;
using Unity.VisualScripting;
using UnityEngine;

public class ChestOpener : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChestRewardsSpawner rewardsSpawner;
    [SerializeField] private RectTransform collectButtonFallback;

    [Header("Parts")]
    [Tooltip("Assign the same PlayerPartsInventory asset used by your parts system.")]
    [SerializeField] private PlayerPartsInventory partsInventory;

    [Header("Tokens")]
    [Tooltip("Assign the same WheelOfFortuneTokensManager")]
    [SerializeField] private WheelOfFortuneTokensManager wheelOfFortuneTokensManager;

    private ChestDefinition _currentChest;
    private bool _opened;
    private bool _collected;

    private ChestReward _pendingReward;
    private int _pendingFxCount = 0;

    private void Awake()
    {
        if (rewardsSpawner == null)
            rewardsSpawner = GetComponent<ChestRewardsSpawner>();
    }

    public void SetChest(ChestDefinition chest)
    {
        _currentChest = chest;
        _opened = false;
        _collected = false;
        _pendingReward = default;
        _pendingFxCount = 0;
        Debug.Log("Chest setted");
    }

    public void OpenChest()
    {
        Debug.Log("openChest");
        if (_opened) return;

        if (_currentChest == null)
        {
            Debug.LogWarning("ChestOpener.OpenChest: currentChest is null. Did you call SetChest()?");
            return;
        }

        _opened = true;
        _collected = false;

        _pendingReward = ChestRewardCalculator.RollReward(_currentChest);

        if (rewardsSpawner != null)
            rewardsSpawner.SpawnRewards(_pendingReward);

        Debug.Log($"Chest opened (pending): {_pendingReward}");
    }

    public void OnCollectPressed()
    {
        if (!_opened)
        {
            Debug.LogWarning("ChestOpener.OnCollectPressed: chest not opened yet.");
            return;
        }

        if (_collected) return;
        _collected = true;

        ApplyPendingRewardToPlayer(_pendingReward);
        Debug.Log($"Chest collected: {_pendingReward}");

        _pendingReward = default;

        MissionsManager.Instance.ReportAction(MissionAction.OpenChests, 1);
        PlayerXPManager.Instance.AddXP(10);

        FirebaseAnalytics.LogEvent("Open_chest", new Parameter("chest_type", _currentChest.name));

    }

    private void ApplyPendingRewardToPlayer(ChestReward reward)
    {
        if (PlayerCurrencyWallet.Instance == null)
        {
            CloseScreen();
            return;
        }

        _pendingFxCount = 0;

        if (reward.gold > 0)
        {
            RectTransform from = rewardsSpawner != null ? rewardsSpawner.GetGoldFxFrom() : null;
            if (from == null) from = collectButtonFallback;

            if (from != null && RewardFlyFXManager.Instance != null)
            {
                _pendingFxCount++;
                PlayerCurrencyWallet.Instance.AddGold(reward.gold, from, OnSingleFxFinished);
            }
            else
            {
                PlayerCurrencyWallet.Instance.AddGold(reward.gold, null);
            }
        }

        if (reward.gems > 0)
        {
            RectTransform from = rewardsSpawner != null ? rewardsSpawner.GetGemsFxFrom() : null;
            if (from == null) from = collectButtonFallback;

            if (from != null && RewardFlyFXManager.Instance != null)
            {
                _pendingFxCount++;
                PlayerCurrencyWallet.Instance.AddGems(reward.gems, from, OnSingleFxFinished);
            }
            else
            {
                PlayerCurrencyWallet.Instance.AddGems(reward.gems, null);
            }
        }

        if (reward.hasPart && reward.part != null)
        {
            RectTransform from = rewardsSpawner != null ? rewardsSpawner.GetPartFxFrom() : null;
            if (from == null) from = collectButtonFallback;

            RewardItemUI partUI = (from != null) ? from.GetComponentInParent<RewardItemUI>() : null;
            RectTransform flyMe = partUI != null ? partUI.PartVisualToFly : null;

            bool canFx = (flyMe != null && RewardFlyFXManager.Instance != null);

            if (canFx)
            {
                _pendingFxCount++;

                RewardFlyFXManager.Instance.FlyExistingToTarget(
                    RewardType.Part,
                    flyMe,
                    onComplete: () =>
                    {
                        if (partsInventory != null)
                            partsInventory.AddPart(reward.part, 1);

                        OnSingleFxFinished();
                    }
                );
            }
            else
            {
                if (partsInventory != null)
                    partsInventory.AddPart(reward.part, 1);
            }
        }

        // Wheel token (no FX for now, just grant and save)
        if (reward.wheelTokens > 0)
        {
            wheelOfFortuneTokensManager.AddWheelToken();
        }


        if (_pendingFxCount <= 0)
            CloseScreen();
    }

    private void OnSingleFxFinished()
    {
        _pendingFxCount--;
        if (_pendingFxCount <= 0)
            CloseScreen();
    }

    private void CloseScreen()
    {
        gameObject.SetActive(false);
    }
}
