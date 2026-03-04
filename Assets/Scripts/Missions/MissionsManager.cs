using Firebase.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MissionsManager : MonoBehaviour
{
    public static MissionsManager Instance;

    [Header("Database")]
    [SerializeField] private MissionDatabase missionDatabase;

    [Header("Active Missions")]
    [SerializeField] private List<MissionInstance> activeDailyMissions = new();
    [SerializeField] private List<MissionInstance> activeWeeklyMissions = new();


    public event Action OnMissionsStateChanged;

    public IReadOnlyList<MissionInstance> ActiveDailyMissions => activeDailyMissions;
    public IReadOnlyList<MissionInstance> ActiveWeeklyMissions => activeWeeklyMissions;

    [SerializeField] private int dailyMissionCount;
    [SerializeField] private int weeklyMissionCount;

    [Header("Set Completion Rewards")]
    [SerializeField] private int dailyAllMissionsGoldReward = 0;
    [SerializeField] private int dailyAllMissionsGemsReward = 0;
    [SerializeField] private ChestDefinition dailyAllMissionsChestReward = null;

    [SerializeField] private int weeklyAllMissionsGoldReward = 0;
    [SerializeField] private int weeklyAllMissionsGemsReward = 0;
    [SerializeField] private ChestDefinition weeklyAllMissionsChestReward = null;

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
            // Reset daily set reward flag
            save.dailySetRewardGranted = false;
        }

        if (save.weeklyMissionsLastResetTicks < weeklyReset.Ticks)
        {
            activeWeeklyMissions.Clear();
            save.activeWeeklyMissions.Clear();
            save.weeklyMissionsLastResetTicks = weeklyReset.Ticks;
            // Reset weekly set reward flag
            save.weeklySetRewardGranted = false;
        }
    }

    // ======================
    // GENERATION
    // ======================
    private void GenerateDailyMissions()
    {
        activeDailyMissions.Clear();

        // Always include the Login mission first
        var loginMission = missionDatabase.dailyMissions
            .FirstOrDefault(m => m != null && m.action == MissionAction.Login);

        if (loginMission != null)
            activeDailyMissions.Add(new MissionInstance(loginMission));

        // Fill remaining slots randomly from non-Login missions
        int remaining = dailyMissionCount - activeDailyMissions.Count;
        var others = missionDatabase.dailyMissions
            .Where(m => m != null && m.action != MissionAction.Login)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(remaining);

        foreach (var def in others)
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


        // If the mission has a chest reward, defer ALL claim logic until the player
        // confirms inside the chest info modal.  Otherwise claim immediately.
        if (mission.definition.chestReward != null && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(mission.definition.chestReward, () =>
            {
                GrantMissionRewards(mission, from);
            });
        }
        else
        {
            GrantMissionRewards(mission, from);
        }

    }

    private void GrantMissionRewards(MissionInstance mission, RectTransform from)
    {
        if (mission.definition.goldReward > 0)
        {
            int boostedGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(mission.definition.goldReward)
                : mission.definition.goldReward;
            PlayerCurrencyWallet.Instance.AddGold(boostedGold, from);
        }

        if (mission.definition.gemsReward > 0)
            PlayerCurrencyWallet.Instance.AddGems(mission.definition.gemsReward, from);

        if (mission.definition.scrollsReward > 0)
            PlayerCurrencyWallet.Instance.AddScrolls(mission.definition.scrollsReward, from);

        mission.claimed = true;
        mission.OnClaimed?.Invoke();
        OnMissionsStateChanged?.Invoke();

        SaveMissionsToGameData();
        FirebaseAnalyticsManager.Instance.LogEvent("complete_mission",
            new Parameter("mission_type", mission.definition.missionType.ToString()),
            new Parameter("mission_name", mission.definition.name));
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

    private void GrantDailySetReward(RectTransform from)
    {
        if (dailyAllMissionsChestReward != null && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(dailyAllMissionsChestReward, () =>
            {
                GrantDailySetCurrencyRewards(from);
                FinalizeDailySetClaim();
            });
        }
        else
        {
            GrantDailySetCurrencyRewards(from);
            FinalizeDailySetClaim();
        }
    }

    private void GrantDailySetCurrencyRewards(RectTransform from)
    {
        if (dailyAllMissionsGoldReward > 0)
        {
            int finalGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(dailyAllMissionsGoldReward)
                : dailyAllMissionsGoldReward;
            PlayerCurrencyWallet.Instance.AddGold(finalGold, from);
        }

        if (dailyAllMissionsGemsReward > 0)
            PlayerCurrencyWallet.Instance.AddGems(dailyAllMissionsGemsReward, from);

        Debug.Log("Daily set completion reward granted.");
    }

    private void GrantWeeklySetReward(RectTransform from)
    {
        if (weeklyAllMissionsChestReward != null && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(weeklyAllMissionsChestReward, () =>
            {
                GrantWeeklySetCurrencyRewards(from);
                FinalizeWeeklySetClaim();
            });
        }
        else
        {
            GrantWeeklySetCurrencyRewards(from);
            FinalizeWeeklySetClaim();
        }
    }

    private void GrantWeeklySetCurrencyRewards(RectTransform from)
    {
        if (weeklyAllMissionsGoldReward > 0)
        {
            int finalGold = MasteryBonusManager.Instance != null
                ? MasteryBonusManager.Instance.GetBoostedGold(weeklyAllMissionsGoldReward)
                : weeklyAllMissionsGoldReward;
            PlayerCurrencyWallet.Instance.AddGold(finalGold, from);
        }

        if (weeklyAllMissionsGemsReward > 0)
            PlayerCurrencyWallet.Instance.AddGems(weeklyAllMissionsGemsReward, from);

        Debug.Log("Weekly set completion reward granted.");
    }
    // ======================
    // SET COMPLETION (CLAIM BUTTON API)
    // ======================

    // Claim becomes available when ALL missions are completed (not necessarily claimed individually).
    public bool CanClaimDailySetReward()
    {
        var save = GameData.Instance.Save;
        if (save == null) return false;

        return !save.dailySetRewardGranted
               && activeDailyMissions.All(m => m.claimed);
    }

    public bool CanClaimWeeklySetReward()
    {
        var save = GameData.Instance.Save;
        if (save == null) return false;

        return !save.weeklySetRewardGranted
               && activeWeeklyMissions.All(m => m.claimed);
    }


    public void ClaimDailySetReward(RectTransform from)
    {
        var save = GameData.Instance.Save;
        if (save == null) return;
        if (!CanClaimDailySetReward()) return;

        GrantDailySetReward(from);
    }

    private void FinalizeDailySetClaim()
    {
        var save = GameData.Instance.Save;
        if (save == null) return;
        save.dailySetRewardGranted = true;
        SaveMissionsToGameData();
        OnMissionsStateChanged?.Invoke();
        FirebaseAnalytics.LogEvent("daily_set_reward_claimed");
    }

    public void ClaimWeeklySetReward(RectTransform from)
    {
        var save = GameData.Instance.Save;
        if (save == null) return;
        if (!CanClaimWeeklySetReward()) return;

        GrantWeeklySetReward(from);
    }

    private void FinalizeWeeklySetClaim()
    {
        var save = GameData.Instance.Save;
        if (save == null) return;
        save.weeklySetRewardGranted = true;
        SaveMissionsToGameData();
        OnMissionsStateChanged?.Invoke();
        FirebaseAnalytics.LogEvent("weekly_set_reward_claimed");
    }

    public bool IsDailySetRewardClaimed()
    {
        var save = GameData.Instance.Save;
        return save != null && save.dailySetRewardGranted;
    }

    public bool IsWeeklySetRewardClaimed()
    {
        var save = GameData.Instance.Save;
        return save != null && save.weeklySetRewardGranted;
    }


}
