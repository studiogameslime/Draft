using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "MissionDatabase",
    menuName = "Missions/Mission Database"
)]
public class MissionDatabase : ScriptableObject
{
    [Header("Daily Missions")]
    public List<MissionDefinition> dailyMissions = new();

    [Header("Weekly Missions")]
    public List<MissionDefinition> weeklyMissions = new();

#if UNITY_EDITOR
    [ContextMenu("Rebuild Database")]
    public void RebuildDatabase()
    {
        dailyMissions.Clear();
        weeklyMissions.Clear();

        AddMissionsFromFolder("Assets/Resources/Missions/Daily", dailyMissions);
        AddMissionsFromFolder("Assets/Resources/Missions/Weekly", weeklyMissions);

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log("MissionDatabase rebuilt successfully");
    }

    private void AddMissionsFromFolder(string folderPath, List<MissionDefinition> targetList)
    {
        string[] guids = AssetDatabase.FindAssets("t:MissionDefinition", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);

            if (mission != null && !targetList.Contains(mission))
                targetList.Add(mission);
        }
    }
    //private void OnValidate()
    //{
    //    RebuildDatabase();
    //}
#endif
}
