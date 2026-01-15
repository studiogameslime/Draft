using UnityEngine;

public class SoulOrbSpawner : MonoBehaviour
{
    public static SoulOrbSpawner instance;

    [Header("Prefabs & Targets")]
    public SoulOrb soulOrbPrefab;      
    [HideInInspector] public Transform soulPoolTarget;   

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        soulPoolTarget = transform;
    }

    /// <summary>
    /// Spawns a soul orb at the given world position and sends it to the pool.
    /// </summary>
    public void SpawnSoul(Vector3 worldPos, int amount = 1)
    {
        if (soulOrbPrefab == null || soulPoolTarget == null)
        {
            Debug.LogWarning("SoulOrbSpawner: prefab or target is missing.");
            return;
        }

        SoulOrb orb = Instantiate(soulOrbPrefab, worldPos, Quaternion.identity);
        orb.Init(worldPos, soulPoolTarget, amount);
    }
}
