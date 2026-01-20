using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls one level row UI.
/// Expects the hierarchy you showed: left reward block + right reward block.
/// </summary>
public class LevelRewardRowView : MonoBehaviour
{
    [Header("Row")]
    [SerializeField] private TMP_Text levelText;

    [Header("Left (Special)")]
    [SerializeField] private GameObject leftRoot;
    [SerializeField] private Button leftButton;
    [SerializeField] private Image leftIcon;
    [SerializeField] private GameObject leftLockOverlay;
    [SerializeField] private GameObject leftClaimedCheckmark;

    [Header("Right (Main)")]
    [SerializeField] private GameObject rightRoot;
    [SerializeField] private Button rightButton;
    [SerializeField] private Image rightIcon;
    [SerializeField] private GameObject rightHighlight;
    [SerializeField] private GameObject rightClaimedCheckmark;

    private int _level;
    private LevelRewardsDatabase _db;
    private ILevelRewardsProgress _progress;

    public void Bind(
        int level,
        LevelRewardsDatabase db,
        ILevelRewardsProgress progress,
        bool isCurrentLevelHighlight)
    {
        _level = level;
        _db = db;
        _progress = progress;

        if (levelText != null)
            levelText.text = level.ToString();

        // Right reward (always expected)
        var rightReward = db.GetReward(level, RewardLane.Right);
        ApplyRewardToLane(
            lane: RewardLane.Right,
            reward: rightReward,
            root: rightRoot,
            button: rightButton,
            icon: rightIcon,
            lockOverlay: null,
            claimedCheckmark: rightClaimedCheckmark,
            highlight: rightHighlight,
            highlightOn: isCurrentLevelHighlight
        );

        // Left reward (optional)
        var leftReward = db.GetReward(level, RewardLane.Left);
        ApplyRewardToLane(
            lane: RewardLane.Left,
            reward: leftReward,
            root: leftRoot,
            button: leftButton,
            icon: leftIcon,
            lockOverlay: leftLockOverlay,
            claimedCheckmark: leftClaimedCheckmark,
            highlight: null,
            highlightOn: false
        );
    }

    private void ApplyRewardToLane(
        RewardLane lane,
        LevelRewardEntry reward,
        GameObject root,
        Button button,
        Image icon,
        GameObject lockOverlay,
        GameObject claimedCheckmark,
        GameObject highlight,
        bool highlightOn)
    {
        if (root == null)
            return;

        // If no reward on this lane, hide it completely.
        if (reward == null)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);

        if (icon != null)
            icon.sprite = reward.icon;

        if (highlight != null)
            highlight.SetActive(highlightOn);

        bool canClaim = _progress.CanClaim(_level, lane);
        bool claimed = _progress.IsClaimed(_level, lane);

        if (claimedCheckmark != null)
            claimedCheckmark.SetActive(claimed);

        if (lockOverlay != null)
            lockOverlay.SetActive(!canClaim && !claimed);

        if (button != null)
        {
            button.interactable = canClaim && !claimed;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _progress.TryClaim(_level, lane,button));
        }
    }
}
