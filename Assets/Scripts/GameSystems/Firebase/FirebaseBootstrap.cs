using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Messaging;
using UnityEngine;
using TMPro;
#if UNITY_ANDROID
using UnityEngine.Android;
using Firebase.Analytics;
#endif

public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseBootstrap Instance;

    [Header("UI Logs")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private int maxLines = 50;

    private FirebaseFirestore db;
    private bool initialized;

    private string DeviceId => SystemInfo.deviceUniqueIdentifier;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Log("App started");

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            Log("Requesting POST_NOTIFICATIONS permission");
        }
#endif

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    LogError("Firebase deps not available: " + task.Result);
                    return;
                }

                InitializeFirebase();
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                FirebaseAnalytics.LogEvent("open_the_game");
            });
    }

    private void InitializeFirebase()
    {
        if (initialized) return;
        initialized = true;

        db = FirebaseFirestore.DefaultInstance;

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;

        FirebaseMessaging.RequestPermissionAsync();
        Log("Firebase initialized (Firestore + Messaging)");

        FirebaseMessaging.GetTokenAsync()
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    LogError("GetTokenAsync failed");
                    return;
                }

                HandleToken(t.Result);

            });
    }

    private void OnDestroy()
    {
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        HandleToken(token.Token);
    }

    private void HandleToken(string fcmToken)
    {
        if (string.IsNullOrEmpty(fcmToken))
            return;

        Log("FCM Token received");
        SaveDeviceToFirestore(fcmToken);
    }

    private void SaveDeviceToFirestore(string fcmToken)
    {
        if (db == null)
        {
            LogError("Firestore not ready");
            return;
        }

        var doc = db.Collection("devices").Document(DeviceId);

        var data = new Dictionary<string, object>
        {
            { "deviceId", DeviceId },
            { "token", fcmToken },
            { "platform", Application.platform.ToString() },
            { "appVersion", Application.version },
            { "unityVersion", Application.unityVersion },
            { "deviceModel", SystemInfo.deviceModel },
            { "updatedAt", FieldValue.ServerTimestamp },
            { "createdAt", FieldValue.ServerTimestamp },
        };

        doc.SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted)
                    LogError("Failed to save device to Firestore");
                else
                    Log("Device saved to Firestore");
            });
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Log("FCM message received");

        var n = e.Message.Notification;
        if (n != null)
            Log($"Notification: {n.Title} | {n.Body}");
    }

    // ======================
    // UI LOGGER
    // ======================

    private void Log(string message)
    {
        AppendLine("Success " + message);
    }

    private void LogError(string message)
    {
        AppendLine("Error " + message);
    }

    private void AppendLine(string line)
    {
        if (logText == null)
            return;

        logText.text += $"[{DateTime.Now:HH:mm:ss}] {line}\n";

        var lines = logText.text.Split('\n');
        if (lines.Length > maxLines)
        {
            logText.text = string.Join("\n", lines, lines.Length - maxLines, maxLines);
        }
    }
}
