using System;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class LocalNotificationManager : MonoBehaviour
{
    private const string ChannelId = "game_reminders";
    private const string LogFileName = "notification_log.txt";

    private static LocalNotificationManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null) return;

        var go = new GameObject("LocalNotificationManager");
        instance = go.AddComponent<LocalNotificationManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "Game Reminders",
            Description = "Daily missions, weekly missions, and comeback reminders",
            Importance = Importance.High
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Log("Channel registered.");
#endif
    }

    private void Start()
    {
        // Schedule immediately on launch so notifications survive swipe-kill.
        ScheduleAll();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ScheduleAll();
        else
            CancelAll();
    }

    private void ScheduleAll()
    {
#if UNITY_ANDROID
        try
        {
            var permStatus = AndroidNotificationCenter.UserPermissionToPost;
            Log($"Permission status: {permStatus}");

            AndroidNotificationCenter.CancelAllScheduledNotifications();

            DateTime now = DateTime.UtcNow;
            Log($"Scheduling at UTC: {now}");

            DateTime nextDaily = DailyResetUtil.GetNextDailyResetUtc(now);
            ScheduleOne("Daily Missions Ready!",
                "Your new daily missions are waiting. Come claim your rewards!",
                nextDaily);

            DateTime nextWeekly = DailyResetUtil.GetNextWeeklyResetUtc(now);
            ScheduleOne("Weekly Missions Refreshed!",
                "New weekly missions with big rewards are here!",
                nextWeekly);

            ScheduleOne("We Miss You!",
                "Your units are waiting for battle. Come back and fight!",
                now.AddHours(24));

            Log("All notifications scheduled successfully.");
        }
        catch (Exception e)
        {
            Log($"ERROR in ScheduleAll: {e}");
        }
#endif
    }

    private void CancelAll()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
    }

#if UNITY_ANDROID
    private void ScheduleOne(string title, string text, DateTime fireTimeUtc)
    {
        var localTime = fireTimeUtc.ToLocalTime();
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = localTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large",
            ShouldAutoCancel = true
        };

        int id = AndroidNotificationCenter.SendNotification(notification, ChannelId);
        var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
        Log($"Scheduled '{title}' -> id={id}, status={status}, fireAt={localTime}");
    }
#endif

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        Debug.Log($"[Notifications] {msg}");

        try
        {
            string path = Path.Combine(Application.persistentDataPath, LogFileName);
            File.AppendAllText(path, line + "\n");
        }
        catch { }
    }
}
