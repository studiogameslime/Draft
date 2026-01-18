using System;
using System.Collections;
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
    [Tooltip("If true, cloud will overwrite local when cloud is newer. Otherwise local wins.")]
    [SerializeField] private bool preferCloudIfNewer = true; // kept (optional)

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string uid;
    private bool ready;
    private bool pendingUpload;
    private bool pendingDownload;

    // --- Compatibility flags for GameData.cs ---
    public bool CloudLoadDone { get; private set; }
    public bool CloudDocExists { get; private set; }
    public PlayerSaveData CloudSaveData { get; private set; }
    // -------------------------------------------

    private const string TAG = "[CloudSave]";

    private static readonly HashSet<string> MetaKeys = new HashSet<string>
    {
        "updatedAtTicks",
        "updatedAt",
        "appVersion",
        "deviceModel"
        // If you ever add more meta fields, add them here.
    };

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log(TAG + " Awake");
    }

    private void Start()
    {
        Debug.Log(TAG + " Start - waiting for FirebaseBootstrap...");
        StartCoroutine(WaitForBootstrapThenInit());
    }

    private IEnumerator WaitForBootstrapThenInit()
    {
        while (!FirebaseBootstrap.FirebaseReady)
            yield return null;

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log(TAG + " Bootstrap ready. Auth/Firestore created.");
        SignInAnonAndSync();
    }

    // ======================
    // Compatibility API (called from GameData.cs)
    // ======================
    public void BeginDownload()
    {
        Debug.Log(TAG + " BeginDownload() called");
        CloudLoadDone = false;
        CloudDocExists = false;
        CloudSaveData = null;
        DownloadCloudSaveOnce();
    }

    public void CloudSave()
    {
        Debug.Log(TAG + " CloudSave() called");
        UploadNow();
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
    // DOWNLOAD (reads spread fields, NOT saveJson)
    // ======================
    private void DownloadCloudSaveOnce()
    {
        if (!ready || db == null || string.IsNullOrEmpty(uid))
        {
            pendingDownload = true;
            Debug.Log(TAG + " Download delayed (not ready). ready=" + ready +
                      " dbNull=" + (db == null) +
                      " uidEmpty=" + string.IsNullOrEmpty(uid));
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
            CloudLoadDone = true;

            if (t.IsFaulted || t.IsCanceled)
            {
                Debug.LogError(TAG + " GetSnapshot failed: " + t.Exception);
                return;
            }

            var snap = t.Result;
            if (snap == null || !snap.Exists)
            {
                CloudDocExists = false;
                Debug.Log(TAG + " No cloud document found (first time).");
                return;
            }

            CloudDocExists = true;

            // Read spread fields from the document
            Dictionary<string, object> dict = snap.ToDictionary();
            if (dict == null || dict.Count == 0)
            {
                Debug.LogError(TAG + " Cloud doc is empty");
                return;
            }

            // Optional: keep your ticks logic
            long cloudUpdatedTicks = 0;
            if (dict.TryGetValue("updatedAtTicks", out var ticksObj))
                cloudUpdatedTicks = SafeToLong(ticksObj);

            long localUpdatedTicks = GetLocalUpdatedTicksSafe();
            bool useCloud = preferCloudIfNewer ? (cloudUpdatedTicks > localUpdatedTicks) : (localUpdatedTicks == 0);

            Debug.Log(TAG + " Cloud compare. cloudUpdatedTicks=" + cloudUpdatedTicks +
                      " localUpdatedTicks=" + localUpdatedTicks +
                      " useCloud=" + useCloud);

            if (!useCloud)
                return;

            // Remove meta keys before mapping into PlayerSaveData
            foreach (var k in MetaKeys)
                dict.Remove(k);

            var loaded = FromFirestoreMap<PlayerSaveData>(dict);
            if (loaded == null)
            {
                Debug.LogError(TAG + " Failed to build PlayerSaveData from spread fields");
                return;
            }

            CloudSaveData = loaded;
            Debug.Log(TAG + " Download success (CloudSaveData set from spread fields)");
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
    // UPLOAD (writes spread fields, NOT saveJson)
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
            Debug.Log(TAG + " Upload delayed (not ready). ready=" + ready +
                      " dbNull=" + (db == null) +
                      " uidEmpty=" + string.IsNullOrEmpty(uid));
            return;
        }

        pendingUpload = false;

        // Optional: local timestamp inside save
        var field = typeof(PlayerSaveData).GetField("lastLocalSaveUtcTicks");
        if (field != null)
            field.SetValue(GameData.Instance.Save, DateTime.UtcNow.Ticks);

        // Spread fields at top-level
        var saveMap = ToFirestoreMap(GameData.Instance.Save);

        // Add meta fields
        saveMap["updatedAtTicks"] = DateTime.UtcNow.Ticks;
        saveMap["updatedAt"] = FieldValue.ServerTimestamp;
        saveMap["appVersion"] = Application.version;
        saveMap["deviceModel"] = SystemInfo.deviceModel;

        Debug.Log(TAG + " Uploading spread fields to Firestore: players/" + uid +
                  " keys=" + saveMap.Count +
                  " net=" + Application.internetReachability);

        var doc = PlayerDoc();
        if (doc == null)
        {
            Debug.LogError(TAG + " Upload aborted: PlayerDoc is null");
            return;
        }

        doc.SetAsync(saveMap, SetOptions.MergeAll).ContinueWithOnMainThread(t =>
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
    // SERIALIZATION (Unity -> Firestore map)  [unchanged]
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
            map[f.Name] = ConvertToFirestoreValue(value);
        }
        return map;
    }

    private static object ConvertToFirestoreValue(object value)
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
                if (item != null) list.Add(ConvertToFirestoreValue(item));
            return list;
        }

        if (value is DateTime dt)
            return dt.ToUniversalTime().Ticks;

        return ToFirestoreMap(value);
    }

    // ======================
    // DESERIALIZATION (Firestore map -> Unity object)  [NEW]
    // ======================
    private static T FromFirestoreMap<T>(Dictionary<string, object> map) where T : new()
    {
        if (map == null) return default;

        var obj = new T();
        var type = typeof(T);

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var f in fields)
        {
            if (!map.TryGetValue(f.Name, out var raw) || raw == null)
                continue;

            object converted = ConvertFromFirestoreValue(raw, f.FieldType);
            if (converted != null)
                f.SetValue(obj, converted);
        }

        return obj;
    }

    private static object ConvertFromFirestoreValue(object raw, Type targetType)
    {
        // Handle null
        if (raw == null) return null;

        // Direct assign
        if (targetType.IsAssignableFrom(raw.GetType()))
            return raw;

        // Enums stored as string
        if (targetType.IsEnum)
        {
            if (raw is string s && Enum.TryParse(targetType, s, out var enumVal))
                return enumVal;
            return null;
        }

        // Numbers coming from Firestore might be long/double
        if (targetType == typeof(int)) return (int)SafeToLong(raw);
        if (targetType == typeof(long)) return SafeToLong(raw);
        if (targetType == typeof(float)) return SafeToFloat(raw);
        if (targetType == typeof(double)) return SafeToDouble(raw);
        if (targetType == typeof(bool)) return SafeToBool(raw);
        if (targetType == typeof(string)) return raw.ToString();

        // List<T>
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type elemType = targetType.GetGenericArguments()[0];
            if (raw is List<object> rawList)
            {
                var list = (System.Collections.IList)Activator.CreateInstance(targetType);
                foreach (var item in rawList)
                {
                    var elem = ConvertFromFirestoreValue(item, elemType);
                    list.Add(elem);
                }
                return list;
            }
            return null;
        }

        // Nested object as map
        if (raw is Dictionary<string, object> childMap)
        {
            object child = Activator.CreateInstance(targetType);
            var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fields)
            {
                if (!childMap.TryGetValue(f.Name, out var v) || v == null)
                    continue;

                var converted = ConvertFromFirestoreValue(v, f.FieldType);
                if (converted != null)
                    f.SetValue(child, converted);
            }
            return child;
        }

        return null;
    }

    private static long SafeToLong(object v)
    {
        try
        {
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is double d) return (long)d;
            if (v is float f) return (long)f;
            if (v is string s && long.TryParse(s, out var parsed)) return parsed;
        }
        catch { }
        return 0;
    }

    private static float SafeToFloat(object v)
    {
        try
        {
            if (v is float f) return f;
            if (v is double d) return (float)d;
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is string s && float.TryParse(s, out var parsed)) return parsed;
        }
        catch { }
        return 0f;
    }

    private static double SafeToDouble(object v)
    {
        try
        {
            if (v is double d) return d;
            if (v is float f) return f;
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is string s && double.TryParse(s, out var parsed)) return parsed;
        }
        catch { }
        return 0d;
    }

    private static bool SafeToBool(object v)
    {
        try
        {
            if (v is bool b) return b;
            if (v is string s && bool.TryParse(s, out var parsed)) return parsed;
        }
        catch { }
        return false;
    }
}
