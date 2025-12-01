using System.Collections.Generic;
using UnityEngine;

public class MonsterGrid : MonoBehaviour
{
    [Header("Vertical spacing")]
    public float cellHeight = 1.5f;

    [Header("Columns X (local)")]
    public float tankColumnX = 1.5f;    
    public float rangerColumnX = 0f;    

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

            // if character move so it w'ont be rearrange
            if (data.lockedIn) continue;

            if (data.monsterType == MonsterType.Melee)
                tanks.Add(child);
            else if (data.monsterType == MonsterType.Ranged)
                rangers.Add(child);
        }


        PositionColumnCentered(tanks, tankColumnX);

        PositionColumnCentered(rangers, rangerColumnX);
    }

    private void PositionColumnCentered(List<Transform> list, float x)
    {
        int count = list.Count;
        if (count == 0) return;

       
        float half = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float offsetIndex = i - half;      
            float y = -offsetIndex * cellHeight;

            list[i].localPosition = new Vector3(x, y, 0f);
        }
    }

    public GameObject AddMonster(GameObject prefab, MonsterType type)
    {
        GameObject monster = Instantiate(prefab, transform);

        var data = monster.GetComponent<CharacterStats>();
        data.Init(Team.MyTeam, type);
        if (data != null)
        {
            data.monsterType = type;
        }

        ArrangeMonsters();
        return monster;
    }
}
