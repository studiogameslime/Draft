using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public PlayerSaveData Save;

    [Header("Optional Defaults")]
    [SerializeField] private string defaultStartingStageId = "stage_1";

    private void Awake()
    {
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

        Save = JsonSaveSystem.Load();

        if (Save == null)
        {
            Save = new PlayerSaveData();
            Save.currentStageId = defaultStartingStageId;

            JsonSaveSystem.Save(Save);
        }

    }

    public void SaveNow()
    {
        JsonSaveSystem.Save(Save);

        if (FirebaseSaveSync.Instance != null)
            FirebaseSaveSync.Instance.UploadNow();
    }
}
