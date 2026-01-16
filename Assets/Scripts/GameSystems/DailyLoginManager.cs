using System;
using UnityEngine;

public class DailyLoginManager : MonoBehaviour
{
    public static DailyLoginManager instance;


    private void Awake()
    {
        instance = this;
    }

    public void CheckDailyLogin()
    {
        if (GameData.Instance?.Save == null)
            return;

        DateTime nowUtc = DateTime.UtcNow;
        DateTime currentReset = DailyResetUtil.GetCurrentDailyResetUtc(nowUtc);

        long lastTicks = GameData.Instance.Save.lastDailyLoginUtcTicks;
        if (lastTicks < currentReset.Ticks)
        {
            TriggerLogin(currentReset);
        }

    }


    private void TriggerLogin(DateTime resetTime)
    {
        Debug.Log($"[DailyLogin] New daily login for reset {resetTime}");
        OnNewDailyLogin();
        GameData.Instance.Save.lastDailyLoginUtcTicks = resetTime.Ticks;
        GameData.Instance.SaveNow();
    }


    private void OnNewDailyLogin()
    {
        MissionsManager.Instance?.ReportAction(MissionAction.Login, 1);
        Debug.Log("Daily login granted");
    }
}
