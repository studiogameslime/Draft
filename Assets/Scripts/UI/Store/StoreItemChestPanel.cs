using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemChestPanel : MonoBehaviour
{
    public static StoreItemChestPanel Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text goldRange;
    [SerializeField] private TMP_Text gemsRange;
    [SerializeField] private TMP_Text wofDropChanceText;
    [SerializeField] private Image costIconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject panelRoot;
    
    [SerializeField] private float maxIconSize = 100f;

    [Header("Part Drop Chances")]
    [SerializeField] private Transform partDropChancesContainer;
    [SerializeField] private ChestPartDropChance partDropChanceRowPrefab;



    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    private StoreItemDefinition definition;

    private void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Close);
    }

    public void Show(StoreItemDefinition item, RectTransform rect)
    {
        definition = item;

        titleText.text = item.title;
        iconImage.sprite = item.icon;
        NormalizeIconSize(iconImage);
        goldRange.text = $"{item.chestReward.goldRange.min.ToString()} - {item.chestReward.goldRange.max.ToString()}";
        gemsRange.text = $"{item.chestReward.diamondsRange.min.ToString()} - {item.chestReward.diamondsRange.max.ToString()}";
        wofDropChanceText.text = $"{item.chestReward.wheelOfFortuneChance * 100}%";
        BuildPartDropChances(item);


        int price = item.costType == CostType.Gems
            ? item.priceInGems
            : item.priceInGold;

        if (item.discountPercent > 0)
        {
            price = Mathf.RoundToInt(price * (1f - item.discountPercent / 100f));
        }
        
        if (item.isDailyFree)
        {
            priceText.text = "Free";
            priceText.alignment = TextAlignmentOptions.Center;
            RectTransform rt = priceText.rectTransform;
            rt.sizeDelta = new Vector2(150f, rt.sizeDelta.y);
            costIconImage.gameObject.SetActive(false);

        }
        else
        {
            priceText.text = price.ToString();
            priceText.alignment = TextAlignmentOptions.Left;
            RectTransform rt = priceText.rectTransform;
            rt.sizeDelta = new Vector2(100f, rt.sizeDelta.y);
            costIconImage.gameObject.SetActive(true);

        }

        var goldSprite = StyleManager.instance.goldSprite;
        var gemSprite = StyleManager.instance.gemSprite;
        costIconImage.sprite = item.costType == CostType.Gems ? gemSprite : goldSprite;

       confirmButton.interactable = CanAfford(item);

        if (popupAnimator == null)
        {
            Debug.LogWarning("MissionsScreen: PopupAnimator is not assigned.");
            return;
        }
        panelRoot.SetActive(true);
        popupAnimator.OpenFromRect(rect);
    }

    private bool CanAfford(StoreItemDefinition item)
    {
        var wallet = PlayerCurrencyWallet.Instance;
        if (wallet == null) return false;

        int price = item.GetFinalPrice();

        return item.costType == CostType.Gems
            ? wallet.Gems >= price
            : wallet.Gold >= price;

    }

    private void OnConfirm()
    {
        if (definition == null) return;

        StoreManager.Instance.TryBuy(definition);
        Close();
    }

    public void Close()
    {
        if (popupAnimator == null)
            return;
        panelRoot.SetActive(false);
        definition = null;
        popupAnimator.Close();
    }

    private void NormalizeIconSize(Image image)
    {
        if (image == null || image.sprite == null)
            return;

        RectTransform rt = image.rectTransform;

        Vector2 spriteSize = image.sprite.rect.size;
        float maxSide = Mathf.Max(spriteSize.x, spriteSize.y);

        if (maxSide <= 0f)
            return;

        float scale = maxIconSize / maxSide;
        scale = Mathf.Min(scale, 1f); // never upscale, only downscale

        rt.sizeDelta = spriteSize * scale;
    }

    private void ClearPartDropChances()
    {
        for (int i = partDropChancesContainer.childCount - 1; i >= 0; i--)
            Destroy(partDropChancesContainer.GetChild(i).gameObject);
    }

    private void BuildPartDropChances(StoreItemDefinition item)
    {
        if (item.chestReward == null ||
            item.chestReward.partRarityWeights == null ||
            partDropChanceRowPrefab == null)
            return;

        ClearPartDropChances();

        foreach (var entry in item.chestReward.partRarityWeights)
        {
            var row = Instantiate(partDropChanceRowPrefab, partDropChancesContainer);
            row.Bind(entry.rarity, entry.weight);
        }
    }



}
