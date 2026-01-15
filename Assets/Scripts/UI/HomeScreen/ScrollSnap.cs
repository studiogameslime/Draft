using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    [Tooltip("Extra width in pixels for the active button background.")]
    [SerializeField] private float activeExtraWidth = 20f;

    [Tooltip("Extra height in pixels for the active button background.")]
    [SerializeField] private float activeExtraHeight = 20f;

    private int pageCount = 0;
    private bool isDragging = false;
    private float targetPos = 0f;   // horizontalNormalizedPosition target
    private int currentPageIndex = 0;

    // original size of each background
    private Vector2[] baseSize;

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

        // Register button listeners and cache base sizes
        for (int i = 0; i < pageCount; i++)
        {
            int index = i;
            if (bottomButtons[i] != null)
                bottomButtons[i].onClick.AddListener(() => GoToPage(index));

            RectTransform rect = buttonBackgrounds[i];
            if (rect != null)
                baseSize[i] = rect.sizeDelta;
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

        float step = 1f / Mathf.Max(1, pageCount - 1);
        currentPageIndex = index;
        targetPos = index * step;

        UpdateButtonsUI(index);
    }

    // Make only active background bigger (width and height)
    private void UpdateButtonsUI(int activeIndex)
    {
        for (int i = 0; i < pageCount; i++)
        {
            RectTransform rect = buttonBackgrounds[i];
            if (rect == null)
                continue;

            Vector2 size = baseSize[i];

            if (i == activeIndex)
            {
                rect.sizeDelta = new Vector2(
                    size.x + activeExtraWidth,
                    size.y + activeExtraHeight
                );
            }
            else
            {
                rect.sizeDelta = size;
            }
        }
    }
}
