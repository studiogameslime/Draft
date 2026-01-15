using System;
using UnityEngine;

public class PlayerXPManager : MonoBehaviour
{
    public static PlayerXPManager Instance;

    public int currentXP;
    public int currentLevel = 1;

    public event Action<int> OnXPChanged;
    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromSave();
    }

    public int GetXPForNextLevel()
    {
        return Mathf.Max(1, currentLevel * 100);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        SaveToGameData();

        OnXPChanged?.Invoke(currentXP);
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int xpNeeded = GetXPForNextLevel();

        while (currentXP >= xpNeeded)
        {
            currentXP -= xpNeeded;
            currentLevel++;

            SaveToGameData();

            OnLevelChanged?.Invoke(currentLevel);
            OnXPChanged?.Invoke(currentXP);

            xpNeeded = GetXPForNextLevel();
        }
    }

    private void LoadFromSave()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
            return;

        currentLevel = Mathf.Max(1, GameData.Instance.Save.playerLevel);
        currentXP = Mathf.Max(0, GameData.Instance.Save.playerXP);
    }

    private void SaveToGameData()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
            return;

        GameData.Instance.Save.playerLevel = currentLevel;
        GameData.Instance.Save.playerXP = currentXP;
        GameData.Instance.SaveNow();
    }
}
