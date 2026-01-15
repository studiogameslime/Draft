using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseSaveSync : MonoBehaviour
{
    public static FirebaseSaveSync Instance;

    [Header("Options")]
    [Tooltip("If true, cloud will overwrite local when cloud is newer. Otherwise local wins.")]
    [SerializeField] private bool preferCloudIfNewer = true;

    FirebaseAuth auth;
    FirebaseFirestore db;
    string uid;
    bool ready;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(t =>
        {
            if (t.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase deps not available: " + t.Result);
                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            SignInAnonAndSync();
        });
    }

    void SignInAnonAndSync()
    {
        if (auth.CurrentUser != null)
        {
            uid = auth.CurrentUser.UserId;
            ready = true;
            DownloadCloudSaveOnce();
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Anon sign-in failed: " + task.Exception);
                return;
            }

            var user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("Anon sign-in succeeded but CurrentUser is null");
                return;
            }

            uid = user.UserId;
            ready = true;
            DownloadCloudSaveOnce();
        });
    }

    DocumentReference PlayerDoc()
        => db.Collection("players").Document(uid);

    void DownloadCloudSaveOnce()
    {
        if (!ready || GameData.Instance == null) return;

        PlayerDoc().GetSnapshotAsync().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                Debug.LogError("GetSnapshot failed: " + t.Exception);
                return;
            }

            var snap = t.Result;
            if (!snap.Exists) return;

            // cloud fields
            string cloudJson = snap.ContainsField("saveJson") ? snap.GetValue<string>("saveJson") : null;
            long cloudUpdatedTicks = snap.ContainsField("updatedAtTicks") ? snap.GetValue<long>("updatedAtTicks") : 0;

            if (string.IsNullOrEmpty(cloudJson)) return;

            // local updated ticks (נוסיף אצלך לשמירה)
            long localUpdatedTicks = GameData.Instance.Save != null ? GetLocalUpdatedTicksSafe() : 0;

            bool useCloud = preferCloudIfNewer
                ? cloudUpdatedTicks > localUpdatedTicks
                : localUpdatedTicks == 0; // אם אין לוקאלי בכלל

            if (useCloud)
            {
                var loaded = JsonUtility.FromJson<PlayerSaveData>(cloudJson);
                if (loaded != null)
                {
                    GameData.Instance.Save = loaded;
                    GameData.Instance.SaveNow(); // כדי לסנכרן גם לקובץ המקומי
                    Debug.Log("Loaded save from cloud.");
                }
            }
        });
    }

    long GetLocalUpdatedTicksSafe()
    {
        // מומלץ להוסיף לשמירה שלך שדה כזה:
        // public long lastLocalSaveUtcTicks;
        // ואז כאן להחזיר אותו.
        // אם אין לך עדיין: תחזיר 0 ותוסיף בהמשך.
        var field = typeof(PlayerSaveData).GetField("lastLocalSaveUtcTicks");
        if (field == null) return 0;
        return (long)field.GetValue(GameData.Instance.Save);
    }

    public void UploadNow()
    {
        if (!ready || GameData.Instance?.Save == null) return;

        // עדכון "מתי נשמר מקומית" (מומלץ להוסיף לשדה בסייב)
        var field = typeof(PlayerSaveData).GetField("lastLocalSaveUtcTicks");
        if (field != null)
            field.SetValue(GameData.Instance.Save, DateTime.UtcNow.Ticks);

        string json = JsonUtility.ToJson(GameData.Instance.Save, true);

        var data = new Dictionary<string, object>
        {
            { "saveJson", json },
            { "updatedAtTicks", DateTime.UtcNow.Ticks },
            { "updatedAt", FieldValue.ServerTimestamp },
            { "appVersion", Application.version },
            { "deviceModel", SystemInfo.deviceModel },
        };

        PlayerDoc().SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("Upload save failed: " + t.Exception);
            else Debug.Log("Save uploaded to cloud.");
        });
    }
}
