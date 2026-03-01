using System;
using System.Collections;
using UnityEngine;

public class ChestRewardsSpawner : MonoBehaviour
{
    [Header("References")]
    public RectTransform chestAnchor;
    public RectTransform rewardsGrid;
    public RewardItemUI rewardItemPrefab;
    public Canvas rootCanvas;

    [Header("Animation")]
    public float flyDuration = 0.6f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float startScale = 0.4f;
    public float endScale = 1.0f;
    public float delayBetweenItems = 0.1f;

    public Action OnRewardsFinished;

    private RewardItemUI _goldItem;
    private RewardItemUI _gemsItem;
    private RewardItemUI _partItem;
    private RewardItemUI _scrollsItem;
    private RewardItemUI _wheelTokenItem;

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
    }

    public void SpawnRewards(ChestReward reward)
    {
        StartCoroutine(SpawnRewardsRoutine(reward));
    }

    // FX sources (used by ChestOpener on Collect)
    public RectTransform GetGoldFxFrom() => _goldItem != null ? _goldItem.FxFrom : null;
    public RectTransform GetGemsFxFrom() => _gemsItem != null ? _gemsItem.FxFrom : null;
    public RectTransform GetPartFxFrom() => _partItem != null ? _partItem.FxFrom : null;
    public RectTransform GetScrollsFxFrom() => _scrollsItem != null ? _scrollsItem.FxFrom : null;

    public RectTransform GetWheelTokenFxFrom() => _wheelTokenItem != null ? _wheelTokenItem.FxFrom : null;

    private IEnumerator SpawnRewardsRoutine(ChestReward reward)
    {
        ClearGrid();

        _goldItem = null;
        _gemsItem = null;
        _partItem = null;
        _scrollsItem = null;
        _wheelTokenItem = null;

        bool spawnedAny = false;

        if (reward.gold > 0)
        {
            spawnedAny = true;
            yield return StartCoroutine(SpawnSingleItem(ui =>
            {
                int boostedGold = MasteryBonusManager.Instance.GetBoostedGold(reward.gold);
                ui.SetupGold(boostedGold);
                _goldItem = ui;
            }));
        }

        if (reward.gems > 0)
        {
            spawnedAny = true;
            yield return StartCoroutine(SpawnSingleItem(ui =>
            {
                ui.SetupDiamonds(reward.gems);
                _gemsItem = ui;
            }));
        }

        if (reward.scrolls > 0)
        {
            spawnedAny = true;
            yield return StartCoroutine(SpawnSingleItem(ui =>
            {
                ui.SetupScrolls(reward.scrolls);
                _scrollsItem = ui;
            }));
        }

        if (reward.wheelTokens > 0)
        {
            spawnedAny = true;
            yield return StartCoroutine(SpawnSingleItem(ui =>
            {
                ui.SetupWheelToken(reward.wheelTokens);
                _wheelTokenItem = ui;
            }));
        }

        if (reward.hasPart && reward.part != null)
        {
            spawnedAny = true;
            yield return StartCoroutine(SpawnSingleItem(ui =>
            {
                ui.SetupPart(reward.part, reward.partRarity);
                _partItem = ui;
            }));
        }

        if (spawnedAny && delayBetweenItems > 0f)
            yield return new WaitForSeconds(delayBetweenItems);

        OnRewardsFinished?.Invoke();
    }

    private IEnumerator SpawnSingleItem(Action<RewardItemUI> setupAction)
    {
        if (rewardItemPrefab == null || chestAnchor == null || rewardsGrid == null || rootCanvas == null)
            yield break;

        RewardItemUI item = Instantiate(rewardItemPrefab, rootCanvas.transform);
        RectTransform itemRect = item.GetComponent<RectTransform>();

        Vector3 startPos = chestAnchor.position;
        Vector3 targetPos = rewardsGrid.position;

        itemRect.position = startPos;
        itemRect.localScale = Vector3.one * startScale;

        setupAction?.Invoke(item);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, flyDuration);
            float eased = flyCurve.Evaluate(Mathf.Clamp01(t));
            itemRect.position = Vector3.Lerp(startPos, targetPos, eased);
            itemRect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
            yield return null;
        }

        itemRect.SetParent(rewardsGrid, worldPositionStays: false);
        itemRect.localScale = Vector3.one * endScale;
    }

    private void ClearGrid()
    {
        for (int i = rewardsGrid.childCount - 1; i >= 0; i--)
            Destroy(rewardsGrid.GetChild(i).gameObject);
    }
}
