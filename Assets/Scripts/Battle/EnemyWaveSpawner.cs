using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    private List<EnemySpawner> spawners = new();
    public Transform enemiesContainer;

    public int RemainingToSpawn { get; private set; }
    public bool FinishedSpawning => RemainingToSpawn <= 0;

    [Header("Timing")]
    [Tooltip("CHANGED: Small delay before the first enemy of the wave spawns.")]
    [SerializeField] private float delayBeforeWaveStarts = 0.5f;

    [Tooltip("CHANGED: Small delay between enemies inside the same phase.")]
    [SerializeField] private float delayBetweenEnemies = 0.1f;

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

        // CHANGED: Delay before enemies start coming out
        float preDelay = Mathf.Max(0f, delayBeforeWaveStarts);
        if (preDelay > 0f)
            yield return new WaitForSeconds(preDelay);

        for (int i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];

            // CHANGED: Spawn the phase as a burst, but with a small delay between enemies
            yield return StartCoroutine(SpawnPhaseWithSmallDelay(phase, level));

            // Keep existing behavior: wait between phases using phase.spawnInterval (skip after last phase)
            bool isLastPhase = (i == phases.Count - 1);
            if (!isLastPhase)
            {
                float delay = Mathf.Max(0f, phase.spawnInterval);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }

    // CHANGED: New coroutine to support delay between enemies
    private IEnumerator SpawnPhaseWithSmallDelay(EnemySpawnPhase phase, int level)
    {
        if (phase == null || phase.unit == null)
            yield break;

        int c = Mathf.Max(0, phase.count);
        float perEnemyDelay = Mathf.Max(0f, delayBetweenEnemies);

        for (int i = 0; i < c; i++)
        {
            SpawnOne(phase.unit, level);
            RemainingToSpawn--;

            if (perEnemyDelay > 0f && i < c - 1)
                yield return new WaitForSeconds(perEnemyDelay);
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
        spawner.Spawn(def, level, enemiesContainer);
    }
}
