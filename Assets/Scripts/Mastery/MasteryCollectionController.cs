using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasteryCollectionController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MasteryDatabase database;

    [Header("Grid")]
    [SerializeField] private Transform content;
    [SerializeField] private MasteryItemView itemPrefab;

    [Header("Draw Button")]
    [SerializeField] private Button drawButton;
    [SerializeField] private TMP_Text drawCostText;

    [Header("Rarity Colors")]
    [SerializeField] private RarityColorSettings rarityColors = new RarityColorSettings();

    [Header("Popup")]
    [SerializeField] private MasteryItemDetailsPopup detailsPopup;

    [Header("Free Draw Ad")]
    [SerializeField] private Button freeDrawButton;

    [Header("Locked UI")]
    [Tooltip("Sprite used for locked mastery (level 0).")]
    [SerializeField] private Sprite lockSprite;

    [Header("Roll Animation (on existing cards)")]
    [SerializeField] private float rollDuration = 1.0f;
    [SerializeField] private float startStep = 0.05f;
    [SerializeField] private float stepIncrease = 0.008f;
    [SerializeField] private float maxStep = 0.12f;
    [SerializeField, Range(0.2f, 1f)] private float dimAlpha = 0.35f;
    [SerializeField] private float highlightScale = 1.06f;
    [SerializeField] private float finalHoldTime = 0.8f;
    [SerializeField] private bool openDetailsOnResult = true;

    private Action<int> _goldListener;
    private bool _isRolling;
    private bool _freeDrawRewardGranted;

    private readonly List<MasteryItemView> _views = new List<MasteryItemView>();
    private readonly Dictionary<string, MasteryItemView> _viewById = new Dictionary<string, MasteryItemView>();

    private IEnumerator Start()
    {
        BuildGrid();

        if (drawButton)
            drawButton.onClick.AddListener(OnDrawClicked);

        if (freeDrawButton)
            freeDrawButton.onClick.AddListener(OnFreeDrawClicked);

        yield return null;

        if (PlayerCurrencyWallet.Instance != null)
        {
            _goldListener = _ => RefreshDrawCost();
            PlayerCurrencyWallet.Instance.OnGoldChanged += _goldListener;
        }

        RefreshDrawCost();
        RefreshFreeDrawButton();
    }

    private void OnEnable()
    {
        BuildGrid();
        RefreshDrawCost();
        RefreshFreeDrawButton();
    }

    private void OnDestroy()
    {
        if (PlayerCurrencyWallet.Instance != null && _goldListener != null)
            PlayerCurrencyWallet.Instance.OnGoldChanged -= _goldListener;
    }

    private void OnDrawClicked()
    {
        if (_isRolling) return;
        if (database == null) return;
        if (!MasterySystem.CanDraw(database)) return;

        StartCoroutine(DrawRoutine());
    }

    private IEnumerator DrawRoutine()
    {
        _isRolling = true;

        if (drawButton) drawButton.interactable = false;

        // Pick the final result first (preview only) so we can stop on it
        var picked = MasterySystem.PreviewPick(database);
        if (picked == null || string.IsNullOrEmpty(picked.id))
        {
            _isRolling = false;
            RefreshDrawCost();
            yield break;
        }

        // Find the final card view (ensure we stop on the right one)
        if (!_viewById.TryGetValue(picked.id, out var finalView) || finalView == null)
        {
            BuildGrid();
            _viewById.TryGetValue(picked.id, out finalView);
        }

        if (finalView == null)
        {
            // Fallback: commit without animation
            MasterySystem.CommitDraw(database, picked);
            BuildGrid();
            _isRolling = false;
            RefreshDrawCost();
            yield break;
        }

        DimAll(true);

        yield return PlayRollOnCards(finalView);

        MasterySystem.CommitDraw(database, picked);

        finalView.SetHighlighted(true, highlightScale, dimAlpha);

        if (openDetailsOnResult && detailsPopup != null)
        {
            int newLevel = MasteryProgress.GetLevel(picked.id);
            detailsPopup.Open(picked, newLevel, finalView.transform as RectTransform);
        }

        yield return new WaitForSecondsRealtime(finalHoldTime);

        finalView.SetHighlighted(false, highlightScale, dimAlpha);
        DimAll(false);
        ResetAllScales();

        BuildGrid();
        _isRolling = false;
        RefreshDrawCost();
    }

    private IEnumerator PlayRollOnCards(MasteryItemView finalView)
    {
        if (_views.Count == 0) yield break;

        float t = 0f;
        float step = startStep;
        MasteryItemView current = null;

        while (t < rollDuration)
        {
            if (current != null)
                current.SetHighlighted(false, highlightScale, dimAlpha);

            current = _views[UnityEngine.Random.Range(0, _views.Count)];
            current.SetHighlighted(true, highlightScale, dimAlpha);

            yield return new WaitForSecondsRealtime(step);

            t += step;
            step = Mathf.Min(maxStep, step + stepIncrease);
        }

        if (current != null)
            current.SetHighlighted(false, highlightScale, dimAlpha);

        finalView.SetHighlighted(true, highlightScale, dimAlpha);
        yield return new WaitForSecondsRealtime(0.20f);
        finalView.SetHighlighted(false, highlightScale, dimAlpha);
    }

    // ============================
    // FREE DRAW (Ad Reward)
    // ============================

    private bool IsFreeMasteryDrawAvailable()
    {
        var save = GameData.Instance?.Save;
        if (save == null) return false;
        long nowTicks = System.DateTime.UtcNow.Ticks;
        return nowTicks >= save.nextFreeMasteryDrawUtcTicks;
    }

    private void OnFreeDrawClicked()
    {
        if (_isRolling) return;
        if (database == null) return;
        if (!MasterySystem.HasAnyEligiblePick(database)) return;
        if (!IsFreeMasteryDrawAvailable()) return;

        _freeDrawRewardGranted = false;

        bool started = AdsManager.Instance.ShowRewarded(
            AdRewardType.FreeMasteryDraw,
            onReward: () => { _freeDrawRewardGranted = true; },
            onClosed: () => { StartCoroutine(AfterFreeDrawAdClosed()); });

        if (!started)
            return;

        if (freeDrawButton) freeDrawButton.interactable = false;
    }

    private IEnumerator AfterFreeDrawAdClosed()
    {
        yield return null;

        if (_freeDrawRewardGranted)
        {
            yield return FreeDrawRoutine();

            // Set next available time to tomorrow midnight UTC
            var tomorrow = System.DateTime.UtcNow.Date.AddDays(1);
            GameData.Instance.Save.nextFreeMasteryDrawUtcTicks = tomorrow.Ticks;
            GameData.Instance.SaveNow();
        }

        RefreshFreeDrawButton();
    }

    private IEnumerator FreeDrawRoutine()
    {
        _isRolling = true;

        if (drawButton) drawButton.interactable = false;
        if (freeDrawButton) freeDrawButton.interactable = false;

        var picked = MasterySystem.PreviewPick(database);
        if (picked == null || string.IsNullOrEmpty(picked.id))
        {
            _isRolling = false;
            RefreshDrawCost();
            RefreshFreeDrawButton();
            yield break;
        }

        if (!_viewById.TryGetValue(picked.id, out var finalView) || finalView == null)
        {
            BuildGrid();
            _viewById.TryGetValue(picked.id, out finalView);
        }

        if (finalView == null)
        {
            MasterySystem.CommitFreeDraw(database, picked);
            BuildGrid();
            _isRolling = false;
            RefreshDrawCost();
            RefreshFreeDrawButton();
            yield break;
        }

        DimAll(true);

        yield return PlayRollOnCards(finalView);

        MasterySystem.CommitFreeDraw(database, picked);

        finalView.SetHighlighted(true, highlightScale, dimAlpha);

        if (openDetailsOnResult && detailsPopup != null)
        {
            int newLevel = MasteryProgress.GetLevel(picked.id);
            detailsPopup.Open(picked, newLevel, finalView.transform as RectTransform);
        }

        yield return new WaitForSecondsRealtime(finalHoldTime);

        finalView.SetHighlighted(false, highlightScale, dimAlpha);
        DimAll(false);
        ResetAllScales();

        BuildGrid();
        _isRolling = false;
        RefreshDrawCost();
        RefreshFreeDrawButton();
    }

    private void RefreshFreeDrawButton()
    {
        if (freeDrawButton == null) return;

        bool available = IsFreeMasteryDrawAvailable() && MasterySystem.HasAnyEligiblePick(database);

        if (!_isRolling)
            freeDrawButton.interactable = available;
    }

    private void RefreshDrawCost()
    {
        int cost = MasterySystem.GetDrawCost(database);

        bool canAfford = PlayerCurrencyWallet.Instance != null && PlayerCurrencyWallet.Instance.Gold >= cost;

        if (drawCostText)
        {
            drawCostText.text = cost.ToString();
            drawCostText.color = canAfford ? Color.white : Color.red;
        }

        if (drawButton && !_isRolling)
            drawButton.interactable = canAfford && MasterySystem.HasAnyEligiblePick(database);
    }

    private void BuildGrid()
    {
        if (database == null || content == null || itemPrefab == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        _views.Clear();
        _viewById.Clear();

        // Sort by rarity: Epic -> Rare -> Common
        var defs = new List<MasteryItemDefinition>(database.items);
        defs.Sort((a, b) =>
        {
            int rb = RarityRank(b != null ? b.rarity : MasteryRarity.Common);
            int ra = RarityRank(a != null ? a.rarity : MasteryRarity.Common);
            if (rb != ra) return rb.CompareTo(ra);
            string an = a != null ? a.displayName : "";
            string bn = b != null ? b.displayName : "";
            return string.Compare(an, bn, StringComparison.Ordinal);
        });

        foreach (var def in defs)
        {
            if (def == null) continue;

            var view = Instantiate(itemPrefab, content);
            int level = MasteryProgress.GetLevel(def.id);

            // Pass lock sprite so the view can show it for level 0
            view.SetData(def, level, lockSprite);

            view.SetRarityColor(rarityColors.GetColor(def.rarity));
            view.OnClicked += OnItemClicked;

            _views.Add(view);
            if (!string.IsNullOrEmpty(view.Id))
                _viewById[view.Id] = view;

            view.SetDimmed(false, dimAlpha);
            view.transform.localScale = Vector3.one;
        }
    }

    private int RarityRank(MasteryRarity r)
    {
        return r switch
        {
            MasteryRarity.Epic => 2,
            MasteryRarity.Rare => 1,
            _ => 0
        };
    }

    private void OnItemClicked(MasteryItemDefinition def, int level, RectTransform fromRect)
    {
        // Ignore clicks while rolling
        if (_isRolling) return;

        // Ignore clicks on locked items (level 0)
        if (level <= 0) return;

        if (detailsPopup != null)
            detailsPopup.Open(def, level, fromRect);
    }

    private void DimAll(bool dim)
    {
        for (int i = 0; i < _views.Count; i++)
            _views[i].SetDimmed(dim, dimAlpha);
    }

    private void ResetAllScales()
    {
        for (int i = 0; i < _views.Count; i++)
            _views[i].transform.localScale = Vector3.one;
    }

    [Serializable]
    public class RarityColorSettings
    {
        public Color common = new Color(0.95f, 0.92f, 0.82f, 1f);
        public Color rare = new Color(0.55f, 0.80f, 1.00f, 1f);
        public Color epic = new Color(0.75f, 0.55f, 1.00f, 1f);

        public Color GetColor(MasteryRarity rarity)
        {
            return rarity switch
            {
                MasteryRarity.Rare => rare,
                MasteryRarity.Epic => epic,
                _ => common
            };
        }
    }
}
