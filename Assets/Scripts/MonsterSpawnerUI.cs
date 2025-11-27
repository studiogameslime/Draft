using UnityEngine;

public class MonsterSpawnerUI : MonoBehaviour
{
    public MonsterGrid leftGrid;
    public GameObject meleePrefab;
    public GameObject rangedPrefab;

    public void SpawnTank()
    {
        SpawnType(leftGrid, MonsterType.Melee);
    }

    public void SpawnRanger()
    {
        SpawnType(leftGrid, MonsterType.Ranged);
    }

    private void SpawnType(MonsterGrid grid, MonsterType type)
    {
        GameObject monsterPrefab = type == MonsterType.Melee ? meleePrefab : rangedPrefab;
        if (grid == null || monsterPrefab == null) return;

        int count = Random.Range(1, 6);

        for (int i = 0; i < count; i++)
        {
            grid.AddMonster(monsterPrefab, type);
        }
    }
}
