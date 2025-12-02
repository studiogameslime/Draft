using System.Collections.Generic;
using UnityEngine;

public class MonsterGrid : MonoBehaviour
{
    [Header("Horizontal spacing")]
    public float cellHeight = 1.5f;

    [Header("Columns Y (local)")]
    public float tankColumnY = 1.5f;
    public float rangerColumnY = 0f;

    private void OnTransformChildrenChanged()
    {
        ArrangeMonsters();
    }

    public void ArrangeMonsters()
    {
        List<Transform> tanks = new List<Transform>();
        List<Transform> rangers = new List<Transform>();

        foreach (Transform child in transform)
        {
            var data = child.GetComponent<CharacterStats>();
            if (data == null) continue;

            // Do not rearrange locked units
            if (data.lockedIn) continue;

            if (data.monsterType == MonsterType.Melee)
                tanks.Add(child);
            else if (data.monsterType == MonsterType.Ranged)
                rangers.Add(child);
        }

        PositionColumnCentered(tanks, tankColumnY);
        PositionColumnCentered(rangers, rangerColumnY);
    }

    private void PositionColumnCentered(List<Transform> list, float startY)
    {
        int count = list.Count;
        if (count == 0) return;

        int maxPerRow = 8;          // how many units per row
        float rowHeight = 1.2f;     // vertical spacing between rows

        int rows = Mathf.CeilToInt(count / (float)maxPerRow);
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            int unitsInThisRow = Mathf.Min(maxPerRow, count - (row * maxPerRow));
            float half = (unitsInThisRow - 1) / 2f;
            float y = startY - (row * rowHeight);

            for (int i = 0; i < unitsInThisRow; i++)
            {
                float x = (i - half) * cellHeight;
                list[index].localPosition = new Vector3(x, y, 0f);
                index++;
            }
        }
    }

    // New API: add by UnitDefinition
    public GameObject AddMonster(UnitDefinition def, Team team, int level = 1)
    {
        if (def == null || def.prefab == null)
        {
            Debug.LogError("MonsterGrid.AddMonster: UnitDefinition or prefab is null");
            return null;
        }

        GameObject monster = Instantiate(def.prefab, transform);

        var stats = monster.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Init(team, def, level);
        }
        else
        {
            Debug.LogError("MonsterGrid.AddMonster: CharacterStats missing on prefab " + def.prefab.name);
        }

        var ai = monster.GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        ArrangeMonsters();
        return monster;
    }

    // Convenience overload for "my team, level 1"
    public GameObject AddMonster(UnitDefinition def)
    {
        return AddMonster(def, Team.MyTeam, 1);
    }
}
