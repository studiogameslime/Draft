using UnityEngine;

public class MonsterAutoSpawner : MonoBehaviour
{
    public MonsterGrid rightGrid;
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public float spawnInterval = 5f; // spawn every 5 seconds

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnRandomEnemyWave();
        }
    }

    private void SpawnRandomEnemyWave()
    {
        if (rightGrid == null) return;

        // Randomly choose unit type
        MonsterType type = (Random.value > 0.5f) ? MonsterType.Melee : MonsterType.Ranged;
        GameObject prefab = (type == MonsterType.Melee) ? meleePrefab : rangedPrefab;

        if (prefab == null) return;

        int count = Random.Range(1, 4); // spawn 1–3 units

        for (int i = 0; i < count; i++)
        {
            GameObject monster = rightGrid.AddMonster(prefab, type);
    
            // Set team to Enemy
            var stats = monster.GetComponent<CharacterStats>();
            stats.Init(Team.EnemyTeam, type);

        }
    }
}
