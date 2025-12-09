using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect;
    public int pageCount = 5;        
    public float snapSpeed = 10f;

    private bool isDragging = false;
    private float targetPos = 0f;   

    void Start()
    {
        targetPos = 0f;
        scrollRect.horizontalNormalizedPosition = targetPos;
    }

    void Update()
    {
        if (!isDragging)
        {
            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetPos, Time.deltaTime * snapSpeed);
        }
    }

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

        targetPos = pageIndex * step;
    }

    public void GoToPage(int index)
    {
        index = Mathf.Clamp(index, 0, pageCount - 1);
        float step = 1f / Mathf.Max(1, pageCount - 1);
        targetPos = index * step;
    }
}
