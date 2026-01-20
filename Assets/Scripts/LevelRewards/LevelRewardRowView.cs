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
    [SerializeField] private GameObject rowHighlight;


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
    [SerializeField] private GameObject rightLockOverlay;
    [SerializeField] private GameObject rightClaimedCheckmark;

    [Header("Row Backgrounds")]
    [SerializeField] private GameObject evenRowBackground;
    [SerializeField] private GameObject oddRowBackground;


    private int _level;
    private LevelRewardsDatabase _db;
    private ILevelRewardsProgress _progress;

    public void Bind(
        int level,
        int rowIndex,
        LevelRewardsDatabase db,
        ILevelRewardsProgress progress,
        bool isCurrentLevelHighlight)
    {
        _level = level;
        _db = db;
        _progress = progress;

        if (levelText != null)
            levelText.text = level.ToString();

        // Alternating row backgrounds
        bool isEven = (rowIndex % 2 == 0);
        if (evenRowBackground != null)
            evenRowBackground.SetActive(isEven);
        if (oddRowBackground != null)
            oddRowBackground.SetActive(!isEven);
        // highlight the entire row
        if (rowHighlight != null)
            rowHighlight.SetActive(isCurrentLevelHighlight);

        // Right reward (always expected)
        var rightReward = db.GetReward(level, RewardLane.Right);
        ApplyRewardToLane(
            lane: RewardLane.Right,
            reward: rightReward,
            root: rightRoot,
            button: rightButton,
            icon: rightIcon,
            lockOverlay: rightLockOverlay,
            claimedCheckmark: rightClaimedCheckmark
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
            claimedCheckmark: leftClaimedCheckmark
        );
    }

    private void ApplyRewardToLane(
        RewardLane lane,
        LevelRewardEntry reward,
        GameObject root,
        Button button,
        Image icon,
        GameObject lockOverlay,
        GameObject claimedCheckmark)
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

        bool claimed = _progress.IsClaimed(_level, lane);
        bool unlocked = _progress.PlayerLevel >= _level;
        bool canClaim = unlocked && !claimed;


        if (claimedCheckmark != null)
            claimedCheckmark.SetActive(claimed);

        // Lock overlay (only if not unlocked yet)
        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        // Button state
        if (button != null)
        {
            button.interactable = canClaim;
            button.onClick.RemoveAllListeners();

            if (canClaim)
                button.onClick.AddListener(() => _progress.TryClaim(_level, lane, button));
        }
    }
}
