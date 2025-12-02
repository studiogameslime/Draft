using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Rounds in this level (in order)")]
    public RoundDefinition[] rounds;

    public int RoundsCount => (rounds != null) ? rounds.Length : 0;
}

[Serializable]
public class RoundDefinition
{
    [Header("Enemy wave for this round")]
    public EnemySpawnEntry[] enemySpawns;

    [Header("How many unit picks the player gets this round")]
    public int playerPicks = 3;
}

[Serializable]
public class EnemySpawnEntry
{
    public UnitDefinition unit;
    public int count = 1;
    public int level = 1;
}
