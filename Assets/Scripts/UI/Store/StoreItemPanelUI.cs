using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemPanelUI : MonoBehaviour
{
    public static StoreItemPanelUI Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amount;
    [SerializeField] private Image costIconImage;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject panelRoot;

    [SerializeField] private float maxIconSize = 100f;


    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    private StoreItemDefinition _currentItem;

    private void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Close);
    }

    public void Show(StoreItemDefinition item, RectTransform rect)
    {
        _currentItem = item;

        titleText.text = item.title;
        iconImage.sprite = item.icon;
        NormalizeIconSize(iconImage);
        amount.text = $"x{item.goldAmount.ToString()}";

        int price = item.costType == CostType.Gems
            ? item.priceInGems
            : item.priceInGold;

        if (item.discountPercent > 0)
        {
            price = Mathf.RoundToInt(price * (1f - item.discountPercent / 100f));
        }
        var goldSprite = StyleManager.instance.goldSprite;
        var gemSprite = StyleManager.instance.gemSprite;

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
            priceText.color = CanAfford(item) ? Color.white : Color.red;
        }

        costIconImage.sprite = item.costType == CostType.Gems ? gemSprite : goldSprite;

        if (item.category == StoreCategory.BuyChestsWithGold || item.category == StoreCategory.BuyPartWithGold)
        {
            amount.gameObject.SetActive(false);
            iconImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, 0f);

        }
        else
        {
            amount.gameObject.SetActive(true);
            iconImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, 20f);

        }




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
        if (_currentItem == null) return;

        StoreManager.Instance.TryBuy(_currentItem);
        Close();
    }

    public void Close()
    {
        if (popupAnimator == null)
            return;
        panelRoot.SetActive(false);
        _currentItem = null;
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
}
