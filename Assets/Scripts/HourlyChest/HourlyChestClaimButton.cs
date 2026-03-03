using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HourlyChestClaimButton : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int cooldownHours = 3;

    [Header("UI")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text buttonText;

    [Header("Reward")]
    [SerializeField] private ChestDefinition chestReward;

    private float _nextUiTick;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClickClaim);
    }

    private void OnEnable()
    {
        RefreshUI(true);
    }

    private void Update()
    {
        // Update once per second (cheap) so the timer counts down smoothly
        if (Time.unscaledTime >= _nextUiTick)
        {
            _nextUiTick = Time.unscaledTime + 1f;
            RefreshUI(false);
        }
    }

    private void RefreshUI(bool force)
    {
        var save = GameData.Instance?.Save;
        if (save == null || claimButton == null || buttonText == null)
            return;

        bool ready = IsReady(save.nextHourlyChestUtcTicks);

        claimButton.interactable = ready;

        if (ready)
        {
            buttonText.text = "CLAIM";
            return;
        }

        TimeSpan left = GetTimeLeft(save.nextHourlyChestUtcTicks);
        buttonText.text = $"{left.Hours:D2}:{left.Minutes:D2}:{left.Seconds:D2}";
    }

    private void OnClickClaim()
    {
        var save = GameData.Instance?.Save;
        if (save == null)
            return;

        if (!IsReady(save.nextHourlyChestUtcTicks))
            return;

        // Give reward (choose what your project uses)
        if (chestReward != null && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(chestReward);
        }
        else
        {
            Debug.LogWarning("HourlyChest: chestReward or StoreItemChestPanel missing");
        }

        // Set next available time: base cooldown minus mastery reduction (min 120 min)
        float reductionMinutes = MasteryBonusManager.Instance != null
            ? MasteryBonusManager.Instance.GetFlat(MasteryStat.HourlyChestCooldownReduction)
            : 0f;
        float totalMinutes = Mathf.Max(cooldownHours * 60f - reductionMinutes, 120f);
        DateTime nextUtc = DateTime.UtcNow.AddMinutes(totalMinutes);
        save.nextHourlyChestUtcTicks = nextUtc.Ticks;
        GameData.Instance.SaveNow();

        RefreshUI(true);
    }

    private bool IsReady(long nextUtcTicks)
    {
        if (nextUtcTicks <= 0) return true; // never claimed yet
        DateTime nextUtc = new DateTime(nextUtcTicks, DateTimeKind.Utc);
        return DateTime.UtcNow >= nextUtc;
    }

    private TimeSpan GetTimeLeft(long nextUtcTicks)
    {
        if (nextUtcTicks <= 0) return TimeSpan.Zero;
        DateTime nextUtc = new DateTime(nextUtcTicks, DateTimeKind.Utc);
        TimeSpan t = nextUtc - DateTime.UtcNow;
        return (t.TotalSeconds < 0) ? TimeSpan.Zero : t;
    }
}
