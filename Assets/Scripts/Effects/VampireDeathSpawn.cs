using UnityEngine;

public class VampireDeathSpawn : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject batPrefab;

    private CharacterStats stats;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnDied += HandleVampireDied;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnDied -= HandleVampireDied;
    }

    private void HandleVampireDied(CharacterStats vampireStats)
    {
        Debug.Log("HandleVampireDied");
        if (batPrefab != null)
        {
            // Spawn the bat at current vampire position
            Instantiate(batPrefab, transform.position, Quaternion.identity);
        }
    }
}