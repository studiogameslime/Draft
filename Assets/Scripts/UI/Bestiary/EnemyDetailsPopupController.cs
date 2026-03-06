using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDetailsPopupController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Animator")]
    [SerializeField] private PopupAnimator popupAnimator;

    [Header("Enemy UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private GameObject bossTag;

    [Header("Challenges")]
    [SerializeField] private EnemyChallengeSet[] allChallengeSets;
    [SerializeField] private EnemyChallengeRowView challengeRowPrefab;
    [SerializeField] private Transform challengesContent;

    private UnitDefinition _enemy;
    private RectTransform _lastFromRect;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(Close);

        if (root != null)
            root.SetActive(false);
    }

    public void Open(UnitDefinition enemy, RectTransform fromRect)
    {
        if (enemy == null) return;

        _enemy = enemy;
        _lastFromRect = fromRect;

        if (root != null)
            root.SetActive(true);

        FillData();
        FillChallenges();

        if (popupAnimator != null && fromRect != null)
            popupAnimator.OpenFromRect(fromRect, hideButton: false);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
            return;

        if (popupAnimator != null && _lastFromRect != null && _lastFromRect.gameObject.activeInHierarchy)
            popupAnimator.Close();
        else if (root != null)
            root.SetActive(false);
    }

    private void FillData()
    {
        if (_enemy == null) return;

        FillIconImageOrAnimator();

        if (nameText != null) nameText.text = _enemy.displayName;
        if (descriptionText != null) descriptionText.text = _enemy.description;
        if (hpText != null) hpText.text = _enemy.maxHealth.ToString();
        if (dmgText != null) dmgText.text = _enemy.damage.ToString();
        if (atkSpeedText != null) atkSpeedText.text = _enemy.attackCooldown.ToString("F1");

        if (rangeText != null)
        {
            var label = UILabels.GetRangeLabel(_enemy.attackRange);
            rangeText.text = UILabels.RangeToDisplay(label);
        }

        if (speedText != null)
        {
            var cat = UILabels.GetSpeedLabel(_enemy.moveSpeed);
            speedText.text = UILabels.SpeedToDisplay(cat);
        }

        if (bossTag != null) bossTag.SetActive(_enemy.isBoss);

        if (killCountText != null)
        {
            var stat = GameData.Instance.Save.enemyKillStats
                .FirstOrDefault(s => s.enemyId == _enemy.id);
            killCountText.text = (stat?.kills ?? 0).ToString();
        }
    }

    private void FillIconImageOrAnimator()
    {
        if (icon == null || _enemy == null) return;

        icon.sprite = _enemy.HeadWithEyes != null ? _enemy.HeadWithEyes : _enemy.icon;

        if (iconAnimator != null)
        {
            if (_enemy.animatorController != null)
            {
                iconAnimator.enabled = true;
                iconAnimator.runtimeAnimatorController = _enemy.animatorController;
            }
            else
            {
                iconAnimator.enabled = false;
                iconAnimator.runtimeAnimatorController = null;
            }
        }
    }

    private void FillChallenges()
    {
        // Clear old rows
        for (int i = challengesContent.childCount - 1; i >= 0; i--)
            Destroy(challengesContent.GetChild(i).gameObject);

        if (allChallengeSets == null || challengeRowPrefab == null)
            return;

        var set = allChallengeSets.FirstOrDefault(s => s != null && s.enemy != null && s.enemy.id == _enemy.id);
        if (set == null || set.challenges == null)
            return;

        var save = GameData.Instance.Save;

        foreach (var challenge in set.challenges)
        {
            if (challenge == null) continue;

            int progress = GetChallengeProgress(save, challenge.challengeId);
            bool claimed = IsChallengeComplete(save, challenge.challengeId);

            var row = Instantiate(challengeRowPrefab, challengesContent);
            row.Setup(challenge, progress, claimed);
        }
    }

    private int GetChallengeProgress(PlayerSaveData save, string challengeId)
    {
        var data = save.enemyChallengeProgress.FirstOrDefault(c => c.challengeId == challengeId);
        return data?.progress ?? 0;
    }

    private bool IsChallengeComplete(PlayerSaveData save, string challengeId)
    {
        var data = save.enemyChallengeProgress.FirstOrDefault(c => c.challengeId == challengeId);
        return data?.claimed ?? false;
    }
}
