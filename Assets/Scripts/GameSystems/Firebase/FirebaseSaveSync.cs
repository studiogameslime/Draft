using System;
using System.Collections.Generic;
using System.Reflection;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseSaveSync : MonoBehaviour
{
    public static FirebaseSaveSync Instance;

    [Header("Options")]
    [SerializeField] private bool preferCloudIfNewer = true; // kept (optional)

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string uid;

    private bool ready;
    private bool pendingUpload;
    private bool pendingDownload;

    private const string TAG = "[CloudSave]";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // IMPORTANT: must be on a ROOT GameObject
        DontDestroyOnLoad(gameObject);

        Debug.Log(TAG + " Awake");
    }

    private void Start()
    {
        Debug.Log(TAG + " Start - waiting for FirebaseBootstrap...");
        StartCoroutine(WaitForBootstrapThenInit());
    }

    private System.Collections.IEnumerator WaitForBootstrapThenInit()
    {
        while (!FirebaseBootstrap.FirebaseReady)
            yield return null;

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        Debug.Log(TAG + " Bootstrap ready. Auth/Firestore created.");
        SignInAnonAndSync();
    }

    // ======================
    // AUTH + READY
    // ======================
    private void SignInAnonAndSync()
    {
        if (auth == null || db == null)
        {
            Debug.LogError(TAG + " SignInAnonAndSync aborted: auth/db is null");
            return;
        }

        if (auth.CurrentUser != null)
        {
            uid = auth.CurrentUser.UserId;
            ready = true;
            Debug.Log(TAG + " Already signed in. UID=" + uid);

            if (pendingDownload) DownloadCloudSaveOnce();
            if (pendingUpload) UploadNow();
            return;
        }

        Debug.Log(TAG + " Signing in anonymously...");
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError(TAG + " Anonymous sign-in failed: " + task.Exception);
                return;
            }

            var user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError(TAG + " Anonymous sign-in success but CurrentUser is null");
                return;
            }

            uid = user.UserId;
            ready = true;
            Debug.Log(TAG + " Anonymous sign-in success. UID=" + uid);

            if (pendingDownload) DownloadCloudSaveOnce();
            if (pendingUpload) UploadNow();
        });
    }

    private DocumentReference PlayerDoc()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return null;

        return db.Collection("players").Document(uid);
    }

    // ======================
    // DOWNLOAD (optional)
    // ======================
    private void DownloadCloudSaveOnce()
    {
        if (!ready || GameData.Instance == null || GameData.Instance.Save == null || db == null || string.IsNullOrEmpty(uid))
        {
            pendingDownload = true;
            Debug.Log(TAG + " Download delayed (not ready). ready=" + ready + " dbNull=" + (db == null) + " uidEmpty=" + string.IsNullOrEmpty(uid));
            return;
        }

        pendingDownload = false;

        var doc = PlayerDoc();
        if (doc == null)
        {
            Debug.LogError(TAG + " Download aborted: PlayerDoc is null");
            return;
        }

        Debug.Log(TAG + " Downloading from Firestore: players/" + uid);

        doc.GetSnapshotAsync().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                Debug.LogError(TAG + " GetSnapshot failed: " + t.Exception);
                return;
            }

            var snap = t.Result;
            if (snap == null || !snap.Exists)
            {
                Debug.Log(TAG + " No cloud document found (first time).");
                return;
            }

            string cloudJson = snap.ContainsField("saveJson") ? snap.GetValue<string>("saveJson") : null;
            long cloudUpdatedTicks = snap.ContainsField("updatedAtTicks") ? snap.GetValue<long>("updatedAtTicks") : 0;

            if (string.IsNullOrEmpty(cloudJson))
            {
                Debug.LogError(TAG + " Cloud doc exists but saveJson is empty");
                return;
            }

            long localUpdatedTicks = GetLocalUpdatedTicksSafe();
            bool useCloud = preferCloudIfNewer ? (cloudUpdatedTicks > localUpdatedTicks) : (localUpdatedTicks == 0);

            Debug.Log(TAG + " Cloud compare. cloudUpdatedTicks=" + cloudUpdatedTicks + " localUpdatedTicks=" + localUpdatedTicks + " useCloud=" + useCloud);

            if (!useCloud) return;

            var loaded = JsonUtility.FromJson<PlayerSaveData>(cloudJson);
            if (loaded == null)
            {
                Debug.LogError(TAG + " Failed to parse cloud saveJson");
                return;
            }

            GameData.Instance.Save = loaded;
            GameData.Instance.SaveNow();
            Debug.Log(TAG + " Loaded save from cloud and wrote to local");
        });
    }

    private long GetLocalUpdatedTicksSafe()
    {
        try
        {
            var field = typeof(PlayerSaveData).GetField("lastLocalSaveUtcTicks");
            if (field == null || GameData.Instance?.Save == null) return 0;
            return (long)field.GetValue(GameData.Instance.Save);
        }
        catch { return 0; }
    }

    // ======================
    // UPLOAD
    // ======================
    public void UploadNow()
    {
        if (GameData.Instance?.Save == null)
        {
            Debug.LogError(TAG + " Upload aborted: GameData.Save is null");
            return;
        }

        if (!ready || db == null || string.IsNullOrEmpty(uid))
        {
            pendingUpload = true;
            Debug.Log(TAG + " Upload delayed (not ready). ready=" + ready + " dbNull=" + (db == null) + " uidEmpty=" + string.IsNullOrEmpty(uid));
            return;
        }

        pendingUpload = false;

        var field = typeof(PlayerSaveData).GetField("lastLocalSaveUtcTicks");
        if (field != null)
            field.SetValue(GameData.Instance.Save, DateTime.UtcNow.Ticks);

        var saveMap = ToFirestoreMap(GameData.Instance.Save);

        var data = new Dictionary<string, object>(saveMap)
        {
            { "updatedAtTicks", DateTime.UtcNow.Ticks },
            { "updatedAt", FieldValue.ServerTimestamp },
            { "appVersion", Application.version },
            { "deviceModel", SystemInfo.deviceModel },
            { "saveJson", JsonUtility.ToJson(GameData.Instance.Save) }, // optional backup
        };

        Debug.Log(TAG + " Uploading to Firestore: players/" + uid + " keys=" + data.Count + " net=" + Application.internetReachability);

        var doc = PlayerDoc();
        if (doc == null)
        {
            Debug.LogError(TAG + " Upload aborted: PlayerDoc is null");
            return;
        }

        doc.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                Debug.LogError(TAG + " Upload failed: " + t.Exception);
                return;
            }
            Debug.Log(TAG + " Upload success");
        });
    }

    // ======================
    // SERIALIZATION
    // ======================
    private static Dictionary<string, object> ToFirestoreMap(object obj)
    {
        var map = new Dictionary<string, object>();
        if (obj == null) return map;

        var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var f in fields)
        {
            var value = f.GetValue(obj);
            if (value == null) continue;
            map[f.Name] = ConvertValue(value);
        }
        return map;
    }

    private static object ConvertValue(object value)
    {
        if (value is string || value is bool || value is int || value is long || value is float || value is double)
            return value;

        var t = value.GetType();
        if (t.IsEnum)
            return value.ToString();

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
                if (item != null) list.Add(ConvertValue(item));
            return list;
        }

        if (value is DateTime dt)
            return dt.ToUniversalTime().Ticks;

        return ToFirestoreMap(value);
    }
}
