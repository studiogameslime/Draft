using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyChallengeRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject completedCheckmark;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button claimButton;

    private EnemyChallengeDefinition _challenge;

    public void Setup(EnemyChallengeDefinition challenge, int progress, bool claimed)
    {
        _challenge = challenge;

        int clamped = Mathf.Min(progress, challenge.targetCount);

        if (descriptionText != null)
            descriptionText.text = challenge.GetDescription();

        if (progressText != null)
            progressText.text = $"{clamped}/{challenge.targetCount}";

        if (progressBar != null)
        {
            progressBar.maxValue = challenge.targetCount;
            progressBar.value = clamped;
        }

        bool isComplete = clamped >= challenge.targetCount;

        if (completedCheckmark != null)
            completedCheckmark.SetActive(claimed);

        if (rewardText != null)
        {
            if (challenge.goldReward > 0)
                rewardText.text = $"{challenge.goldReward}";
            else if (challenge.gemsReward > 0)
                rewardText.text = $"{challenge.gemsReward}";
            else
                rewardText.text = $"{challenge.xpReward} XP";
        }

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(isComplete && !claimed);
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }
    }

    private void OnClaimClicked()
    {
        if (_challenge == null) return;

        EnemyChallengeTracker.ClaimReward(_challenge);

        if (completedCheckmark != null)
            completedCheckmark.SetActive(true);

        if (claimButton != null)
            claimButton.gameObject.SetActive(false);
    }
}
