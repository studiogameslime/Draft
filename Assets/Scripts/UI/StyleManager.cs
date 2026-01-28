using UnityEngine;

public class StyleManager : MonoBehaviour
{
public static StyleManager instance;


    public Sprite goldSprite;
    public Sprite gemSprite;
    public Sprite newUnitSprite;
    public Sprite commonChestSprite;
    public Sprite rareChestSprite;
    public Sprite PartWeaponSprite;
    public Sprite commonUnitBackground;
    public Sprite rareUnitBackground;
    public Sprite epicUnitBackground;

    private void Awake()
    {
        instance = this;    
    }

}
