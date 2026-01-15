using UnityEngine;

[CreateAssetMenu(menuName = "Fortune Wheel/Slot Definition", fileName = "WheelSlot_")]
public class FortuneWheelSlotDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;

    [Header("Display")]
    public string displayName;
    public Sprite icon;

    [Header("Reward")]
    public FortuneWheelReward reward = new FortuneWheelReward();
}
