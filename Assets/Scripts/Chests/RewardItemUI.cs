using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum RewardItemType
{
    Gold,
    Diamonds,
    Part,
    WheelToken,
    Scrolls
}

public class RewardItemUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform visualRoot;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;

    [Header("FX")]
    [Tooltip("FX will start from this rect (usually the icon inside the card).")]
    [SerializeField] private RectTransform fxFrom;

    [Header("Prefabs")]
    public GameObject goldPrefab;
    public GameObject diamondPrefab;
    public GameObject scrollsPrefab;
    public GameObject partContainerPrefab;

    public GameObject wheelTokenPrefab;

    [Header("Rarity Colors (optional)")]
    public Image rarityFrame;
    public Color greenColor = new Color(0.3f, 1f, 0.3f);
    public Color blueColor = new Color(0.4f, 0.7f, 1f);
    public Color purpleColor = new Color(0.7f, 0.4f, 1f);

    private RewardItemType _type;
    public RewardItemType Type => _type;

    private RectTransform _partVisualToFly;

    public RectTransform FxFrom => fxFrom != null ? fxFrom : (RectTransform)transform;
    public RectTransform PartVisualToFly => _partVisualToFly;

    public void SetupGold(int amount)
    {
        _type = RewardItemType.Gold;
        ClearVisual();
        InstantiatePrefabUnderRoot(goldPrefab);
        if (nameText != null) nameText.text = "Gold";
        if (amountText != null) amountText.text = amount.ToString();
        if (rarityFrame != null) rarityFrame.color = Color.white;
    }

    public void SetupDiamonds(int amount)
    {
        _type = RewardItemType.Diamonds;
        ClearVisual();
        InstantiatePrefabUnderRoot(diamondPrefab);
        if (nameText != null) nameText.text = "Diamonds";
        if (amountText != null) amountText.text = amount.ToString();
        if (rarityFrame != null) rarityFrame.color = Color.cyan;
    }


    public void SetupScrolls(int amount)
    {
        _type = RewardItemType.Scrolls;
        ClearVisual();
        InstantiatePrefabUnderRoot(scrollsPrefab);
        if (nameText != null) nameText.text = "Scrolls";
        if (amountText != null) amountText.text = amount.ToString();
        if (rarityFrame != null) rarityFrame.color = Color.cyan;
    }

    public void SetupWheelToken(int amount)
    {
        _type = RewardItemType.WheelToken;
        ClearVisual();
        InstantiatePrefabUnderRoot(wheelTokenPrefab);
        if (nameText != null) nameText.text = "Wheel Token";
        if (amountText != null) amountText.text = amount.ToString();
        if (rarityFrame != null) rarityFrame.color = Color.white;
    }

    public void SetupPart(PartDefinition part, PartRarity rarity)
    {
        _type = RewardItemType.Part;
        ClearVisual();

        GameObject container = null;

        if (partContainerPrefab != null && visualRoot != null)
        {
            container = Instantiate(partContainerPrefab, visualRoot);
            RectTransform cRect = container.GetComponent<RectTransform>();
            _partVisualToFly = cRect != null ? cRect : visualRoot;

            if (cRect != null)
            {
                cRect.anchoredPosition = Vector2.zero;
                cRect.localScale = Vector3.one;
            }

            Image bg = container.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = rarity switch
                {
                    PartRarity.Common => StyleManager.instance.commonColor,
                    PartRarity.Rare => StyleManager.instance.rareColor,
                    PartRarity.Epic => StyleManager.instance.epicColor,
                    _ => Color.white
                };
            }
        }
        else
        {
            _partVisualToFly = visualRoot;
        }

        Transform parent = container != null ? container.transform : (Transform)visualRoot;

        // CHANGED: Create a UI Image dynamically since we no longer use prefabs for parts
        if (part != null && part.partSprite != null && parent != null)
        {
            GameObject partObj = new GameObject("PartSprite");
            partObj.transform.SetParent(parent, false);

            RectTransform pr = partObj.AddComponent<RectTransform>();
            pr.anchoredPosition = Vector2.zero;
            pr.localScale = Vector3.one;
            // Stretch to fill parent container
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;

            Image img = partObj.AddComponent<Image>();
            img.sprite = part.partSprite;
            img.preserveAspect = true;
        }

        if (nameText != null) nameText.text = part != null ? part.name : "Part";
        if (amountText != null) amountText.text = "1";
    }

    private void ClearVisual()
    {
        _partVisualToFly = null;
        if (visualRoot == null) return;
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
            Destroy(visualRoot.GetChild(i).gameObject);
    }

    private void InstantiatePrefabUnderRoot(GameObject prefab)
    {
        if (prefab == null || visualRoot == null) return;
        GameObject instance = Instantiate(prefab, visualRoot);
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
