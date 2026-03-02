using System.Collections.Generic;
using UnityEngine;

public class SkeletonBossSummoner : MonoBehaviour
{
    [Header("Summon Settings")]
    [SerializeField] private UnitDefinition skeletonDefinition;
    [SerializeField] private int skeletonsPerSummon = 3;
    [SerializeField] private float summonCooldown = 5f;
    [SerializeField] private float summonRadius = 1.5f;
    [SerializeField] private int maxSkeletons = 9;

    [Header("Spawn Offset")]
    [SerializeField] private float spawnYOffset = -0.25f;

    [Header("FX")]
    [SerializeField] private GameObject summonFxPrefab;

    private float summonTimer;
    private readonly List<GameObject> aliveSkeletons = new();
    private CharacterStats summonerStats;

    private void Awake()
    {
        summonerStats = GetComponent<CharacterStats>();
        summonTimer = summonCooldown;
    }

    private void Update()
    {
        if (!BattleManager.instance.IsBattleRunning) return;

        CleanupList();

        summonTimer -= Time.deltaTime;
        if (summonTimer > 0f) return;

        summonTimer = summonCooldown;

        int canSpawn = Mathf.Min(skeletonsPerSummon, maxSkeletons - aliveSkeletons.Count);
        if (canSpawn <= 0) return;

        for (int i = 0; i < canSpawn; i++)
            SummonOne(i, canSpawn);
    }

    private void SummonOne(int index, int total)
    {
        if (skeletonDefinition == null) return;

        float angle = (360f / total) * index + Random.Range(-20f, 20f);
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad) * summonRadius, spawnYOffset, 0f);
        Vector3 pos = transform.position + offset;

        if (summonFxPrefab != null)
            Instantiate(summonFxPrefab, pos + Vector3.up * -spawnYOffset, Quaternion.identity);

        GameObject sk = Instantiate(skeletonDefinition.prefab, pos, Quaternion.identity);
        aliveSkeletons.Add(sk);

        CharacterStats skStats = sk.GetComponent<CharacterStats>();
        if (skStats != null)
            skStats.Init(Team.EnemyTeam, skeletonDefinition, GetSummonerLevel());

        SkeletonEmerge emerge = sk.GetComponent<SkeletonEmerge>();
        if (emerge != null)
            emerge.enabled = true;
    }

    private int GetSummonerLevel()
    {
        if (summonerStats != null)
        {
            var f = summonerStats.GetType().GetField("level");
            if (f != null && f.FieldType == typeof(int))
            {
                int lvl = (int)f.GetValue(summonerStats);
                if (lvl > 0) return lvl;
            }
        }
        return 1;
    }

    private void CleanupList()
    {
        for (int i = aliveSkeletons.Count - 1; i >= 0; i--)
        {
            if (aliveSkeletons[i] == null)
                aliveSkeletons.RemoveAt(i);
        }
    }
}
