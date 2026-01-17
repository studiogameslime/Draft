using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System.Reflection;


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

        var saveMap = ToFirestoreMap(GameData.Instance.Save);

        // "spread": כל השדות של הסייב יושבים בטופ-לבל של הדוקומנט
        var data = new Dictionary<string, object>(saveMap)
{
    { "updatedAtTicks", DateTime.UtcNow.Ticks },
    { "updatedAt", FieldValue.ServerTimestamp },
    { "appVersion", Application.version },
    { "deviceModel", SystemInfo.deviceModel },
};
        data["saveJson"] = JsonUtility.ToJson(GameData.Instance.Save);
        // אופציונלי: אם אתה עדיין רוצה “גיבוי” אחד של JSON (לא חובה)
        // data["saveJson"] = JsonUtility.ToJson(GameData.Instance.Save);

        PlayerDoc().SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("Upload save failed: " + t.Exception);
            else Debug.Log("Save uploaded to cloud.");
        });


        PlayerDoc().SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("Upload save failed: " + t.Exception);
            else Debug.Log("Save uploaded to cloud.");
        });
    }

    private static Dictionary<string, object> ToFirestoreMap(object obj)
    {
        var map = new Dictionary<string, object>();
        if (obj == null) return map;

        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (var f in fields)
        {
            object v = f.GetValue(obj);
            if (v == null) continue;

            map[f.Name] = ConvertToFirestoreValue(v);
        }

        return map;
    }

    private static object ConvertToFirestoreValue(object v)
    {
        // primitives / strings
        if (v is string || v is bool || v is int || v is long || v is float || v is double)
            return v;

        // enums -> string (מומלץ לקריאות) או int אם אתה מעדיף
        var t = v.GetType();
        if (t.IsEnum)
            return v.ToString();

        // List<T> / arrays
        if (v is System.Collections.IEnumerable enumerable && v is not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                list.Add(ConvertToFirestoreValue(item));
            }
            return list;
        }

        // DateTime -> ticks (כי Firestore C# לא תמיד אוהב DateTime ישיר במיפוי ידני)
        if (v is DateTime dt)
            return dt.ToUniversalTime().Ticks;

        // אובייקט מורכב -> map רקורסיבי
        // (אם יש לך פה Unity types כמו Vector3 וכו' עדיף להמיר ידנית)
        return ToFirestoreMap(v);
    }
}
