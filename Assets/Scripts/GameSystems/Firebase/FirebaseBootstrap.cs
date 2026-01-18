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

    // Global readiness flag for other scripts (FirebaseSaveSync will wait on this)
    public static bool FirebaseReady { get; private set; }

    [Header("UI Logs")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private int maxLines = 50;

    private FirebaseFirestore db;
    private bool initialized;

    private const string AllPlayersTopic = "all_players";
    private string DeviceId => SystemInfo.deviceUniqueIdentifier;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // IMPORTANT: must be on a ROOT GameObject
        DontDestroyOnLoad(gameObject);

        FirebaseReady = false;
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

        // Only this script calls dependencies check
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    LogError("CheckAndFixDependenciesAsync failed: " + task.Exception);
                    return;
                }

                if (task.Result != DependencyStatus.Available)
                {
                    LogError("Firebase deps not available: " + task.Result);
                    return;
                }

                InitializeFirebase();
                FirebaseReady = true;
                Log("FirebaseReady = true");

#if UNITY_ANDROID
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                FirebaseAnalytics.LogEvent("open_the_game");
#endif
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
                    LogError("GetTokenAsync failed: " + t.Exception);
                    return;
                }
                HandleToken(t.Result);
            });
    }

    private void OnDestroy()
    {
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;

        if (Instance == this)
            Instance = null;
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
        SubscribeToAllPlayersTopicOnce(fcmToken);
    }

    private void SubscribeToAllPlayersTopicOnce(string fcmToken)
    {
        string key = "fcm_topic_subscribed_token_" + AllPlayersTopic;
        string lastToken = PlayerPrefs.GetString(key, "");
        if (lastToken == fcmToken)
            return;

        FirebaseMessaging.SubscribeAsync(AllPlayersTopic)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    LogError("SubscribeAsync(" + AllPlayersTopic + ") failed: " + t.Exception);
                    return;
                }

                PlayerPrefs.SetString(key, fcmToken);
                PlayerPrefs.Save();
                Log("Subscribed to topic: " + AllPlayersTopic);
            });
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
                if (t.IsFaulted || t.IsCanceled)
                    LogError("Failed to save device to Firestore: " + t.Exception);
                else
                    Log("Device saved to Firestore");
            });
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Log("FCM message received");
        var n = e.Message.Notification;
        if (n != null)
            Log("Notification: " + n.Title + " | " + n.Body);
    }

    // ======================
    // UI LOGGER
    // ======================
    private void Log(string message) => AppendLine("Success " + message);
    private void LogError(string message) => AppendLine("Error " + message);

    private void AppendLine(string line)
    {
        if (logText == null)
            return;

        logText.text += "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line + "\n";

        var lines = logText.text.Split('\n');
        if (lines.Length > maxLines)
            logText.text = string.Join("\n", lines, lines.Length - maxLines, maxLines);
    }
}
