using System.Collections.Generic;
using UnityEngine;

public class SkeletonSummoner : MonoBehaviour
{
    [Header("Summon Settings")]
    [SerializeField] private UnitDefinition skeletonDefinition;
    [SerializeField] private Transform summonPoint;
    [SerializeField] private GameObject summonFxPrefab;
    [SerializeField] private int maxSkeletons = 5;
    [SerializeField] private float summonCooldown = 6f;

    [Header("Spawn Offset (for 'emerge from ground' feel)")]
    [SerializeField] private float spawnYOffset = -0.25f;

    [Header("Level (SkeletonLevel = SummonerLevel)")]
    [SerializeField] private int fallbackLevel = 1;

    private float summonTimer;
    private readonly List<GameObject> aliveSkeletons = new();

    private CharacterStats summonerStats;

    private void Awake()
    {
        if (summonPoint == null) summonPoint = transform;
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

        if (aliveSkeletons.Count >= maxSkeletons) return;

        SummonOne();
    }

    private void SummonOne()
    {
        if (skeletonDefinition == null) return;

        int level = GetSummonerLevel();

        Vector3 pos = summonPoint.position + new Vector3(0f, spawnYOffset, 0f);

        if (summonFxPrefab != null)
            Instantiate(summonFxPrefab, summonPoint.position, Quaternion.identity);

        GameObject sk = Instantiate(skeletonDefinition.prefab, pos, Quaternion.identity);
        aliveSkeletons.Add(sk);

        CharacterStats skStats = sk.GetComponent<CharacterStats>();
        if (skStats != null)
        {
            skStats.Init(Team.EnemyTeam, skeletonDefinition, GetSummonerLevel());
            
        }
        SkeletonEmerge skeletonEmerge = sk.GetComponent<SkeletonEmerge>();
        if (skeletonEmerge != null)
        {
            skeletonEmerge.enabled = true; // Coming from the ground
        }
    }

    private int GetSummonerLevel()
    {
        if (summonerStats != null)
        {
            int lvl = TryGetPublicIntField(summonerStats, "level");
            if (lvl > 0) return lvl;
        }

        return Mathf.Max(1, fallbackLevel);
    }

    private void CleanupList()
    {
        for (int i = aliveSkeletons.Count - 1; i >= 0; i--)
        {
            if (aliveSkeletons[i] == null)
                aliveSkeletons.RemoveAt(i);
        }
    }

    // ---- tiny reflection helpers (רק לשדה public int level; לא חובה שתהיה לכם) ----
    private static int TryGetPublicIntField(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName);
        if (f != null && f.FieldType == typeof(int))
            return (int)f.GetValue(obj);
        return -1;
    }

    private static void TrySetPublicIntField(object obj, string fieldName, int value)
    {
        var f = obj.GetType().GetField(fieldName);
        if (f != null && f.FieldType == typeof(int))
            f.SetValue(obj, value);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (summonPoint == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(summonPoint.position, 0.15f);
    }
#endif
}
