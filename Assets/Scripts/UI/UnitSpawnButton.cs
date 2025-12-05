using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UnitSpawnButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI refs")]
    public Image iconImage;
    public TMP_Text unitName;
    public TMP_Text UnitToSpawn;
    public TMP_Text UnitSoulCost;
    public CanvasGroup canvasGroup;

    [Header("Drag settings")]
    [Tooltip("How far (in screen pixels) the card must move before switching to world-unit dragging")]
    public float switchToWorldDistance = 40f;

    [Header("Grid placement")]
    public DropAreaGrid dropGrid;       // יתמלא אוטומטית אם לא מחובר
    [Tooltip("Max distance from a cell center to snap placement")]
    public float maxCellSnapDistance = 0.6f;

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
    private RectTransform _placeholderRT;

    // Drag state
    private Vector2 _dragStartScreenPos;
    private bool _usingCardGraphic = false;
    private bool _spawnedWorldUnit = false;

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

        ResolveGridReference();
    }

    // מוצא DropAreaGrid אוטומטית אם לא חיברת ידנית
    private void ResolveGridReference()
    {
        if (dropGrid != null)
            return;

        dropGrid = FindObjectOfType<DropAreaGrid>();
        if (dropGrid == null)
        {
            Debug.LogWarning("UnitSpawnButton: no DropAreaGrid found in scene.");
        }
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

        // עוד פעם ביטחון – אם יצרת את הכרטיס בזמן ריצה אחרי Awake
        ResolveGridReference();
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

        // ליתר ביטחון – אם הגריד עוד לא נמצא, ננסה שוב
        ResolveGridReference();

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
        // Restore card to its original parent under the GridLayoutGroup.
        _rt.SetParent(_originalParent, false);
        if (_placeholderRT != null)
        {
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

        Vector3 worldPos = ScreenToWorld(eventData.position);

        if (dropGrid == null)
        {
            Debug.LogWarning("UnitSpawnButton: dropGrid is null on EndDrag, destroying ghost.");
            Destroy(_dragUnitInstance);
            CleanupDragState();
            return;
        }

        // ===================== GRID PLACEMENT ==========================
        float distToCell = 0;
        DropAreaCell cell = dropGrid.GetClosestCell(worldPos, out distToCell);
        bool hasSouls = SoulsManager.instance.CheckIfThereIsEnoughSouls(data.soulCost);
        bool closeEnough = (cell != null && distToCell <= maxCellSnapDistance);
        bool spaceFree = cell != null && IsPlacementFree(cell.transform.position);
        bool valid = closeEnough && spaceFree && hasSouls;

        if (!valid)
        {
            // Invalid placement - destroy the dragged unit
            Destroy(_dragUnitInstance);
        }
        else
        {
            // Place unit on the cell center
            Vector3 finalPos = cell.transform.position;
            finalPos.z = 0f;
            _dragUnitInstance.transform.position = finalPos;

            // Enable physics & colliders
            if (_dragUnitRb != null)
                _dragUnitRb.simulated = true;
            if (_dragUnitColliders != null)
            {
                foreach (var col in _dragUnitColliders)
                    col.enabled = true;
            }

            // Pay cost
            SoulsManager.instance.UseSouls(data.soulCost);

            // Initialize stats and apply cell bonus
            var stats = _dragUnitInstance.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.SetInitialPosition();
                if (cell.IsSpecial)
                {
                    float p = cell.percentValue;
                    if (Mathf.Abs(p) > 1f)
                        p /= 100f;
                    float multiplier = 1f + p;

                    switch (cell.bonusType)
                    {
                        case CellBonusType.HpPercent:
                            stats.maxHealth = (int)(stats.maxHealth * multiplier);
                            stats.currentHealth = stats.maxHealth;
                            break;
                        case CellBonusType.AttackPercent:
                            stats.damage = (int)(stats.damage * multiplier);
                            break;
                    }
                }
            }
        }

        CleanupDragState();
    }

    // ======================================================================
    // INTERNAL HELPERS
    // ======================================================================
    private void CleanupDragState()
    {
        _dragUnitInstance = null;
        _dragUnitRb = null;
        _dragUnitColliders = null;
        _spawnedWorldUnit = false;
        _usingCardGraphic = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void SwitchToWorldDrag(PointerEventData eventData)
    {
        _usingCardGraphic = false;
        _spawnedWorldUnit = true;

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
        _dragUnitInstance = Object.Instantiate(data.prefab, worldPos, Quaternion.identity);

        _dragUnitRb = _dragUnitInstance.GetComponent<Rigidbody2D>();
        _dragUnitColliders = _dragUnitInstance.GetComponentsInChildren<Collider2D>();

        if (_dragUnitRb != null)
            _dragUnitRb.simulated = false;
        if (_dragUnitColliders != null)
        {
            foreach (var col in _dragUnitColliders)
                col.enabled = false;
        }

        var stats = _dragUnitInstance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Init(Team.MyTeam, data, battleManager.playerUnitsLevel);
            stats.lockedIn = false;
        }

        var ai = _dragUnitInstance.GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;
    }

    private void CreatePlaceholder()
    {
        if (_placeholderRT != null)
            return;

        GameObject placeholderGO = new GameObject("CardPlaceholder", typeof(RectTransform));
        _placeholderRT = placeholderGO.GetComponent<RectTransform>();
        _placeholderRT.SetParent(_originalParent, false);
        _placeholderRT.SetSiblingIndex(_originalSiblingIndex);
        _placeholderRT.sizeDelta = _rt.sizeDelta;

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

    private bool IsPlacementFree(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, minDistanceFromOtherUnits, unitsLayerMask);
        return hits == null || hits.Length == 0;
    }

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

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.alpha = interactable ? 1f : 0.4f;
    }
}
