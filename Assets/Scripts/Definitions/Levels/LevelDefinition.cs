using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Rounds in this level (in order)")]
    public RoundDefinition[] rounds;

    public int RoundsCount => (rounds != null) ? rounds.Length : 0;

    [Header("XP")]
    public int xpOnLevelComplete = 50;

    [Header("Gold Rewards")]
    [Tooltip("Gold awarded when the player completes the whole level (all rounds).")]
    public int goldOnLevelComplete = 100;

    [Tooltip("If true, gold is also awarded per round using RoundDefinition.goldOnRoundWin.")]
    public bool alsoRewardGoldPerRound = false;

    public int GetTotalGoldIfAllRoundsWon()
    {
        int total = Mathf.Max(0, goldOnLevelComplete);
        if (alsoRewardGoldPerRound && rounds != null)
        {
            foreach (var r in rounds)
                total += Mathf.Max(0, r.goldOnRoundWin);
        }
        return total;
    }

    public int GetGoldFromRounds(int roundsCompleted)
    {
        int total = 0;
        if (!alsoRewardGoldPerRound || rounds == null) return 0;

        int n = Mathf.Clamp(roundsCompleted, 0, rounds.Length);
        for (int i = 0; i < n; i++)
            total += Mathf.Max(0, rounds[i].goldOnRoundWin);

        return total;
    }

    public int GetXpFromRounds(int roundsCompleted)
    {
        int total = 0;
        if (rounds == null) return 0;

        int n = Mathf.Clamp(roundsCompleted, 0, rounds.Length);
        for (int i = 0; i < n; i++)
            total += Mathf.Max(0, rounds[i].xpOnRoundWin);

        return total;
    }

}

[Serializable]
public class RoundDefinition
{
    //[Header("Enemy wave for this round")]
    //public EnemySpawnEntry[] enemySpawns;

    [Header("How many unit picks the player gets this round")]
    public int playerPicks = 3;

    [Header("Souls")]
    public int souls = 10;

    [Header("Gold (optional)")]
    [Tooltip("Gold awarded when this round is won (only used if LevelDefinition.alsoRewardGoldPerRound = true).")]
    public int goldOnRoundWin = 0;

    public List<EnemySpawnPhase> enemyPhases;
    public int enemyLevel = 1;

    [Header("XP")]
    public int xpOnRoundWin = 20;
}

[Serializable]
public class EnemySpawnPhase
{
    public UnitDefinition unit;
    public int count;

    // CHANGED: spawnInterval is now used as the delay AFTER this phase, before the next phase starts.
    public float spawnInterval;
}


//[Serializable]
//public class EnemySpawnEntry
//{
//    public UnitDefinition unit;
//    public int count = 1;
   
//}
