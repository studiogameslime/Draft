using System.Collections.Generic;
using UnityEngine;

public class EnemyPreviewBubblesController : MonoBehaviour
{
    public static EnemyPreviewBubblesController Instance;
    [Header("UI")]
    [SerializeField] private Transform bubblesRoot;
    [SerializeField] private EnemyPreviewBubble bubblePrefab;

    private readonly List<GameObject> spawnedBubbles = new();

    private void Awake()
    {
        Instance = this;
    }

    public void BuildForRound(LevelDefinition levelDefinition, int roundIndex)
    {
        if (levelDefinition == null || levelDefinition.rounds == null) return;
        if (roundIndex < 0 || roundIndex >= levelDefinition.rounds.Length) return;

        BuildForRound(levelDefinition.rounds[roundIndex]);
    }

    public void BuildForRound(RoundDefinition round)
    {
        ClearBubbles();

        if (round == null || round.enemyPhases == null || round.enemyPhases.Count == 0)
            return;

        if (bubblesRoot == null || bubblePrefab == null)
        {
            Debug.LogWarning("EnemyPreviewBubblesController: missing bubblesRoot or bubblePrefab.");
            return;
        }

        // Sum counts per unit, keep order of first appearance
        var totals = new Dictionary<UnitDefinition, int>();
        var orderedUnits = new List<UnitDefinition>();

        foreach (var phase in round.enemyPhases)
        {
            if (phase == null || phase.unit == null) continue;

            if (!totals.ContainsKey(phase.unit))
            {
                totals[phase.unit] = 0;
                orderedUnits.Add(phase.unit);
            }

            totals[phase.unit] += Mathf.Max(0, phase.count);
        }

        foreach (var unit in orderedUnits)
        {
            int count = totals[unit];
            if (count <= 0) continue;

            var bubble = Instantiate(bubblePrefab, bubblesRoot);
            bubble.Init(unit, count);
            spawnedBubbles.Add(bubble.gameObject);
        }
    }

    public void ClearBubbles()
    {
        for (int i = 0; i < spawnedBubbles.Count; i++)
        {
            if (spawnedBubbles[i] != null)
                Destroy(spawnedBubbles[i]);
        }
        spawnedBubbles.Clear();
    }
}
