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

    public void Show(StoreItemDefinition item)
    {
        _currentItem = item;

        titleText.text = item.title;
        iconImage.sprite = item.icon;
        amount.text = $"x{item.goldAmount.ToString()}";

        int price = item.costType == CostType.Gems
            ? item.priceInGems
            : item.priceInGold;

        if (item.discountPercent > 0)
        {
            price = Mathf.RoundToInt(price * (1f - item.discountPercent / 100f));
        }
        var goldSprite = SpriteManager.instance.goldSprite;
        var gemSprite = SpriteManager.instance.gemSprite;
        priceText.text = price.ToString();
        costIconImage.sprite = item.costType == CostType.Gems ? gemSprite : goldSprite;

        if (item.category == StoreCategory.BuyChestsWithGold)
        {
            amount.gameObject.SetActive(false);
            iconImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetComponent<RectTransform>().anchoredPosition.x, 0f);

        }

        confirmButton.interactable = CanAfford(item);

        //if (popupAnimator == null)
        //{
        //    Debug.LogWarning("MissionsScreen: PopupAnimator is not assigned.");
        //    return;
        //}
        panelRoot.SetActive(true);
        //popupAnimator.OpenFromRect();
    }

    private bool CanAfford(StoreItemDefinition item)
    {
        var wallet = PlayerCurrencyWallet.Instance;
        if (wallet == null) return false;

        return item.costType == CostType.Gems
            ? wallet.Gems >= item.priceInGems
            : wallet.Gold >= item.priceInGold;
    }

    private void OnConfirm()
    {
        if (_currentItem == null) return;

        StoreManager.Instance.TryBuy(_currentItem);
        Close();
    }

    private void Close()
    {
        //if (popupAnimator == null)
        //    return;
        panelRoot.SetActive(false);
        _currentItem = null;
        //popupAnimator.Close();
    }
}
