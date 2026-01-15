using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Store/Store Database")]
public class StoreDatabase : ScriptableObject
{
    public List<StoreItemDefinition> allItems = new List<StoreItemDefinition>();

    public List<StoreItemDefinition> GetItems(StoreCategory category)
    {
        var result = new List<StoreItemDefinition>();

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (!item.isEnabled) continue;
            if (item.category != category) continue;

            result.Add(item);
        }

        return result;
    }
}
