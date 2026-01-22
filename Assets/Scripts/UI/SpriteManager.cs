using UnityEngine;

public class SpriteManager : MonoBehaviour
{
public static SpriteManager instance;


    public Sprite goldSprite;
    public Sprite gemSprite;
    public Sprite newUnitSprite;

    private void Awake()
    {
        instance = this;    
    }
}
