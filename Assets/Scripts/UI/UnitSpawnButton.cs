using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitSpawnButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI refs")]
    public Image iconImage;             // Unit icon image
    public TMP_Text unitName;           // Unit display name
    public TMP_Text UnitToSpawn;        // "+X" amount to spawn
    public TMP_Text UnitSoulCost;       // Cost text (souls / elixir)
    public CanvasGroup canvasGroup;     // Used to hide/show card during drag

    [Header("Drag settings")]
    [Tooltip("How far (in screen pixels) the card must move before switching to world-unit dragging")]
    public float switchToWorldDistance = 40f;

    [Header("Placement rules")]
    [Tooltip("Layer mask for all units on the board (used to prevent overlapping placement)")]
    public LayerMask unitsLayerMask;
    [Tooltip("Minimum distance from other units when placing a new one")]
    public float minDistanceFromOtherUnits = 0.5f;

    [HideInInspector]
    public BattleManager battleManager;  // Assigned from UnitSelectionUI (or manually)

    // Data
    private UnitDefinition data;
    private UnitSelectionUI selectionUI;

    // Layout / transform state
    private RectTransform _rt;
    private Transform _originalParent;
    private Vector2 _originalAnchoredPos;
    private int _originalSiblingIndex;
    private RectTransform _placeholderRT;   // placeholder inside the GridLayoutGroup

    // Drag state
    private Vector2 _dragStartScreenPos;
    private bool _usingCardGraphic = false; // True while dragging the card itself (UI)
    private bool _spawnedWorldUnit = false; // True once we switched to world-unit dragging

    // Dragged world unit
    private GameObject _dragUnitInstance;
    private Rigidbody2D _dragUnitRb;
    private Collider2D[] _dragUnitColliders;

    private Camera _cam;

    // ======================================================================
    // LIFECYCLE
    // ======================================================================
    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _originalParent = _rt.parent;
        _originalAnchoredPos = _rt.anchoredPosition;
        _originalSiblingIndex = _rt.GetSiblingIndex();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        _cam = Camera.main;
    }

    // Initialize card visuals & references
    public void Init(UnitDefinition def, UnitSelectionUI ui)
    {
        data = def;
        selectionUI = ui;
        battleManager = ui != null ? ui.battleManager : null;

        if (unitName != null)
            unitName.text = def.displayName;

        if (UnitToSpawn != null)
            UnitToSpawn.text = "+" + def.spawnCount;

        if (UnitSoulCost != null)
            UnitSoulCost.text = def.soulCost.ToString();

        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            iconImage.transform.localScale = Vector3.one * def.iconScale;
            iconImage.SetNativeSize();
        }
    }

    public void OnClick()
    {
        // Not used in the drag-based flow.
    }

    // ======================================================================
    // DRAG HANDLERS
    // ======================================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (data == null || data.prefab == null || battleManager == null)
            return;

        if (battleManager.IsBattleRunning)
            return;

        _dragStartScreenPos = eventData.position;
        _originalAnchoredPos = _rt.anchoredPosition;
        _originalSiblingIndex = _rt.GetSiblingIndex();

        _usingCardGraphic = true;
        _spawnedWorldUnit = false;
        _dragUnitInstance = null;
        _dragUnitRb = null;
        _dragUnitColliders = null;

        CreatePlaceholder();

        // Detach from layout parent to freely move the card under the pointer
        _rt.SetParent(transform.root, true); // keep world position
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;

        // Turn on highlight for all drop zones
        DropZone.SetAllHighlights(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (data == null || battleManager == null)
            return;

        // Phase 1: dragging the UI card itself
        if (_usingCardGraphic)
        {
            _rt.position = eventData.position;

            float dist = Vector2.Distance(eventData.position, _dragStartScreenPos);
            if (dist >= switchToWorldDistance)
            {
                // Switch to dragging a world-space unit prefab instead of the card
                SwitchToWorldDrag(eventData);
            }
        }
        // Phase 2: already dragging a world unit instance
        else if (_spawnedWorldUnit && _dragUnitInstance != null)
        {
            Vector3 worldPos = ScreenToWorld(eventData.position);
            _dragUnitInstance.transform.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Disable highlight for all drop zones
        DropZone.SetAllHighlights(false);

        // Restore card to its original parent under the GridLayoutGroup.
        _rt.SetParent(_originalParent, false);

        if (_placeholderRT != null)
        {
            // Put card where the placeholder was
            _rt.SetSiblingIndex(_placeholderRT.GetSiblingIndex());
            Destroy(_placeholderRT.gameObject);
            _placeholderRT = null;
        }
        else
        {
            _rt.SetSiblingIndex(_originalSiblingIndex);
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If we never spawned a world unit, it was just a small drag on the card
        if (!_spawnedWorldUnit || _dragUnitInstance == null)
        {
            _usingCardGraphic = false;
            return;
        }

        // Check if released over a valid DropZone AND there is no other unit too close
        Vector3 worldPos = ScreenToWorld(eventData.position);
        DropZone zone = DropZone.GetZoneAtWorldPoint(worldPos);

        if (zone == null || !IsPlacementFree(worldPos))
        {
            // Invalid placement (no zone or overlapping another unit) - destroy the dragged unit
            Object.Destroy(_dragUnitInstance);
        }
        else
        {
            // Valid placement - place unit and re-enable physics
            _dragUnitInstance.transform.position = worldPos;

            if (_dragUnitRb != null)
                _dragUnitRb.simulated = true;

            if (_dragUnitColliders != null)
            {
                foreach (var col in _dragUnitColliders)
                    col.enabled = true;
            }
        }

        // Initialize stats
        var stats = _dragUnitInstance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.SetInitialPosition();
        }

        _dragUnitInstance = null;
        _dragUnitRb = null;
        _dragUnitColliders = null;

        _spawnedWorldUnit = false;
        _usingCardGraphic = false;
    }

    // ======================================================================
    // INTERNAL HELPERS
    // ======================================================================

    private void SwitchToWorldDrag(PointerEventData eventData)
    {
        _usingCardGraphic = false;
        _spawnedWorldUnit = true;

        // Card stays under root while dragging world unit.
        // We keep it invisible so only the unit is visible.
        if (canvasGroup != null)
            canvasGroup.alpha = 0f; // hide card during world dragging

        Vector3 worldPos = ScreenToWorld(eventData.position);
        SpawnWorldUnit(worldPos);
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_cam == null)
            _cam = Camera.main;

        Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        return worldPos;
    }

    private void SpawnWorldUnit(Vector3 worldPos)
    {
        // Instantiate the regular unit prefab in the world
        _dragUnitInstance = Object.Instantiate(data.prefab, worldPos, Quaternion.identity);

        // Cache rigidbody & colliders
        _dragUnitRb = _dragUnitInstance.GetComponent<Rigidbody2D>();
        _dragUnitColliders = _dragUnitInstance.GetComponentsInChildren<Collider2D>();

        // Disable physics & collisions while dragging so the ghost will not push anything
        if (_dragUnitRb != null)
            _dragUnitRb.simulated = false;

        if (_dragUnitColliders != null)
        {
            foreach (var col in _dragUnitColliders)
                col.enabled = false;
        }

        // Initialize stats
        var stats = _dragUnitInstance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Init(Team.MyTeam, data, battleManager.playerUnitsLevel);
            stats.lockedIn = false; // Still in planning phase
        }

        // Ensure AI is disabled during setup phase
        var ai = _dragUnitInstance.GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;
    }

    // Create a dummy object inside the grid so the layout doesn't jump when the card leaves
    private void CreatePlaceholder()
    {
        if (_placeholderRT != null)
            return;

        GameObject placeholderGO = new GameObject("CardPlaceholder", typeof(RectTransform));
        _placeholderRT = placeholderGO.GetComponent<RectTransform>();
        _placeholderRT.SetParent(_originalParent, false);
        _placeholderRT.SetSiblingIndex(_originalSiblingIndex);
        _placeholderRT.sizeDelta = _rt.sizeDelta;

        // Copy LayoutElement settings if present
        LayoutElement myLE = GetComponent<LayoutElement>();
        if (myLE != null)
        {
            LayoutElement le = placeholderGO.AddComponent<LayoutElement>();
            le.preferredWidth = myLE.preferredWidth;
            le.preferredHeight = myLE.preferredHeight;
            le.flexibleWidth = myLE.flexibleWidth;
            le.flexibleHeight = myLE.flexibleHeight;
            le.minWidth = myLE.minWidth;
            le.minHeight = myLE.minHeight;
        }
    }

    /// <summary>
    /// Returns true if there is enough space to place a new unit at the given position.
    /// Uses Physics2D.OverlapCircleAll on unitsLayerMask.
    /// </summary>
    private bool IsPlacementFree(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, minDistanceFromOtherUnits, unitsLayerMask);
        return hits == null || hits.Length == 0;
    }

    // Optional: visualize placement radius in editor (for debugging)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        pos.z = 0f;
        Gizmos.DrawWireSphere(pos, minDistanceFromOtherUnits);
    }
    public void SetCardInteractable(bool interactable)
    {
        if (canvasGroup == null)
            return;

        // Block input
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;

        // Visual feedback
        canvasGroup.alpha = interactable ? 1f : 0.4f;   // 40% transparent when disabled
    }

}
