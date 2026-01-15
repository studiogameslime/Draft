using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    private List<EnemySpawner> spawners = new();

    public int RemainingToSpawn { get; private set; }
    public bool FinishedSpawning => RemainingToSpawn <= 0;

    private void Awake()
    {
        var objs = GameObject.FindGameObjectsWithTag("EnemySpawner");
        foreach (var o in objs)
        {
            var sp = o.GetComponent<EnemySpawner>();
            if (sp != null)
                spawners.Add(sp);
        }
    }

    public void StartWave(List<EnemySpawnPhase> phases, int enemyLevel)
    {
        StopAllCoroutines();

        RemainingToSpawn = 0;
        foreach (var p in phases)
            RemainingToSpawn += p.count;

        StartCoroutine(SpawnWaveRoutine(phases, enemyLevel));
    }

    private IEnumerator SpawnWaveRoutine(List<EnemySpawnPhase> phases, int level)
    {
        if (phases == null || phases.Count == 0)
            yield break;

        foreach (var phase in phases)
        {
            yield return StartCoroutine(SpawnPhase(phase, level));
        }
    }

    private IEnumerator SpawnPhase(EnemySpawnPhase phase, int level)
    {
        for (int i = 0; i < phase.count; i++)
        {
            SpawnOne(phase.unit, level);
            RemainingToSpawn--;

            yield return new WaitForSeconds(phase.spawnInterval);
        }
    }

    private void SpawnOne(UnitDefinition def, int level)
    {
        if (spawners.Count == 0)
        {
            Debug.LogError("EnemyWaveSpawner: No EnemySpawners found!");
            return;
        }

        var spawner = spawners[Random.Range(0, spawners.Count)];
        spawner.Spawn(def, level);
    }
}
