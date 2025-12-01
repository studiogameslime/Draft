using System.Collections.Generic;
using UnityEngine;

public class MonsterGrid : MonoBehaviour
{
    [Header("Vertical spacing")]
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

            // if character move so it won't be rearranged
            if (data.lockedIn) continue;

            if (data.monsterType == MonsterType.Melee)
                tanks.Add(child);
            else if (data.monsterType == MonsterType.Ranged)
                rangers.Add(child);
        }

        PositionColumnCentered(tanks, tankColumnY);
        PositionColumnCentered(rangers, rangerColumnY);
    }

    private void PositionColumnCentered(List<Transform> list, float y)
    {
        int count = list.Count;
        if (count == 0) return;

        float half = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float offsetIndex = i - half;
            float x = -offsetIndex * cellHeight;
            list[i].localPosition = new Vector3(x, y, 0f);
        }
    }

    public GameObject AddMonster(GameObject prefab, MonsterType type)
    {
        return AddMonster(prefab, type, Team.MyTeam);
    }

    public GameObject AddMonster(GameObject prefab, MonsterType type, Team team)
    {
        GameObject monster = Instantiate(prefab, transform);
        var data = monster.GetComponent<CharacterStats>();

        if (data != null)
        {
            data.Init(team, type);
            data.monsterType = type;
        }

        var ai = monster.GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        ArrangeMonsters();
        return monster;
    }
}
