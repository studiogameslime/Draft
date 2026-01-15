using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public PlayerSaveData Save;

    [Header("Optional Defaults")]
    [SerializeField] private string defaultStartingStageId = "stage_1";

    private void Awake()
    {
        Debug.Log("GameData Awake");
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOrCreate();
    }

    private void LoadOrCreate()
    {
        Debug.Log("GameData LoadOrCreate start");

        Save = JsonSaveSystem.Load();

        if (Save == null)
        {
            Save = new PlayerSaveData();
            Save.currentStageId = defaultStartingStageId;

            JsonSaveSystem.Save(Save);
        }
        Debug.Log("GameData LoadOrCreate end");

    }

    public void SaveNow()
    {
        Debug.Log($"Save now {FirebaseSaveSync.Instance}");
        JsonSaveSystem.Save(Save);

        if (FirebaseSaveSync.Instance != null)
            FirebaseSaveSync.Instance.UploadNow();
    }
}
