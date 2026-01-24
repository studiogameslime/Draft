using UnityEngine;

[CreateAssetMenu(menuName = "Units/Part Definition")]
public class PartDefinition : ScriptableObject
{
    public string id;
    public UnitDefinition unit;
    public PartSlot slot;
    public PartRarity rarity;
    public GameObject prefab;

    [Header("Upgrade path")]
    public PartDefinition upgradeTo;
}

public enum PartRarity
{
    Common,
    Rare,
    Epic
}