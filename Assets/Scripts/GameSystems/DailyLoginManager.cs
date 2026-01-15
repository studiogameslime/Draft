using System;
using UnityEngine;

public class DailyLoginManager : MonoBehaviour
{
    public static DailyLoginManager instance;

    [Header("Daily Reset Hour (UTC)")]
    [SerializeField] private int resetHourUtc = 8;

    private void Awake()
    {
        instance = this;
    }

    public void CheckDailyLogin()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
            return;

        DateTime now = DateTime.UtcNow;
        DateTime currentResetTime = GetCurrentResetTime(now);

        long lastClaimedResetTicks = GameData.Instance.Save.lastDailyLoginUtcTicks;

        // First time ever OR reset window passed
        if (lastClaimedResetTicks < currentResetTime.Ticks)
        {
            TriggerLogin(currentResetTime);
        }
    }

    private DateTime GetCurrentResetTime(DateTime now)
    {
        DateTime todayReset =
            new DateTime(now.Year, now.Month, now.Day, resetHourUtc, 0, 0, DateTimeKind.Utc);

        // If we haven't reached today's reset yet – use yesterday's reset
        if (now < todayReset)
            todayReset = todayReset.AddDays(-1);

        return todayReset;
    }

    private void TriggerLogin(DateTime resetTime)
    {
        OnNewDailyLogin();
        GameData.Instance.Save.lastDailyLoginUtcTicks = resetTime.Ticks;
        GameData.Instance.SaveNow();
    }

    private void OnNewDailyLogin()
    {
        MissionsManager.Instance.ReportAction(MissionAction.Login, 1);
        Debug.Log("Daily login granted");
    }
}
