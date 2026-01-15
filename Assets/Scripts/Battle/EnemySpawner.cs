using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform spawnPoint;

    private void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    public GameObject Spawn(UnitDefinition def, int level)
    {
        GameObject go = Instantiate(def.prefab, spawnPoint.position, Quaternion.identity);
        var stats = go.GetComponent<CharacterStats>();
        stats.Init(Team.EnemyTeam, def, level);

        var ai = go.GetComponent<UnitAI>();
        if (ai != null)
            ai.enabled = true; 

        return go;
    }
}
