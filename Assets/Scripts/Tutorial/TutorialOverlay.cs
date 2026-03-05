using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlay : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);

    [Header("Cell Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private float highlightPadding = 5f;

    [Header("Hand")]
    [SerializeField] private Vector2 handOffset = new Vector2(40f, -40f);
    [SerializeField] private Vector2 handSize = new Vector2(350f, 350f);

    // Overlay canvas (sortingOrder 999)
    private Canvas overlayCanvas;
    private RectTransform overlayCanvasRect;
    private Image darkPanel;
    private Image cellHighlight;

    // Hand lives on a separate canvas (sortingOrder 1001) so it's above everything
    private GameObject handCanvasGo;
    private RectTransform handCanvasRect;
    private RectTransform handRect;

    // Tracks elevated UI target for cleanup
    private Canvas addedCanvas;
    private GraphicRaycaster addedRaycaster;

    // Tap-to-continue for info steps
    private Button tapToContinueButton;

    private GameObject handPrefab;

    public void Init(GameObject handPrefab = null)
    {
        this.handPrefab = handPrefab;
        Debug.Log($"[TutorialOverlay] Init handPrefab = {(handPrefab != null ? handPrefab.name : "NULL")}");
        CreateOverlayCanvas();
        CreateHandCanvas();
        Hide();
    }

    private void CreateOverlayCanvas()
    {
        overlayCanvas = gameObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        overlayCanvasRect = GetComponent<RectTransform>();

        // Full-screen dark panel
        var panelGo = new GameObject("DarkPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        darkPanel = panelGo.GetComponent<Image>();
        darkPanel.color = overlayColor;
        darkPanel.raycastTarget = true;
        var panelRt = darkPanel.rectTransform;
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Cell highlight (rendered above dark panel as later sibling, initially hidden)
        var hlGo = new GameObject("CellHighlight", typeof(RectTransform), typeof(Image));
        hlGo.transform.SetParent(transform, false);
        cellHighlight = hlGo.GetComponent<Image>();
        cellHighlight.color = highlightColor;
        cellHighlight.raycastTarget = false;
        hlGo.SetActive(false);
    }

    private void CreateHandCanvas()
    {
        handCanvasGo = new GameObject("TutorialHandCanvas");
        DontDestroyOnLoad(handCanvasGo);

        var canvas = handCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        var scaler = handCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        handCanvasRect = handCanvasGo.GetComponent<RectTransform>();
    }

    /// <summary>
    /// Highlight a UI element by elevating it above the overlay.
    /// </summary>
    public void EnableTapToContinue()
    {
        if (darkPanel == null) return;
        tapToContinueButton = darkPanel.gameObject.AddComponent<Button>();
        tapToContinueButton.onClick.AddListener(() =>
        {
            TutorialManager.Instance?.NotifyAction(TutorialAction.TapToContinue);
        });
    }

    public void DisableTapToContinue()
    {
        if (tapToContinueButton != null)
        {
            Destroy(tapToContinueButton);
            tapToContinueButton = null;
        }
    }

    public void SetTargetUI(RectTransform target, bool showHand = true)
    {
        if (target == null) { Hide(); return; }

        DisableTapToContinue();
        CleanupPreviousTarget();
        gameObject.SetActive(true);
        if (handCanvasGo != null) handCanvasGo.SetActive(showHand);
        cellHighlight.gameObject.SetActive(false);

        // Elevate the target above the overlay (sortingOrder 1000 > 999)
        addedCanvas = target.gameObject.AddComponent<Canvas>();
        addedCanvas.overrideSorting = true;
        addedCanvas.sortingOrder = 1000;
        addedRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();

        if (showHand)
        {
            // Position hand near the target's center
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 screenCenter = (RectTransformUtility.WorldToScreenPoint(null, corners[0]) +
                                    RectTransformUtility.WorldToScreenPoint(null, corners[2])) * 0.5f;
            PositionHand(screenCenter);
        }
        else if (handRect != null)
        {
            handRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Highlight a world-space object (e.g. grid cell) with a UI highlight above the overlay.
    /// The actual click goes through via physics (OnMouseDown), unblocked by UI.
    /// </summary>
    public void SetTargetWorld(Vector3 worldPos, Vector2 worldSize)
    {
        var cam = Camera.main;
        if (cam == null) { Hide(); return; }

        DisableTapToContinue();
        CleanupPreviousTarget();
        gameObject.SetActive(true);
        if (handCanvasGo != null) handCanvasGo.SetActive(true);

        // Calculate screen rect for the cell
        Vector3 screenCenter = cam.WorldToScreenPoint(worldPos);
        Vector3 screenEdge = cam.WorldToScreenPoint(
            worldPos + new Vector3(worldSize.x * 0.5f, worldSize.y * 0.5f, 0f));
        float hlHalfW = Mathf.Abs(screenEdge.x - screenCenter.x) + highlightPadding;
        float hlHalfH = Mathf.Abs(screenEdge.y - screenCenter.y) + highlightPadding;

        // Position highlight in canvas-local space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvasRect, new Vector2(screenCenter.x, screenCenter.y), null, out var localPos);

        var hlRt = cellHighlight.rectTransform;
        hlRt.anchorMin = new Vector2(0.5f, 0.5f);
        hlRt.anchorMax = new Vector2(0.5f, 0.5f);
        hlRt.pivot = new Vector2(0.5f, 0.5f);
        hlRt.anchoredPosition = localPos;
        hlRt.sizeDelta = new Vector2(hlHalfW * 2f, hlHalfH * 2f);
        cellHighlight.gameObject.SetActive(true);

        PositionHand(new Vector2(screenCenter.x, screenCenter.y));
    }

    public void ShowDarkPanelOnly()
    {
        DisableTapToContinue();
        CleanupPreviousTarget();
        gameObject.SetActive(true);
        cellHighlight.gameObject.SetActive(false);
        if (handCanvasGo != null) handCanvasGo.SetActive(false);
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    public void Hide()
    {
        DisableTapToContinue();
        CleanupPreviousTarget();
        gameObject.SetActive(false);
        if (handCanvasGo != null) handCanvasGo.SetActive(false);
    }

    private void CleanupPreviousTarget()
    {
        if (addedRaycaster != null) { Destroy(addedRaycaster); addedRaycaster = null; }
        if (addedCanvas != null) { Destroy(addedCanvas); addedCanvas = null; }
        if (cellHighlight != null) cellHighlight.gameObject.SetActive(false);
    }

    private void PositionHand(Vector2 screenPos)
    {
        if (handRect == null)
        {
            GameObject hand;

            if (handPrefab != null)
            {
                hand = Instantiate(handPrefab, handCanvasGo.transform, false);
                hand.name = "TutorialHand";
                var img = hand.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
            else
            {
                hand = new GameObject("TutorialHand", typeof(RectTransform), typeof(Image));
                hand.transform.SetParent(handCanvasGo.transform, false);
                var img = hand.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
            }

            handRect = hand.GetComponent<RectTransform>();
            handRect.sizeDelta = handSize;
        }

        handRect.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handCanvasRect, screenPos, null, out var localPos);

        handRect.anchorMin = new Vector2(0.5f, 0.5f);
        handRect.anchorMax = new Vector2(0.5f, 0.5f);
        handRect.anchoredPosition = localPos + handOffset;
    }

    private void OnDestroy()
    {
        CleanupPreviousTarget();
        if (handCanvasGo != null) Destroy(handCanvasGo);
    }
}
