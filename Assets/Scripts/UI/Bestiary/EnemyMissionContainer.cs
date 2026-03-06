using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyMissionContainer : MonoBehaviour
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
            int finalGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(mission.definition.goldReward)
                : mission.definition.goldReward;

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

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
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
            SetButtonActive();
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
        if (mission == null || !mission.completed || mission.claimed)
            return;

        if (mission.definition.chestReward != null && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(mission.definition.chestReward, () =>
            {
                GrantRewards();
            });
        }
        else
        {
            GrantRewards();
        }
    }

    private void GrantRewards()
    {
        if (mission.definition.goldReward > 0)
        {
            int boostedGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(mission.definition.goldReward)
                : mission.definition.goldReward;
            PlayerCurrencyWallet.Instance.AddGold(boostedGold, rewardIcon.rectTransform);
        }

        if (mission.definition.gemsReward > 0)
            PlayerCurrencyWallet.Instance.AddGems(mission.definition.gemsReward, rewardIcon.rectTransform);

        if (mission.definition.scrollsReward > 0)
            PlayerCurrencyWallet.Instance.AddScrolls(mission.definition.scrollsReward, rewardIcon.rectTransform);

        mission.claimed = true;
        mission.OnClaimed?.Invoke();
    }

    private void SetButtonInactive()
    {
        claimButton.image.sprite = inactiveButtonSprite;
    }

    private void SetButtonActive()
    {
        claimButton.image.sprite = activeButtonSprite;
    }
}
