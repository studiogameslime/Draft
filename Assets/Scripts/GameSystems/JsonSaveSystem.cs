using System.IO;
using UnityEngine;

public static class JsonSaveSystem
{
    private const string FileName = "player_save.json";

    private static string FullPath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(PlayerSaveData data)
    {
        if (data == null) return;
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FullPath, json);
    }

    public static PlayerSaveData Load()
    {
        if (!File.Exists(FullPath))
            return null;

        string json = File.ReadAllText(FullPath);
        Debug.Log(FullPath);
        return JsonUtility.FromJson<PlayerSaveData>(json);
    }

    public static bool HasSaveFile()
    {
        return File.Exists(FullPath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(FullPath))
            File.Delete(FullPath);
    }
}
