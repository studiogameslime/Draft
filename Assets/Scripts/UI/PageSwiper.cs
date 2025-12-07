using UnityEngine;
using UnityEngine.UI;

public class PageSwiper : MonoBehaviour
{
    [Header("Panels Setup")]
    [Tooltip("Drag your 5 panels here IN ORDER (left ? right)")]
    public RectTransform[] panels;         

    [Tooltip("Distance between panels (depends on your screen width)")]
    public float panelSpacing = 1920f;     

    [Tooltip("Smooth movement speed")]
    public float moveSpeed = 10f;           

    [Header("Buttons Setup")]
    [Tooltip("Drag your 5 navigation buttons here (left ? right)")]
    public Button[] navButtons;             

    private RectTransform parentRect;        
    private Vector2 targetPosition;          
    private int currentIndex = 0;           

    void Awake()
    {
        parentRect = GetComponent<RectTransform>();

        // Assign each button to move to its matching panel
        for (int i = 0; i < navButtons.Length; i++)
        {
            int index = i;  // VERY IMPORTANT! closure fix
            navButtons[i].onClick.AddListener(() => MoveToPanel(index));
        }
    }

    void Start()
    {
        PositionPanelsCorrectly();
        targetPosition = GetTargetPos(currentIndex);
    }

    void Update()
    {
        parentRect.anchoredPosition =
            Vector2.Lerp(parentRect.anchoredPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    // Places all panels horizontally without changing their size/scale/pivots
    private void PositionPanelsCorrectly()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            RectTransform p = panels[i];

            // save original layout values
            Vector2 originalSize = p.sizeDelta;
            Vector3 originalScale = p.localScale;
            Vector2 originalAnchorMin = p.anchorMin;
            Vector2 originalAnchorMax = p.anchorMax;
            Vector2 originalPivot = p.pivot;

            // only move horizontally
            p.anchoredPosition = new Vector2(i * panelSpacing, p.anchoredPosition.y);

            // restore layout values
            p.sizeDelta = originalSize;
            p.localScale = originalScale;
            p.anchorMin = originalAnchorMin;
            p.anchorMax = originalAnchorMax;
            p.pivot = originalPivot;
        }
    }

    public void MoveToPanel(int index)
    {
        if (index < 0 || index >= panels.Length)
            return;

        currentIndex = index;
        targetPosition = GetTargetPos(index);
    }

    private Vector2 GetTargetPos(int index)
    {
        return new Vector2(-index * panelSpacing, 0);
    }
}
