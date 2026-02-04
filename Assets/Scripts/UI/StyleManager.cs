using UnityEngine;

public class StyleManager : MonoBehaviour
{
public static StyleManager instance;

    [Header("Images")]
    public Sprite goldSprite;
    public Sprite gemSprite;
    public Sprite ScrollSprite;
    public Sprite newUnitSprite;
    public Sprite commonChestSprite;
    public Sprite rareChestSprite;
    public Sprite PartWeaponSprite;
    public Sprite commonUnitBackground;
    public Sprite rareUnitBackground;
    public Sprite epicUnitBackground;

    [Header("Colors")]
    public Color commonColor;
    public Color rareColor;
    public Color epicColor;

    private void Awake()
    {
        instance = this;    
    }

}
