using UnityEngine;
using UnityEngine.UI;

public class PageSwiper : MonoBehaviour
{
    [Header("Panels Setup")]
    public RectTransform[] panels;

    [Tooltip("Extra space between panels (optional)")]
    public float spacingOffset = 0f;

    public float moveSpeed = 10f;

    [Header("Buttons Setup")]
    public Button[] navButtons;

    private RectTransform parentRect;
    private Vector2 targetPosition;
    private int currentIndex = 2;

    void Awake()
    {
        parentRect = GetComponent<RectTransform>();

        for (int i = 0; i < navButtons.Length; i++)
        {
            int index = i;
            navButtons[i].onClick.AddListener(() => MoveToPanel(index));
        }
    }

    void Start()
    {
        //PositionPanelsBySize();
        targetPosition = GetTargetPos(currentIndex);
    }

    void Update()
    {
        parentRect.anchoredPosition =
            Vector2.Lerp(parentRect.anchoredPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    private void PositionPanelsBySize()
    {
        float currentX = 0f;

        for (int i = 0; i < panels.Length; i++)
        {
            RectTransform p = panels[i];

            // Get width of the panel
            float width = p.rect.width;

            // position panel without touching its size
            p.anchoredPosition = new Vector2(currentX, p.anchoredPosition.y);

            // Move next panel by panel width (and optional spacing)
            currentX += width + spacingOffset;
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
        // Find X of the selected panel
        float x = -panels[index].anchoredPosition.x;
        return new Vector2(x, 0);
    }
}
