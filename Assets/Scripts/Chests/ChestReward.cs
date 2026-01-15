using UnityEngine;

public struct ChestReward
{
    public int gold;
    public int gems;

    public int wheelTokens;

    public bool hasPart;
    public PartRarity partRarity;
    public PartDefinition part; // Can be null if no specific part is chosen

    public override string ToString()
    {
        string partText = "No part";
        if (hasPart)
        {
            if (part != null)
                partText = $"{partRarity} {part.name}";
            else
                partText = partRarity.ToString();
        }

        string tokenText = wheelTokens > 0 ? $" | WheelTokens: {wheelTokens}" : "";

        return $"Gold: {gold}, Diamonds: {gems}, Part: {partText}{tokenText}";
    }
}
