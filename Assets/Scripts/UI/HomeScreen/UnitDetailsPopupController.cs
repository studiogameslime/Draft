using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitDetailsPopupController : MonoBehaviour
{
    public static UnitDetailsPopupController Instance;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Popup Layout")]
    [SerializeField] private RectTransform windowRect;         
    [SerializeField] private Canvas rootCanvas;                 
    [SerializeField] private CanvasGroup overlayCanvasGroup;    

    [Header("Open Animation")]
    [SerializeField] private float openDuration = 0.28f;
    [SerializeField] private Vector3 startScale = new Vector3(0.15f, 0.15f, 1f);

    [Header("Unit UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text targetPriorityText;
    [SerializeField] private TMP_Text SoulsCostText;
    [SerializeField] private TMP_Text TokensLeftText;
    [SerializeField] private Image rarityTagImage;
    [SerializeField] private Image unitClassImage;

    [Header("Rarity Sprites")]
    [SerializeField] private Sprite commonRaritySprite;
    [SerializeField] private Sprite rareRaritySprite;
    [SerializeField] private Sprite epicRaritySprite;
    [SerializeField] private Sprite legendaryRaritySprite;

    [Header("Unit Classes Sprites")]
    [SerializeField] private Sprite meleeClassSprite;
    [SerializeField] private Sprite rangedClassSprite;
    [SerializeField] private Sprite mageClassSprite;
    [SerializeField] private Sprite supportClassSprite;

    [Header("Tabs")]
    [SerializeField] private GameObject statsTab;
    [SerializeField] private GameObject partsTab;
    [SerializeField] private Button statsTabButton;
    [SerializeField] private Button partsTabButton;

    private UnitDefinition _unit;
    private UnitsDeckManager _deckManager;
    private Coroutine _openRoutine;

    private Vector2 _finalAnchoredPos;
    private Vector3 _finalScale;

    [SerializeField] private float closeDuration = 0.22f;
    [SerializeField] private Vector3 closeEndScale = new Vector3(0.15f, 0.15f, 1f);

    private RectTransform _lastOpenedCardRect;
    private Coroutine _closeRoutine;
    [SerializeField] private UnitPartsTabController partsTabController;


    void Awake()
    {
        root.SetActive(true);
        Instance = this;

        _finalAnchoredPos = windowRect.anchoredPosition;
        _finalScale = Vector3.one;


        closeButton.onClick.AddListener(Close);
        backgroundCloseButton.onClick.AddListener(Close);
        
    }

    private void Start()
    {
        root.SetActive(false);
    }

    public void OpenFromCard(UnitDefinition unit, bool isDeckSlot, UnitsDeckManager deckManager, RectTransform cardRect)
    {
        _unit = unit;
        _deckManager = deckManager;
        _lastOpenedCardRect = cardRect;
        FillData();

        if (_openRoutine != null) StopCoroutine(_openRoutine);

        root.SetActive(true);
        OpenStatsTab();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = true;
        }

        windowRect.anchoredPosition = GetAnchoredPositionInCanvas(cardRect, windowRect);
        windowRect.localScale = startScale;

        _openRoutine = StartCoroutine(OpenAnimRoutine());
    }

    public void Close()
    {
        if (!root.activeSelf) return;

        if (_openRoutine != null) StopCoroutine(_openRoutine);
        if (_closeRoutine != null) StopCoroutine(_closeRoutine);

        OpenStatsTab();
        if (_lastOpenedCardRect == null || !_lastOpenedCardRect.gameObject.activeInHierarchy)
        {
            root.SetActive(false);
            return;
        }

        _closeRoutine = StartCoroutine(CloseAnimRoutine(_lastOpenedCardRect));
    }

    private IEnumerator CloseAnimRoutine(RectTransform cardRect)
    {
        // לחסום אינטראקציות בזמן אנימציה
        closeButton.interactable = false;
        backgroundCloseButton.interactable = false;

        float t = 0f;

        Vector2 startPos = windowRect.anchoredPosition;
        Vector2 endPos = GetAnchoredPositionInCanvas(cardRect, windowRect);

        Vector3 sScale = windowRect.localScale;
        Vector3 eScale = closeEndScale;

        float startOverlay = overlayCanvasGroup ? overlayCanvasGroup.alpha : 1f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, closeDuration);
            float eased = EaseOutCubic(Mathf.Clamp01(t));

            windowRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            windowRect.localScale = Vector3.Lerp(sScale, eScale, eased);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = Mathf.Lerp(startOverlay, 0f, eased);

            yield return null;
        }

        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = 0f;

        // להחזיר אינטראקציות לפעם הבאה
        closeButton.interactable = true;
        backgroundCloseButton.interactable = true;

        root.SetActive(false);
        _closeRoutine = null;
    }


    private IEnumerator OpenAnimRoutine()
    {
        float t = 0f;

        Vector2 startPos = windowRect.anchoredPosition;
        Vector2 endPos = _finalAnchoredPos;

        Vector3 sScale = windowRect.localScale;
        Vector3 eScale = _finalScale;

        float startOverlay = overlayCanvasGroup ? overlayCanvasGroup.alpha : 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, openDuration);
            float eased = EaseOutCubic(Mathf.Clamp01(t));

            windowRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            windowRect.localScale = Vector3.Lerp(sScale, eScale, eased);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = Mathf.Lerp(startOverlay, 1f, eased);

            yield return null;
        }

        windowRect.anchoredPosition = endPos;
        windowRect.localScale = eScale;

        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = 1f;

        _openRoutine = null;
    }

    private Vector2 GetAnchoredPositionInCanvas(RectTransform fromRect, RectTransform targetRectInSameCanvas)
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        Camera cam = null;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        Vector3 worldCenter = fromRect.TransformPoint(fromRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        RectTransform parentRect = targetRectInSameCanvas.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint);

        return localPoint;
    }

    private float EaseOutCubic(float x)
    {
        float p = 1f - x;
        return 1f - (p * p * p);
    }

    private void FillData()
    {
        if (partsTabController != null && _unit != null)
            partsTabController.Show(_unit);


        FillIconImageOrAnimator();
        nameText.text = _unit.displayName;
        descriptionText.text = _unit.description;
        hpText.text = _unit.maxHealth.ToString();
        dmgText.text = _unit.damage.ToString();
        atkSpeedText.text = _unit.attackCooldown.ToString();
        rangeText.text = _unit.attackRange.ToString();
        speedText.text = _unit.moveSpeed.ToString();
        targetPriorityText.text = _unit.targetPriorityClass.ToString();
        SoulsCostText.text = _unit.soulCost.ToString();
        TokensLeftText.text = "8"; //Replace later with real data
        rarityTagImage.sprite = GetRaritySpriteByType(_unit.rarity);
        unitClassImage.sprite = GetUnitClassSpriteByType(_unit.unitClass);
    }

    private void FillIconImageOrAnimator()
    {
        icon.sprite = _unit.icon;

        if (_unit.animatorController)
        {
            iconAnimator.enabled = true;
            iconAnimator.runtimeAnimatorController = _unit.animatorController;
        }
        else
        {
            iconAnimator.enabled = false;
            iconAnimator.runtimeAnimatorController = null;
        }

        icon.SetNativeSize();
    }

    private Sprite GetRaritySpriteByType(UnitRarity type)
    {
        switch (type)
        {
            case UnitRarity.Common: return commonRaritySprite;
            case UnitRarity.Rare: return rareRaritySprite;
            case UnitRarity.Epic: return epicRaritySprite;
            case UnitRarity.Legendary: return legendaryRaritySprite;
            default: return commonRaritySprite;
        }
    }

    private Sprite GetUnitClassSpriteByType(UnitClass type)
    {
        switch (type)
        {
            case UnitClass.Melee: return meleeClassSprite;
            case UnitClass.Ranged: return rangedClassSprite;
            case UnitClass.Mage: return mageClassSprite;
            default: return meleeClassSprite;
        }
    }

    public void OpenStatsTab()
    {
        statsTab.SetActive(true);
        partsTab.SetActive(false);
        statsTabButton.interactable = false;
        partsTabButton.interactable = true;
    }

    public void OpenPartsTab()
    {
        statsTab.SetActive(false);
        partsTab.SetActive(true);
        statsTabButton.interactable = true;
        partsTabButton.interactable = false;

        if (partsTabController != null && _unit != null)
            partsTabController.Refresh();
    }
}
