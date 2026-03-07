using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public enum HomeScreenPage
{
    Store = 0,
    Collection = 1,
    Battle = 2,
    Mastery = 3
}

/// <summary>
/// Horizontal scroll snapping between pages.
/// Bottom buttons jump to pages.
/// Active bottom button background becomes wider and taller.
/// </summary>
public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float snapSpeed = 10f;

    [Header("Bottom Buttons")]
    [Tooltip("Buttons at the bottom. Index must match page index.")]
    [SerializeField] private List<Button> bottomButtons;

    [Tooltip("Background rects that will change size. Same order as bottomButtons.")]
    [SerializeField] private List<RectTransform> buttonBackgrounds;

    [Header("Active Tab Label")]
    [SerializeField] private string[] tabLabels = { "Shop", "Units", "Battle", "Mastery", "Events" };
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private float activeIconYOffset = 15f;
    [SerializeField] private float labelYOffset = 8f;
    [SerializeField] private float activeTabExtraWidth = 40f;

    private int pageCount = 0;
    private bool isDragging = false;
    private float targetPos = 0f;   // horizontalNormalizedPosition target
    private int currentPageIndex = 0;

    // original size of each background
    private Vector2[] baseSize;

    // cached icon transforms and their original positions
    private RectTransform[] iconRects;
    private Vector2[] iconBasePos;
    private Vector2[] iconBaseSize;
    private TMP_Text[] tabLabelTexts;

    // tab container LayoutElements for active width control
    private LayoutElement[] tabLayoutElements;

    private void Awake()
    {
        if (scrollRect == null)
        {
            Debug.LogError("ScrollSnap: ScrollRect is not assigned.");
            enabled = false;
            return;
        }

        if (bottomButtons == null || bottomButtons.Count == 0)
        {
            Debug.LogError("ScrollSnap: bottomButtons list is empty.");
            enabled = false;
            return;
        }

        pageCount = bottomButtons.Count;

        // Make nav bar full width: find the HorizontalLayoutGroup on the buttons' grandparent
        // Structure: BottomNavigationBar (HLG) > ShopButtonTab > Background (button)
        if (bottomButtons[0] != null)
        {
            // Button is on the background, parent is the tab, grandparent is BottomNavigationBar
            var tabParent = bottomButtons[0].transform.parent;
            var navBar = tabParent != null ? tabParent.parent : null;
            var hlg = navBar != null ? navBar.GetComponent<HorizontalLayoutGroup>() : null;
            if (hlg != null)
            {
                hlg.childControlWidth = true;
                hlg.childForceExpandWidth = true;
            }
        }

        // If buttonBackgrounds list is missing or wrong size, try auto fill
        if (buttonBackgrounds == null || buttonBackgrounds.Count != pageCount)
        {
            buttonBackgrounds = new List<RectTransform>(pageCount);
            for (int i = 0; i < pageCount; i++)
            {
                if (bottomButtons[i] != null)
                    buttonBackgrounds.Add(bottomButtons[i].GetComponent<RectTransform>());
                else
                    buttonBackgrounds.Add(null);
            }
        }

        baseSize = new Vector2[pageCount];
        iconRects = new RectTransform[pageCount];
        iconBasePos = new Vector2[pageCount];
        iconBaseSize = new Vector2[pageCount];
        tabLabelTexts = new TMP_Text[pageCount];
        tabLayoutElements = new LayoutElement[pageCount];

        // Register button listeners and cache base sizes
        for (int i = 0; i < pageCount; i++)
        {
            int index = i;
            if (bottomButtons[i] != null)
                bottomButtons[i].onClick.AddListener(() => GoToPage(index));

            RectTransform rect = buttonBackgrounds[i];
            if (rect != null)
            {
                baseSize[i] = rect.sizeDelta;

                // Stretch background to fill its parent tab container (remove gaps)
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Add LayoutElement to the tab container (parent) for width control
                var tabContainer = rect.parent;
                if (tabContainer != null)
                {
                    var le = tabContainer.GetComponent<LayoutElement>();
                    if (le == null)
                        le = tabContainer.gameObject.AddComponent<LayoutElement>();
                    le.flexibleWidth = 1f;
                    tabLayoutElements[i] = le;
                }

                // Find the icon: first child inside the background
                var iconImg = rect.childCount > 0 ? rect.GetChild(0).GetComponent<RectTransform>() : null;
                if (iconImg != null)
                {
                    iconRects[i] = iconImg;
                    iconBasePos[i] = iconImg.anchoredPosition;
                    iconBaseSize[i] = iconImg.sizeDelta;
                }
            }
        }
    }

    private void Start()
    {
        // Start on page 0
        float step = 1f / Mathf.Max(1, pageCount - 1);
        currentPageIndex = 2;
        targetPos = currentPageIndex * step;
        scrollRect.horizontalNormalizedPosition = targetPos;

        UpdateButtonsUI(currentPageIndex);
    }

    private void Update()
    {
        if (!isDragging)
        {
            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(scrollRect.horizontalNormalizedPosition,
                           targetPos,
                           Time.deltaTime * snapSpeed);
        }
    }

    // Drag handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            return;
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        float pos = scrollRect.horizontalNormalizedPosition;
        float step = 1f / Mathf.Max(1, pageCount - 1);

        int pageIndex = Mathf.RoundToInt(pos / step);
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);

        GoToPage(pageIndex);
    }

    // Go to page
    public void GoToPage(int index)
    {
        index = Mathf.Clamp(index, 0, pageCount - 1);

        if (!IsPageAllowed(index))
            return;

        if (index != currentPageIndex)
            SoundManager.Instance?.PlayPageSwipe();

        float step = 1f / Mathf.Max(1, pageCount - 1);
        currentPageIndex = index;
        targetPos = index * step;

        UpdateButtonsUI(index);
        TutorialManager.Instance?.NotifyAction(TutorialAction.PageChanged, index);
    }

    private bool IsPageAllowed(int index)
    {
        // During tutorial, only allow the target page
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive
            && !TutorialManager.Instance.IsPageAllowed(index))
            return false;

        var page = (HomeScreenPage)index;

        switch (page)
        {
            case HomeScreenPage.Mastery:
                if (!FeatureUnlockManager.IsUnlocked(GameFeature.Mastery))
                {
                    if (ToastManager.Instance != null)
                        ToastManager.Instance.Show("Unlock at Level 3!");
                    return false;
                }
                break;
        }

        return true;
    }

    public Button GetBottomButton(int index)
    {
        if (bottomButtons == null || index < 0 || index >= bottomButtons.Count)
            return null;
        return bottomButtons[index];
    }

    private void UpdateButtonsUI(int activeIndex)
    {
        for (int i = 0; i < pageCount; i++)
        {
            RectTransform rect = buttonBackgrounds[i];
            if (rect == null)
                continue;

            bool isActive = (i == activeIndex);

            // Active tab gets more width via LayoutElement
            if (tabLayoutElements[i] != null)
                tabLayoutElements[i].flexibleWidth = isActive ? 1.5f : 1f;

            // Icon: shift up for active tab to make room for label
            if (iconRects[i] != null)
            {
                iconRects[i].sizeDelta = iconBaseSize[i];
                iconRects[i].anchoredPosition = isActive
                    ? new Vector2(iconBasePos[i].x, iconBasePos[i].y + activeIconYOffset)
                    : iconBasePos[i];
            }

            // Label text: show only on active tab
            if (isActive)
            {
                if (tabLabelTexts[i] == null)
                    tabLabelTexts[i] = CreateTabLabel(rect, i);

                if (tabLabelTexts[i] != null)
                {
                    tabLabelTexts[i].gameObject.SetActive(true);
                    string label = (tabLabels != null && i < tabLabels.Length) ? tabLabels[i] : "";
                    tabLabelTexts[i].text = label;
                }
            }
            else
            {
                if (tabLabelTexts[i] != null)
                    tabLabelTexts[i].gameObject.SetActive(false);
            }
        }
    }

    private TMP_Text CreateTabLabel(RectTransform parent, int index)
    {
        var go = new GameObject("TabLabel");
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, labelYOffset);
        rt.sizeDelta = new Vector2(200f, 40f);

        if (labelFont != null)
            tmp.font = labelFont;
        tmp.fontSizeMax = 28f;
        tmp.fontSizeMin = 12f;
        tmp.enableAutoSizing = true;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return tmp;
    }
}
