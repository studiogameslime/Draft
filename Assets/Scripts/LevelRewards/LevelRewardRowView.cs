using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LevelRewardRowView : MonoBehaviour
{
    [Header("Row")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject rowHighlight;

    [Header("Progress (between this level and next)")]
    [SerializeField] private Image progressFill;

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

    [Header("Anchor")]
    [SerializeField] private RectTransform levelAnchor;
    public RectTransform LevelAnchor => levelAnchor;

    private int _level;
    private LevelRewardsDatabase _db;
    private ILevelRewardsProgress _progress;

    public void Bind(
        int level,
        int rowIndex,
        LevelRewardsDatabase db,
        ILevelRewardsProgress progress,
        bool isCurrentLevelHighlight,
        int playerLevel,
        float currentLevelXp01)
    {
        _level = level;
        _db = db;
        _progress = progress;

        if (levelText != null)
            levelText.text = level.ToString();

        bool isEven = (rowIndex % 2 == 0);
        if (evenRowBackground != null) evenRowBackground.SetActive(isEven);
        if (oddRowBackground != null) oddRowBackground.SetActive(!isEven);

        if (rowHighlight != null)
            rowHighlight.SetActive(isCurrentLevelHighlight);

        // NEW: per-row progress
        SetProgress(playerLevel, currentLevelXp01);

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

    private void SetProgress(int playerLevel, float currentLevelXp01)
    {
        if (progressFill == null) return;

        float fill01;
        if (_level < playerLevel) fill01 = 1f;
        else if (_level == playerLevel) fill01 = Mathf.Clamp01(currentLevelXp01);
        else fill01 = 0f;

        progressFill.fillAmount = fill01;
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
        if (root == null) return;

        if (reward == null)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);



        switch (reward.type)
        {
            case RewardType.Gold:
                {
                    icon.sprite = SpriteManager.instance.goldSprite;
                    icon.SetNativeSize();
                    icon.rectTransform.sizeDelta = new Vector2(icon.rectTransform.sizeDelta.x * 0.2f, icon.rectTransform.sizeDelta.y * 0.2f);
                    break;
                }
            case RewardType.Gems:
                {
                    icon.sprite = SpriteManager.instance.gemSprite;
                    icon.SetNativeSize();
                    icon.rectTransform.sizeDelta = new Vector2(icon.rectTransform.sizeDelta.x * 0.2f, icon.rectTransform.sizeDelta.y * 0.2f);
                    break;
                }
            case RewardType.Chest:
                {
                    icon.sprite = reward.chest.buttonIcon;
                    icon.SetNativeSize();
                    icon.rectTransform.sizeDelta = new Vector2(icon.rectTransform.sizeDelta.x * 0.25f, icon.rectTransform.sizeDelta.y * 0.25f);
                    break;
                }
        }

        bool claimed = _progress.IsClaimed(_level, lane);
        bool unlocked = _progress.PlayerLevel >= _level;
        bool canClaim = unlocked && !claimed;

        if (claimedCheckmark != null)
            claimedCheckmark.SetActive(claimed);

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);

        if (button != null)
        {
            button.interactable = canClaim;
            button.onClick.RemoveAllListeners();
            if (canClaim)
                button.onClick.AddListener(() => _progress.TryClaim(_level, lane, button));
        }
    }
}
