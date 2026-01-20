using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the level rewards window.
/// - Creates rows 1..maxLevel under ScrollView/Content
/// - Updates progress fill and current level number
/// - Refreshes claim states after each claim
/// - Auto scrolls to current level row
/// </summary>
public class LevelRewardWindowController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelRewardsDatabase rewardsDatabase;
    [SerializeField] private LevelRewardsProgressController progressController;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;              // Assign your ScrollView's ScrollRect
    [SerializeField] private RectTransform viewport;             // Usually ScrollRect.viewport
    [SerializeField] private RectTransform content;              // ScrollView/Viewport/Content
    [SerializeField] private LevelRewardRowView rowPrefab;

    [Header("Center Progress UI")]
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private Image progressFill;

    [Header("Auto Scroll")]
    [SerializeField] private bool autoScrollOnOpen = true;
    [SerializeField] private bool smoothAutoScroll = true;
    [SerializeField] private float smoothDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] private float viewportAnchor = 0.5f; // 0=top, 0.5=center, 1=bottom

    private readonly List<LevelRewardRowView> _rows = new();
    private Coroutine _scrollRoutine;

    private void Awake()
    {
        if (progressController != null)
            progressController.OnClaimed += HandleClaimed;
    }

    private void Start()
    {
        Build();
        RefreshAll();
        StartCoroutine(ScrollToCurrentLevelNextFrame());
    }

    private void OnDestroy()
    {
        if (progressController != null)
            progressController.OnClaimed -= HandleClaimed;
    }

    public void Build()
    {
        if (rewardsDatabase == null)
        {
            Debug.LogError("[LevelRewardWindowController] rewardsDatabase is not assigned.");
            return;
        }
        if (progressController == null)
        {
            Debug.LogError("[LevelRewardWindowController] progressController is not assigned.");
            return;
        }
        if (content == null || rowPrefab == null)
        {
            Debug.LogError("[LevelRewardWindowController] content/rowPrefab not assigned.");
            return;
        }

        // Clear existing children
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        _rows.Clear();

        // Create rows 1..maxLevel
        for (int lvl = 1; lvl <= rewardsDatabase.maxLevel; lvl++)
        {
            var row = Instantiate(rowPrefab, content);
            _rows.Add(row);
        }

        // IMPORTANT: force layout rebuild so positions are valid
        ForceRebuildLayout();
    }

    public void RefreshAll()
    {
        if (rewardsDatabase == null || progressController == null)
            return;

        int playerLevel = Mathf.Clamp(progressController.PlayerLevel, 1, rewardsDatabase.maxLevel);

        // Center progress UI
        if (levelNumberText != null)
            levelNumberText.text = playerLevel.ToString();

        if (progressFill != null)
        {
            float t = (rewardsDatabase.maxLevel <= 1)
                ? 1f
                : (playerLevel - 1f) / (rewardsDatabase.maxLevel - 1f);

            progressFill.fillAmount = Mathf.Clamp01(t);
        }

        // Rows
        for (int i = 0; i < _rows.Count; i++)
        {
            int level = i + 1;
            bool highlight = (level == playerLevel);

            _rows[i].Bind(
                rowIndex: i,
                level: level,
                db: rewardsDatabase,
                progress: progressController,
                isCurrentLevelHighlight: highlight
            );
        }
    }

    private void HandleClaimed(int level, RewardLane lane)
    {
        RefreshAll();

        // Optional: keep current level centered after claim
        // StartCoroutine(AutoScrollToCurrentLevelNextFrame());
    }

    // =========================
    // AUTO SCROLL
    // =========================

    private IEnumerator AutoScrollToCurrentLevelNextFrame()
    {
        // Wait a frame so UI & layout groups can settle
        yield return null;
        ForceRebuildLayout();

        ScrollToLevel(progressController != null ? progressController.PlayerLevel : 1, smoothAutoScroll);
    }

    public void ScrollToLevel(int level, bool smooth)
    {
        if (scrollRect == null || content == null)
        {
            Debug.LogWarning("[LevelRewardWindowController] Missing scrollRect/content.");
            return;
        }

        int max = rewardsDatabase != null ? rewardsDatabase.maxLevel : _rows.Count;
        level = Mathf.Clamp(level, 1, Mathf.Max(1, max));

        int index = level - 1;
        if (index < 0 || index >= content.childCount)
            return;

        var target = content.GetChild(index) as RectTransform;
        if (target == null)
            return;

        // Calculate normalized position to place target at viewportAnchor
        float normalized = CalculateVerticalNormalizedPositionForTarget(target, viewportAnchor);

        if (_scrollRoutine != null)
            StopCoroutine(_scrollRoutine);

        if (!smooth)
        {
            scrollRect.verticalNormalizedPosition = normalized;
            return;
        }

        _scrollRoutine = StartCoroutine(SmoothScrollRoutine(normalized));
    }

    //private IEnumerator SmoothScrollRoutine(float targetNormalized)
    //{
    //    float start = scrollRect.verticalNormalizedPosition;
    //    float t = 0f;

    //    while (t < 1f)
    //    {
    //        t += Time.unscaledDeltaTime / Mathf.Max(0.01f, smoothDuration);
    //        float eased = EaseOutCubic(Mathf.Clamp01(t));
    //        scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, targetNormalized, eased);
    //        yield return null;
    //    }

    //    scrollRect.verticalNormalizedPosition = targetNormalized;
    //    _scrollRoutine = null;
    //}

    private float CalculateVerticalNormalizedPositionForTarget(RectTransform target, float anchor01)
    {
        // Ensure viewport reference
        RectTransform vp = viewport != null ? viewport : scrollRect.viewport;
        if (vp == null)
            vp = scrollRect.GetComponent<RectTransform>();

        // If content smaller than viewport, no scrolling needed
        float contentHeight = content.rect.height;
        float viewportHeight = vp.rect.height;
        float scrollable = contentHeight - viewportHeight;
        if (scrollable <= 0.01f)
            return 1f; // top

        // target position in content local space:
        // anchoredPosition.y is usually negative as you go down in Vertical Layout Groups
        float targetYFromTop = -target.anchoredPosition.y; // distance from top
        float targetCenterOffset = target.rect.height * 0.5f;

        // where we want the target to appear inside viewport:
        float desiredInViewport = viewportHeight * anchor01;

        // compute desired scroll offset from top
        float desiredScrollFromTop = (targetYFromTop + targetCenterOffset) - desiredInViewport;

        // clamp
        desiredScrollFromTop = Mathf.Clamp(desiredScrollFromTop, 0f, scrollable);

        // ScrollRect verticalNormalizedPosition: 1 = top, 0 = bottom
        float normalized = 1f - (desiredScrollFromTop / scrollable);
        return Mathf.Clamp01(normalized);
    }

    private void ForceRebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private float EaseOutCubic(float x)
    {
        float p = 1f - x;
        return 1f - (p * p * p);
    }

    public void ScrollToCurrentLevel(bool smooth)
    {
        int playerLevel = Mathf.Clamp(
            progressController.PlayerLevel,
            1,
            rewardsDatabase.maxLevel
        );

        int index = playerLevel - 1;
        if (index < 0 || index >= _rows.Count)
            return;

        RectTransform rowRect = _rows[index].GetComponent<RectTransform>();
        ScrollToRow(rowRect, smooth);
    }
    private void ScrollToRow(RectTransform row, bool smooth)
    {
        Canvas.ForceUpdateCanvases();

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        // Position of the row inside content (top = 0)
        float rowTop = Mathf.Abs(row.anchoredPosition.y);

        float targetNormalized =
            1f - Mathf.Clamp01(rowTop / (contentHeight - viewportHeight));

        if (smooth)
        {
            StartSmoothScroll(targetNormalized);
        }
        else
        {
            scrollRect.verticalNormalizedPosition = targetNormalized;
        }
    }
    private void StartSmoothScroll(float target)
    {
        if (_scrollRoutine != null)
            StopCoroutine(_scrollRoutine);

        _scrollRoutine = StartCoroutine(SmoothScrollRoutine(target));
    }

    private IEnumerator SmoothScrollRoutine(float target)
    {
        float start = scrollRect.verticalNormalizedPosition;
        float time = 0f;

        while (time < smoothDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / smoothDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            scrollRect.verticalNormalizedPosition =
                Mathf.Lerp(start, target, t);

            yield return null;
        }

        scrollRect.verticalNormalizedPosition = target;
        _scrollRoutine = null;
    }
    private IEnumerator ScrollToCurrentLevelNextFrame()
    {
        // Wait one frame so layout calculations are done
        yield return null;
        ScrollToCurrentLevel(true);
    }




}
