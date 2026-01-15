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
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite gemSprite;
    [SerializeField] private Button buyButton;

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
            timerText.text = "FREE";
            buyButton.interactable = true;
        }
        else
        {
            TimeSpan left = new TimeSpan(save.nextDailyFreeGoldUtcTicks - DateTime.UtcNow.Ticks);
            timerText.text = $"{left.Hours:D2}:{left.Minutes:D2}:{left.Seconds:D2}";
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
        amountText.text = def.goldAmount.ToString();
        iconImage.sprite = def.icon;

        if (def.isDailyFree)
        {
            bool canClaim = StoreManager.Instance != null && StoreManager.Instance.CanClaimDailyFree(def);

            costIconImage.gameObject.SetActive(false);
            saleBadgeRoot.SetActive(false);

            if (canClaim)
            {
                priceText.text = "FREE";
                buyButton.interactable = true;
            }
            else
            {
                var t = StoreManager.Instance.GetTimeUntilDailyFree(def);
                priceText.text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
                buyButton.interactable = false;
            }

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
            return;
        }

        int finalPrice = def.costType == CostType.Gems ? def.priceInGems: def.priceInGold;
        costIconImage.sprite = def.costType == CostType.Gems ? gemSprite: goldSprite;
        if (def.discountPercent > 0)
        {
            finalPrice = Mathf.RoundToInt(
                finalPrice * (1f - def.discountPercent / 100f)
            );

            saleBadgeRoot.SetActive(true);
            salePercentText.text = $"{def.discountPercent}%";
        }
        else
        {
            saleBadgeRoot.SetActive(false);
        }
        if (def.category == StoreCategory.BuyChestsWithGold) 
        {
            amountText.gameObject.SetActive(false);
            iconImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, 0f);


        }

        priceText.text = finalPrice.ToString();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        StoreManager.Instance.TryBuy(definition);
    }
}
