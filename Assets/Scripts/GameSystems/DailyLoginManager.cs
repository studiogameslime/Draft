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

        if (GameData.Instance.Save.lastDailyLoginUtcTicks < currentReset.Ticks)
        {
            OnNewDailyLogin();
            GameData.Instance.Save.lastDailyLoginUtcTicks = currentReset.Ticks;
            GameData.Instance.SaveNow();
        }
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
