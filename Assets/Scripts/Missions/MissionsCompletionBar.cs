using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionsCompletionBar : MonoBehaviour
{
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Image completeMissionRewardIcon;
    [SerializeField] private Sprite completeDailyMissionRewardIcon;
    [SerializeField] private Sprite completeWeeklyMissionRewardIcon;

    [Header("Claim Button")]
    [SerializeField] private Button claimButton;

    [Header("Claim Button Visuals")]
    [SerializeField] private Sprite activeButtonSprite;
    [SerializeField] private Sprite inactiveButtonSprite;
    [SerializeField] private TMP_Text claimButtonText;

    private bool showingDaily = true;


    private void Start()
    {
        if (MissionsManager.Instance != null)
        {
            MissionsManager.Instance.OnMissionsStateChanged += Refresh;
            Refresh();
        }
    }
    private void OnDestroy()
    {
        if (MissionsManager.Instance != null)
            MissionsManager.Instance.OnMissionsStateChanged -= Refresh;
    }


    private void OnEnable()
    {
        if (MissionsManager.Instance == null)
            return;

        MissionsManager.Instance.OnMissionsStateChanged += Refresh;
        Refresh();
    }


    private void OnDisable()
    {
        if (MissionsManager.Instance != null)
            MissionsManager.Instance.OnMissionsStateChanged -= Refresh;
    }

    public void ShowDaily()
    {
        showingDaily = true;
        Refresh();
    }

    public void ShowWeekly()
    {
        showingDaily = false;
        Refresh();
    }

    private void Refresh()
    {
        if (MissionsManager.Instance == null)
            return;

        int claimed;
        int total;
        Sprite rewardIcon;
        bool canClaim;
        bool alreadyClaimed;


        if (showingDaily)
        {
            claimed = MissionsManager.Instance.GetClaimedDailyCount();
            total = MissionsManager.Instance.GetTotalDailyCount();
            rewardIcon = completeDailyMissionRewardIcon;
            canClaim = MissionsManager.Instance.CanClaimDailySetReward();
            alreadyClaimed = MissionsManager.Instance.IsDailySetRewardClaimed();
        }
        else
        {
            claimed = MissionsManager.Instance.GetClaimedWeeklyCount();
            total = MissionsManager.Instance.GetTotalWeeklyCount();
            rewardIcon = completeWeeklyMissionRewardIcon;
            canClaim = MissionsManager.Instance.CanClaimWeeklySetReward();
            alreadyClaimed = MissionsManager.Instance.IsWeeklySetRewardClaimed();
        }
        progressText.text = $"{claimed} / {total}";
        progressFill.fillAmount = total > 0 ? (float)claimed / total : 0f;
        completeMissionRewardIcon.sprite = rewardIcon;

        // Button becomes clickable only when the set is completed and not claimed yet
        if (claimButton != null)
        {
            if (alreadyClaimed)
            {
                claimButton.interactable = false;
                claimButton.image.sprite = inactiveButtonSprite;
                claimButton.image.color = Color.white;
                if (claimButtonText != null)
                    claimButtonText.text = "Claimed";
            }
            else if (canClaim)
            {
                claimButton.interactable = true;
                claimButton.image.sprite = activeButtonSprite;
                claimButton.image.color = UnityEngine.ColorUtility.TryParseHtmlString("#40FF00", out var c) ? c : Color.white;
                if (claimButtonText != null)
                    claimButtonText.text = "Claim";
            }
            else
            {
                claimButton.interactable = false;
                claimButton.image.sprite = inactiveButtonSprite;
                claimButton.image.color = Color.white;
                if (claimButtonText != null)
                    claimButtonText.text = "Claim";
            }
        }
    }

    public void ClaimSetReward()
    {
        if (MissionsManager.Instance == null)
            return;

        var from = transform as RectTransform;

        if (showingDaily)
            MissionsManager.Instance.ClaimDailySetReward(from);
        else
            MissionsManager.Instance.ClaimWeeklySetReward(from);
    }
}
