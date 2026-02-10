using System.Collections;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }
    public PlayerSaveData Save;

    // Init gating
    public bool IsReady { get; private set; }
    private bool pendingCloudUpload;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsReady = false;
        pendingCloudUpload = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(LoadCloudThenFallback());
#else
        LoadOrCreateLocal();
        IsReady = true;
#endif
    }

    private void LoadOrCreateLocal()
    {
        Save = JsonSaveSystem.Load();
        if (Save == null)
        {
            Save = new PlayerSaveData();
            JsonSaveSystem.Save(Save);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator LoadCloudThenFallback()
    {
        // Make sure we have a Save object so other scripts won't crash while we are loading.
        LoadOrCreateLocal();

        while (FirebaseSaveSync.Instance == null)
            yield return null;

        FirebaseSaveSync.Instance.BeginDownload();

        float timeout = 8f;
        float start = Time.realtimeSinceStartup;

        while (!FirebaseSaveSync.Instance.CloudLoadDone &&
               (Time.realtimeSinceStartup - start) < timeout)
            yield return null;

        if (FirebaseSaveSync.Instance.CloudLoadDone &&
            FirebaseSaveSync.Instance.CloudDocExists &&
            FirebaseSaveSync.Instance.CloudSaveData != null)
        {
            Save = FirebaseSaveSync.Instance.CloudSaveData;

            // Update local cache WITHOUT triggering another cloud upload during init.
            JsonSaveSystem.Save(Save);

            Debug.Log("[GameData] Loaded from cloud and wrote local cache");
        }
        else
        {
            Debug.Log("[GameData] Cloud not available -> using local fallback");
            // We already loaded local at the top of this coroutine.
        }

        IsReady = true;

        // If someone called SaveNow during init, upload once now.
        if (pendingCloudUpload && FirebaseSaveSync.Instance != null)
        {
            pendingCloudUpload = false;
            FirebaseSaveSync.Instance.UploadNow();
        }
    }
#endif

    public void SaveNow()
    {
        // Always save local.
        JsonSaveSystem.Save(Save);
        MasteryBonusManager.Instance?.RebuildCache();

#if UNITY_ANDROID && !UNITY_EDITOR
        // Block cloud upload until initial cloud/local decision is done.
        if (!IsReady)
        {
            pendingCloudUpload = true;
            Debug.Log("[GameData] SaveNow during init -> pending cloud upload");
            return;
        }

        if (FirebaseSaveSync.Instance != null)
            FirebaseSaveSync.Instance.UploadNow();
#endif
    }
}
