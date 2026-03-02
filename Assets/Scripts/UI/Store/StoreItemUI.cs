using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image costIconImage;
    [SerializeField] private GameObject costIconImageContainer;
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite gemSprite;
    [SerializeField] private Button buyButton;

    [SerializeField] private float maxIconSize = 100f;


    [Header("Sale UI")]
    [SerializeField] private GameObject saleBadgeRoot;
    [SerializeField] private TMP_Text salePercentText;

    private StoreItemDefinition definition;
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        if (definition == null || !definition.isDailyFree)
            return;

        var save = GameData.Instance.Save;
        if (DailyResetUtil.IsReady(save.nextDailyFreeGoldUtcTicks))
        {
            RectTransform rt = timerText.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            timerText.text = "FREE";
            buyButton.interactable = true;
        }
        else
        {
            UpdateDaily();
            buyButton.interactable = false;

            RectTransform rt = timerText.GetComponent<RectTransform>();
            Vector2 offsetMin = rt.offsetMin;
            offsetMin.x = 0f;          // LEFT
            rt.offsetMin = offsetMin;

            timerText.fontSizeMax = 40;

        }
    }

    public void Setup(StoreItemDefinition def)
    {
        definition = def;

        titleText.text = def.title;
        amountText.text = def.category == StoreCategory.BuyScrollsWithGold
            ? $"x{def.scrollAmount}"
            : $"x{def.goldAmount}";
        iconImage.sprite = def.icon;
        NormalizeIconSize(iconImage);


        if (def.isDailyFree)
        {
            bool canClaim = StoreManager.Instance != null && StoreManager.Instance.CanClaimDailyFree(def);

            Destroy(costIconImageContainer);
            saleBadgeRoot.SetActive(false);

            if (canClaim)
            {
                RectTransform rect = priceText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                priceText.alignment = TextAlignmentOptions.Center;
                priceText.text = "FREE";
                buyButton.interactable = true;
            }
            else
            {
                RectTransform rect = priceText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                priceText.alignment = TextAlignmentOptions.Center;
                var t = StoreManager.Instance.GetTimeUntilDailyFree(def);
                priceText.text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
                buyButton.interactable = false;
            }

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
            return;
        }

        int finalPrice = def.GetFinalPrice();

        costIconImage.sprite = def.costType == CostType.Gems ? gemSprite : goldSprite;
        if (def.discountPercent > 0)
        {
            saleBadgeRoot.SetActive(true);
            salePercentText.text = $"{def.discountPercent}%";
        }
        else
        {
            saleBadgeRoot.SetActive(false);
        }
        if (def.category == StoreCategory.BuyChestsWithGold || def.category == StoreCategory.BuyPartWithGold)
        {
            amountText.gameObject.SetActive(false);
            iconImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, 0f);


        }

        priceText.text = finalPrice.ToString();
        priceText.color = CanAfford(def) ? Color.white : Color.red;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        var rectTransform = GetComponent<RectTransform>();

        if (definition.category == StoreCategory.BuyChestsWithGold)
        {
            StoreItemChestPanel.Instance.Show(definition, rectTransform);
        }
        else
        {
            StoreItemPanelUI.Instance.Show(definition, rectTransform);
        }

    }

    private void UpdateDaily()
    {
        DateTime nextUtc = DailyResetUtil.GetNextDailyResetUtc(DateTime.UtcNow);
        TimeSpan left = nextUtc - DateTime.UtcNow;
        timerText.text = $"{left.Hours:D2}:{left.Minutes:D2}:{left.Seconds:D2}";
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
