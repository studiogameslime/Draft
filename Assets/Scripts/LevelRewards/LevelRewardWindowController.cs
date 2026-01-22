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
    [SerializeField] private RectTransform rowContainer; // Content/RowContainer

    [Header("Center Progress UI")]
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private Image progressFill;

    [Header("Auto Scroll")]
    [SerializeField] private bool autoScrollOnOpen = true;
    [SerializeField] private bool smoothAutoScroll = true;
    [SerializeField] private float smoothDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] private float viewportAnchor = 0.5f; // 0=top, 0.5=center, 1=bottom

    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    private readonly List<LevelRewardRowView> _rows = new();
    private Coroutine _scrollRoutine;

    public event System.Action OnRowsBuilt;

    private void Awake()
    {
        if (progressController != null)
            progressController.OnClaimed += HandleClaimed;

        root.SetActive(true); // init
    }

    private void Start()
    {
        Build();
        RefreshAll();
        StartCoroutine(ScrollToCurrentLevelNextFrame());

        root.SetActive(false); // closed by default
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

        // Clear only rows (do NOT delete ProgressBarOverlay)
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
            Destroy(rowContainer.GetChild(i).gameObject);
        _rows.Clear();


        _rows.Clear();

        // Create rows 1..maxLevel
        for (int lvl = 1; lvl <= rewardsDatabase.maxLevel; lvl++)
        {
            var row = Instantiate(rowPrefab, rowContainer);
            _rows.Add(row);
        }

        // IMPORTANT: force layout rebuild so positions are valid
        ForceRebuildLayout();
        OnRowsBuilt?.Invoke();

    }

    public void RefreshAll()
    {
        if (rewardsDatabase == null || progressController == null)
            return;

        int playerLevel = Mathf.Clamp(progressController.PlayerLevel, 1, rewardsDatabase.maxLevel);

        float xp01 = 0f;
        if (PlayerXPManager.Instance != null)
        {
            int next = PlayerXPManager.Instance.GetXPForNextLevel();
            xp01 = (next <= 0) ? 0f : Mathf.Clamp01((float)PlayerXPManager.Instance.currentXP / next);
        }

        if (levelNumberText != null)
            levelNumberText.text = playerLevel.ToString();

        for (int i = 0; i < _rows.Count; i++)
        {
            int level = i + 1;
            bool highlight = (level == playerLevel);

            _rows[i].Bind(
                level: level,
                rowIndex: i,
                db: rewardsDatabase,
                progress: progressController,
                isCurrentLevelHighlight: highlight,
                playerLevel: playerLevel,
                currentLevelXp01: xp01
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
        if (index < 0 || index >= rowContainer.childCount) return;


        var target = rowContainer.GetChild(index) as RectTransform;
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

   
    private float CalculateVerticalNormalizedPositionForTarget(RectTransform target, float anchor01)
    {
        // Ensure viewport reference
        RectTransform vp = viewport != null ? viewport : scrollRect.viewport;
        if (vp == null)
            vp = scrollRect.GetComponent<RectTransform>();

        // If content smaller than viewport, no scrolling needed
        float contentHeight = rowContainer.rect.height;
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
        LayoutRebuilder.ForceRebuildLayoutImmediate(rowContainer);
        Canvas.ForceUpdateCanvases();
    }

    public void ScrollToCurrentLevel(bool smooth)
    {
        int playerLevel = Mathf.Clamp(
            progressController.PlayerLevel,
            1,
            rewardsDatabase.maxLevel
        );

        ScrollToLevel(playerLevel, smooth);
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

    public void OpenFromButton(RectTransform buttonRect)
    {
        if (popupAnimator == null)
        {
            Debug.LogWarning("MissionsScreen: PopupAnimator is not assigned.");
            return;
        }
        root.SetActive(true);

        popupAnimator.OpenFromRect(buttonRect);
        StartCoroutine(ScrollToCurrentLevelNextFrame());

    }


    public void Close()
    {
        if (popupAnimator == null)
            return;

        //root.SetActive(false);
        popupAnimator.Close();
    }

}
