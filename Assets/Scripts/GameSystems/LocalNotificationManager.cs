using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class LocalNotificationManager : MonoBehaviour
{
    private const string ChannelId = "game_reminders";

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
            Importance = Importance.Default
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ScheduleAll();
        else
            CancelAll();
    }

    private void OnApplicationQuit()
    {
        ScheduleAll();
    }

    private void ScheduleAll()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        DateTime now = DateTime.UtcNow;

        // Daily missions refresh
        DateTime nextDaily = DailyResetUtil.GetNextDailyResetUtc(now);
        SendNotification(
            "Daily Missions Ready!",
            "Your new daily missions are waiting. Come claim your rewards!",
            nextDaily);

        // Weekly missions refresh
        DateTime nextWeekly = DailyResetUtil.GetNextWeeklyResetUtc(now);
        SendNotification(
            "Weekly Missions Refreshed!",
            "New weekly missions with big rewards are here!",
            nextWeekly);

        // Inactivity reminder (24h)
        SendNotification(
            "We Miss You!",
            "Your units are waiting for battle. Come back and fight!",
            now.AddHours(24));
#endif
    }

    private void CancelAll()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
    }

    private void SendNotification(string title, string text, DateTime fireTimeUtc)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = fireTimeUtc.ToLocalTime()
        };
        AndroidNotificationCenter.SendNotification(notification, ChannelId);
#endif
    }
}
