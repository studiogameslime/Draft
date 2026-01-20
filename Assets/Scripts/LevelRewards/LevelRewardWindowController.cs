using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the level rewards window.
/// - Creates rows 1..maxLevel under ScrollView/Content
/// - Updates progress fill and current level number
/// - Refreshes claim states after each claim
/// </summary>
public class LevelRewardWindowController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelRewardsDatabase rewardsDatabase;
    [SerializeField] private LevelRewardsProgressController progressController;

    [Header("Scroll")]
    [SerializeField] private RectTransform content;
    [SerializeField] private LevelRewardRowView rowPrefab;

    [Header("Center Progress UI")]
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private Image progressFill;

    private readonly List<LevelRewardRowView> _rows = new();

    private void Awake()
    {
        if (progressController != null)
            progressController.OnClaimed += HandleClaimed;
    }

    private void Start()
    {
        Build();
        RefreshAll();
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
            // Fill is 0..1 across maxLevel
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
                level: level,
                rowIndex: i,
                db: rewardsDatabase,
                progress: progressController,
                isCurrentLevelHighlight: highlight
            );
        }
    }

    private void HandleClaimed(int level, RewardLane lane)
    {
        // After claim, refresh everything so checkmarks/locks update
        RefreshAll();
    }
}
