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

        for (int i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];

            // CHANGED: Spawn the whole phase instantly (all enemies in one burst).
            SpawnPhaseInstant(phase, level);

            // CHANGED: Wait between phases using phase.spawnInterval (skip after last phase).
            bool isLastPhase = (i == phases.Count - 1);
            if (!isLastPhase)
            {
                float delay = Mathf.Max(0f, phase.spawnInterval);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }

    // CHANGED: Replaced coroutine phase spawning with instant spawning.
    private void SpawnPhaseInstant(EnemySpawnPhase phase, int level)
    {
        if (phase == null || phase.unit == null)
            return;

        int c = Mathf.Max(0, phase.count);
        for (int i = 0; i < c; i++)
        {
            SpawnOne(phase.unit, level);
            RemainingToSpawn--;
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
