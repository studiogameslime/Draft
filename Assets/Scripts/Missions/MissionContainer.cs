using System.Security.Claims;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class MissionContainer : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimButtonText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Image completedMissionMark;
    [SerializeField] private Sprite activeButtonSprite;
    [SerializeField] private Sprite inactiveButtonSprite;

    private MissionInstance mission;

    public void Setup(MissionInstance mission)
    {
        this.mission = mission;

        descriptionText.text = mission.definition.description;

        if (mission.definition.goldReward > 0)
        {
            // Calculate the full amount including mastery bonus
            int finalGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(mission.definition.goldReward)
                : mission.definition.goldReward;

            // Display only the final boosted total
            rewardText.text = finalGold.ToString();
            rewardIcon.sprite = StyleManager.instance.goldSprite;
        }

        if (mission.definition.gemsReward > 0)
        {
            rewardText.text = mission.definition.gemsReward.ToString();
            rewardIcon.sprite = StyleManager.instance.gemSprite;
        }
        if (mission.definition.scrollsReward > 0)
        {
            rewardText.text = mission.definition.scrollsReward.ToString();
            rewardIcon.sprite = StyleManager.instance.scrollSprite;
        }
        if (mission.definition.chestReward != null)
        {
            RectTransform rt = rewardIcon.GetComponent<RectTransform>();

            // Center anchors
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            // Center pivot (important so size is symmetric)
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Position: centered, Y = 0
            rt.anchoredPosition = new Vector2(0f, 0f);

            // Size: 80x80
            rt.sizeDelta = new Vector2(80f, 80f);

            rewardText.gameObject.SetActive(false);
            rewardIcon.sprite = mission.definition.chestReward.buttonIcon;
        }

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaimPressed);

        mission.OnProgressChanged += Refresh;
        mission.OnClaimed += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (mission != null)
        {
            mission.OnProgressChanged -= Refresh;
            mission.OnClaimed -= Refresh;
        }
    }

    void Refresh()
    {
        progressText.text =
            $"{mission.currentProgress}/{mission.definition.targetAmount}";

        progressFill.fillAmount =
            (float)mission.currentProgress / mission.definition.targetAmount;

        if (mission.claimed)
        {
            claimButton.interactable = false;
            SetButtonInactive();
            if (claimButtonText != null)
                claimButtonText.text = "Claimed";
            claimButton.image.color = Color.white;

        }
        else if (mission.completed)
        {
            claimButton.interactable = true;
            SetButtonactive();
            if (claimButtonText != null)
                claimButtonText.text = "Claim";
            claimButton.image.color = UnityEngine.ColorUtility.TryParseHtmlString("#40FF00", out var c) ? c : Color.white;
        }
        else
        {
            claimButton.interactable = false;
            SetButtonInactive();
            if (claimButtonText != null)
                claimButtonText.text = "Claim";
            claimButton.image.color = Color.white;
        }
    }


    void OnClaimPressed()
    {
        MissionsManager.Instance.ClaimMission(mission, rewardIcon.rectTransform);

    }

    private void SetButtonInactive()
    {
        claimButton.image.sprite = inactiveButtonSprite;
    }
    private void SetButtonactive()
    {
        claimButton.image.sprite = activeButtonSprite;

    }
}
