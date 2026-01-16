using Firebase.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionsManager : MonoBehaviour
{
    public static MissionsManager Instance;

    [Header("Database")]
    [SerializeField] private MissionDatabase missionDatabase;

    [Header("Active Missions")]
    [SerializeField] private List<MissionInstance> activeDailyMissions = new();
    [SerializeField] private List<MissionInstance> activeWeeklyMissions = new();

    [Header("Reward UI")]
    [SerializeField] private ChestOpeningUI chestOpeningUI;

    public event Action OnMissionsStateChanged;

    public IReadOnlyList<MissionInstance> ActiveDailyMissions => activeDailyMissions;
    public IReadOnlyList<MissionInstance> ActiveWeeklyMissions => activeWeeklyMissions;

    [SerializeField] private int dailyMissionCount = 8;
    [SerializeField] private int weeklyMissionCount = 3;

    // ======================
    // UNITY
    // ======================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        //missionDatabase.RebuildDatabase();

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (missionDatabase == null)
        {
            Debug.LogError("MissionsManager: MissionDatabase is NOT assigned!");
            return;
        }

        HandleResetsIfNeeded();
        LoadMissionsFromGameData();

        if (activeDailyMissions.Count == 0)
            GenerateDailyMissions();

        if (activeWeeklyMissions.Count == 0)
            GenerateWeeklyMissions();

        DailyLoginManager.instance?.CheckDailyLogin();

        OnMissionsStateChanged?.Invoke();
        SaveMissionsToGameData();


    }

    // ======================
    // RESET LOGIC
    // ======================
    private void HandleResetsIfNeeded()
    {
        DateTime nowUtc = DateTime.UtcNow;
        var save = GameData.Instance.Save;

        DateTime dailyReset = DailyResetUtil.GetCurrentDailyResetUtc(nowUtc);
        DateTime weeklyReset = DailyResetUtil.GetCurrentWeeklyResetUtc(nowUtc);

        if (save.dailyMissionsLastResetTicks < dailyReset.Ticks)
        {
            activeDailyMissions.Clear();
            save.activeDailyMissions.Clear();
            save.dailyMissionsLastResetTicks = dailyReset.Ticks;
        }

        if (save.weeklyMissionsLastResetTicks < weeklyReset.Ticks)
        {
            activeWeeklyMissions.Clear();
            save.activeWeeklyMissions.Clear();
            save.weeklyMissionsLastResetTicks = weeklyReset.Ticks;
        }
    }

    // ======================
    // GENERATION
    // ======================
    private void GenerateDailyMissions()
    {
        activeDailyMissions.Clear();

        var selected = missionDatabase.dailyMissions
            .Where(m => m != null)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(dailyMissionCount);

        foreach (var def in selected)
            activeDailyMissions.Add(new MissionInstance(def));
    }

    private void GenerateWeeklyMissions()
    {
        activeWeeklyMissions.Clear();

        var selected = missionDatabase.weeklyMissions
            .Where(m => m != null)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(weeklyMissionCount);

        foreach (var def in selected)
            activeWeeklyMissions.Add(new MissionInstance(def));
    }


    // ======================
    // PROGRESS
    // ======================
    public void ReportAction(
        MissionAction action,
        int amount = 1,
        UnitDefinition unitDef = null,
        UnitClass unitClass = UnitClass.None)
    {
        UpdateMissions(activeDailyMissions, action, amount, unitDef, unitClass);
        UpdateMissions(activeWeeklyMissions, action, amount, unitDef, unitClass);

        SaveMissionsToGameData();
        OnMissionsStateChanged?.Invoke();

    }

    private void UpdateMissions(
        List<MissionInstance> missions,
        MissionAction action,
        int amount,
        UnitDefinition unitDef,
        UnitClass unitClass)
    {
        foreach (var mission in missions)
        {
            if (mission.completed)
                continue;

            if (mission.definition.action != action)
                continue;

            if (mission.definition.requiredUnitClass != UnitClass.None &&
                mission.definition.requiredUnitClass != unitClass)
                continue;

            if (mission.definition.requiredUnit != null &&
                mission.definition.requiredUnit != unitDef)
                continue;

            mission.AddProgress(amount);
            OnMissionsStateChanged?.Invoke();


        }
    }

    // ======================
    // CLAIM
    // ======================
    public void ClaimMission(MissionInstance mission, RectTransform from)
    {
        if (mission == null || !mission.completed || mission.claimed)
            return;

        if (mission.definition.goldReward > 0)
        {
            PlayerCurrencyWallet.Instance.AddGold(mission.definition.goldReward, from);
        }

        if (mission.definition.gemsReward > 0)
        {
            PlayerCurrencyWallet.Instance.AddGems(mission.definition.gemsReward, from);
        }

        if (mission.definition.chestReward != null && chestOpeningUI != null)
        {
            chestOpeningUI.gameObject.SetActive(true);
            chestOpeningUI.Show(mission.definition.chestReward);
        }

        mission.claimed = true;
        mission.OnClaimed?.Invoke();
        OnMissionsStateChanged?.Invoke();


        SaveMissionsToGameData();
        FirebaseAnalytics.LogEvent("complete_mission", new Parameter("mission_type", mission.definition.missionType.ToString()), new Parameter("mission_name", mission.definition.name));

    }

    // ======================
    // SAVE / LOAD (GameData)
    // ======================
    private void LoadMissionsFromGameData()
    {
        activeDailyMissions.Clear();
        activeWeeklyMissions.Clear();

        LoadMissionList(
            activeDailyMissions,
            missionDatabase.dailyMissions,
            GameData.Instance.Save.activeDailyMissions
        );

        LoadMissionList(
            activeWeeklyMissions,
            missionDatabase.weeklyMissions,
            GameData.Instance.Save.activeWeeklyMissions
        );
    }

    private void LoadMissionList(
        List<MissionInstance> target,
        List<MissionDefinition> definitions,
        List<MissionSaveData> saved)
    {
        if (saved == null)
            return;

        foreach (var data in saved)
        {
            var def = definitions.FirstOrDefault(d => d != null && d.id == data.missionId);
            if (def == null)
                continue;

            var instance = new MissionInstance(def)
            {
                currentProgress = data.currentProgress,
                completed = data.completed,
                claimed = data.claimed
            };

            target.Add(instance);
        }
    }

    private void SaveMissionsToGameData()
    {
        SaveMissionList(
            activeDailyMissions,
            GameData.Instance.Save.activeDailyMissions
        );

        SaveMissionList(
            activeWeeklyMissions,
            GameData.Instance.Save.activeWeeklyMissions
        );

        GameData.Instance.SaveNow();
    }

    private void SaveMissionList(
        List<MissionInstance> source,
        List<MissionSaveData> target)
    {
        target.Clear();

        foreach (var m in source)
        {
            target.Add(new MissionSaveData
            {
                missionId = m.definition.id,
                currentProgress = m.currentProgress,
                completed = m.completed,
                claimed = m.claimed
            });
        }
    }
    public bool HasUnclaimedCompletedMissions()
    {
        return activeDailyMissions.Any(m => m.completed && !m.claimed)
            || activeWeeklyMissions.Any(m => m.completed && !m.claimed);
    }
    public int GetClaimedDailyCount()
    {
        return activeDailyMissions.Count(m => m.claimed);
    }

    public int GetClaimedWeeklyCount()
    {
        return activeWeeklyMissions.Count(m => m.claimed);
    }

    public int GetTotalDailyCount()
    {
        return activeDailyMissions.Count;
    }

    public int GetTotalWeeklyCount()
    {
        return activeWeeklyMissions.Count;
    }

}
