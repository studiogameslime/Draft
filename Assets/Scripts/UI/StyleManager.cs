using UnityEngine;

public class StyleManager : MonoBehaviour
{
public static StyleManager instance;


    public Sprite goldSprite;
    public Sprite gemSprite;
    public Sprite newUnitSprite;

    private void Awake()
    {
        instance = this;    
    }

}
