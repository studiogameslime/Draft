using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestPartDropChance : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private Image rarityIcon;

    public void Bind(PartRarity rarity, float weight)
    {
        if (rarityText != null)
            rarityText.text = rarity.ToString();

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(weight)}%";

        switch (rarity)
        {
            case PartRarity.Common:
                rarityIcon.color = Color.green;
                break;
            case PartRarity.Rare:
                rarityIcon.color = Color.blue;
                break;
            case PartRarity.Epic:
                rarityIcon.color = Color.purple;
                break;
        }

    }


}
