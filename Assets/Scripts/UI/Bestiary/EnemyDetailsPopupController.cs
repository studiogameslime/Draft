using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDetailsPopupController : MonoBehaviour
{
    [Header("Root & Close")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;
    [SerializeField] private PopupAnimator popupAnimator;

    [Header("Enemy Info")]
    [SerializeField] private Image icon;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject bossTag;

    [Header("Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text killCountText;

    [Header("Missions Section")]
    [SerializeField] private Transform missionsContent;
    [SerializeField] private EnemyMissionContainer missionRowPrefab;
    [SerializeField] private GameObject noMissionsText;

    private UnitDefinition _enemy;
    private RectTransform _lastFromRect;
    private readonly List<MissionInstance> _activeMissionInstances = new();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(Close);
    }

    public void Open(UnitDefinition enemy, RectTransform fromRect)
    {
        if (enemy == null) return;

        _enemy = enemy;
        _lastFromRect = fromRect;

        if (root != null)
            root.SetActive(true);

        FillStats();
        FillMissions();

        if (popupAnimator != null && fromRect != null)
            popupAnimator.OpenFromRect(fromRect, hideButton: false);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
            return;

        // Unsubscribe from mission events
        foreach (var m in _activeMissionInstances)
        {
            m.OnClaimed -= SaveEnemyMissions;
            m.OnProgressChanged -= SaveEnemyMissions;
        }
        _activeMissionInstances.Clear();

        if (popupAnimator != null && _lastFromRect != null && _lastFromRect.gameObject.activeInHierarchy)
            popupAnimator.Close();
        else if (root != null)
            root.SetActive(false);
    }

    private void FillStats()
    {
        if (_enemy == null) return;

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

        if (icon != null)
        {
            icon.sprite = _enemy.icon;
            icon.preserveAspect = true;
        }

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

    private void FillMissions()
    {
        // Cleanup previous
        foreach (var m in _activeMissionInstances)
        {
            m.OnClaimed -= SaveEnemyMissions;
            m.OnProgressChanged -= SaveEnemyMissions;
        }
        _activeMissionInstances.Clear();

        if (missionsContent == null) return;

        for (int i = missionsContent.childCount - 1; i >= 0; i--)
            Destroy(missionsContent.GetChild(i).gameObject);

        if (_enemy.enemyMissions == null || _enemy.enemyMissions.Count == 0 || missionRowPrefab == null)
        {
            if (noMissionsText != null) noMissionsText.SetActive(true);
            return;
        }

        if (noMissionsText != null) noMissionsText.SetActive(false);

        var save = GameData.Instance.Save;

        foreach (var missionDef in _enemy.enemyMissions)
        {
            if (missionDef == null) continue;

            var savedData = save.enemyMissionProgress
                .FirstOrDefault(d => d.missionId == missionDef.id);

            var instance = new MissionInstance(missionDef);
            if (savedData != null)
            {
                instance.currentProgress = savedData.currentProgress;
                instance.completed = savedData.completed;
                instance.claimed = savedData.claimed;
            }

            instance.OnClaimed += SaveEnemyMissions;
            instance.OnProgressChanged += SaveEnemyMissions;
            _activeMissionInstances.Add(instance);

            var row = Instantiate(missionRowPrefab, missionsContent);
            row.Setup(instance);
        }
    }

    private void SaveEnemyMissions()
    {
        var save = GameData.Instance.Save;

        foreach (var instance in _activeMissionInstances)
        {
            var existing = save.enemyMissionProgress
                .FirstOrDefault(d => d.missionId == instance.definition.id);

            if (existing != null)
            {
                existing.currentProgress = instance.currentProgress;
                existing.completed = instance.completed;
                existing.claimed = instance.claimed;
            }
            else
            {
                save.enemyMissionProgress.Add(new MissionSaveData
                {
                    missionId = instance.definition.id,
                    currentProgress = instance.currentProgress,
                    completed = instance.completed,
                    claimed = instance.claimed
                });
            }
        }

        GameData.Instance.SaveNow();
    }
}
