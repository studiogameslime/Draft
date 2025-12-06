using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UnitSpawnButton : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI refs")]
    public Image iconImage;
    public TMP_Text unitName;
    public TMP_Text UnitToSpawn;
    public TMP_Text UnitSoulCost;
    public CanvasGroup canvasGroup;

    [Header("Drag settings")]
    public float switchToWorldDistance = 40f;

    [Header("Grid placement")]
    public float maxCellSnapDistance = 0.6f;

    [Header("Placement rules")]
    public LayerMask unitsLayerMask;
    public float minDistanceFromOtherUnits = 0.5f;

    [HideInInspector] public BattleManager battleManager;

    // Data
    private UnitDefinition data;
    private UnitSelectionUI selectionUI;

    // Layout state
    private RectTransform _rt;
    private Transform _originalParent;
    private Vector2 _originalAnchoredPos;
    private int _originalSiblingIndex;
    private RectTransform _placeholderRT;

    // Drag state
    private Vector2 _dragStartScreenPos;
    private bool _usingCardGraphic = false;
    private bool _spawnedWorldUnit = false;

    // Dragged unit object
    private GameObject _dragUnitInstance;
    private Rigidbody2D _dragUnitRb;
    private Collider2D[] _dragUnitColliders;
    private UnityEngine.AI.NavMeshAgent _dragUnitAgent;

    private Camera _cam;

    // ================================================================
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

    public void Init(UnitDefinition def, UnitSelectionUI ui)
    {
        data = def;
        selectionUI = ui;
        battleManager = ui != null ? ui.battleManager : null;

        unitName.text = def.displayName;
        UnitToSpawn.text = "+" + def.spawnCount;
        UnitSoulCost.text = def.soulCost.ToString();

        iconImage.sprite = def.icon;
        iconImage.transform.localScale = Vector3.one * def.iconScale;
        iconImage.SetNativeSize();
    }

    // ================================================================
    // DRAG BEGIN
    // ================================================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (data == null || data.prefab == null || battleManager == null)
            return;
        if (battleManager.IsBattleRunning)
            return;

        _dragStartScreenPos = eventData.position;
        _originalSiblingIndex = _rt.GetSiblingIndex();
        _usingCardGraphic = true;
        _spawnedWorldUnit = false;

        CreatePlaceholder();

        _rt.SetParent(transform.root, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 1f;
    }

    // ================================================================
    // DRAG UPDATE
    // ================================================================
    public void OnDrag(PointerEventData eventData)
    {
        if (data == null) return;

        if (_usingCardGraphic)
        {
            _rt.position = eventData.position;

            float dist = Vector2.Distance(eventData.position, _dragStartScreenPos);
            if (dist >= switchToWorldDistance)
            {
                SwitchToWorldDrag(eventData);
            }
        }
        else if (_spawnedWorldUnit && _dragUnitInstance != null)
        {
            Vector3 worldPos = ScreenToWorld(eventData.position);
            _dragUnitInstance.transform.position = worldPos;
        }
    }

    // ================================================================
    // DRAG END
    // ================================================================
    public void OnEndDrag(PointerEventData eventData)
    {
        ResetCardToGrid();

        if (!_spawnedWorldUnit || _dragUnitInstance == null)
        {
            CleanupDragState();
            return;
        }

        DropAreaGrid closestGrid = null;
        DropAreaCell closestCell = null;
        float bestDist = float.MaxValue;

        foreach (var grid in battleManager.dropAreaGrids)
        {
            float cellDist;
            var cell = grid.GetClosestCell(_dragUnitInstance.transform.position, out cellDist);
            if (cell != null && cellDist < bestDist)
            {
                bestDist = cellDist;
                closestCell = cell;
                closestGrid = grid;
            }
        }

        bool valid =
            closestCell != null &&
            bestDist <= maxCellSnapDistance &&
            SoulsManager.instance.CheckIfThereIsEnoughSouls(data.soulCost) &&
            IsPlacementFree(closestCell.transform.position);

        if (!valid)
        {
            Destroy(_dragUnitInstance);
            CleanupDragState();
            return;
        }

        // --- Place ---
        Vector3 finalPos = closestCell.transform.position;
        finalPos.z = 0f;
        _dragUnitInstance.transform.position = finalPos;

        EnablePhysicsAfterPlace();
        SoulsManager.instance.UseSouls(data.soulCost);

        // Apply cell bonus
        var stats = _dragUnitInstance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.SetInitialPosition();

            if (closestCell.IsSpecial)
            {
                float p = closestCell.percentValue;
                if (Mathf.Abs(p) > 1f) p /= 100f;
                float mul = 1f + p;

                switch (closestCell.bonusType)
                {
                    case CellBonusType.HpPercent:
                        stats.maxHealth = Mathf.RoundToInt(stats.maxHealth * mul);
                        stats.currentHealth = stats.maxHealth;
                        break;

                    case CellBonusType.AttackPercent:
                        stats.damage = Mathf.RoundToInt(stats.damage * mul);
                        break;
                }
            }
        }

        CleanupDragState();
    }

    // ================================================================
    // HELPERS
    // ================================================================
    void SwitchToWorldDrag(PointerEventData eventData)
    {
        _usingCardGraphic = false;
        _spawnedWorldUnit = true;

        canvasGroup.alpha = 0f;

        Vector3 worldPos = ScreenToWorld(eventData.position);
        SpawnWorldUnit(worldPos);
    }

    void SpawnWorldUnit(Vector3 pos)
    {
        _dragUnitInstance = Instantiate(data.prefab, pos, Quaternion.identity);

        _dragUnitRb = _dragUnitInstance.GetComponent<Rigidbody2D>();
        _dragUnitColliders = _dragUnitInstance.GetComponentsInChildren<Collider2D>();
        _dragUnitAgent = _dragUnitInstance.GetComponent<UnityEngine.AI.NavMeshAgent>();

        DisablePhysicsForDrag();

        var stats = _dragUnitInstance.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Init(Team.MyTeam, data, battleManager.playerUnitsLevel);
            stats.lockedIn = false;
        }

        var ai = _dragUnitInstance.GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
    }

    void DisablePhysicsForDrag()
    {
        if (_dragUnitRb != null)
            _dragUnitRb.simulated = false;

        if (_dragUnitColliders != null)
            foreach (var col in _dragUnitColliders)
                col.enabled = false;

        if (_dragUnitAgent != null)
            _dragUnitAgent.enabled = false;
    }

    void EnablePhysicsAfterPlace()
    {
        if (_dragUnitRb != null)
            _dragUnitRb.simulated = true;

        if (_dragUnitColliders != null)
            foreach (var col in _dragUnitColliders)
                col.enabled = true;

        if (_dragUnitAgent != null)
        {
            _dragUnitAgent.enabled = true;
            _dragUnitAgent.Warp(_dragUnitInstance.transform.position);
        }
    }

    Vector3 ScreenToWorld(Vector2 s)
    {
        Vector3 w = _cam.ScreenToWorldPoint(s);
        w.z = 0f;
        return w;
    }

    private void CreatePlaceholder()
    {
        if (_placeholderRT != null) return;

        GameObject placeholderGO = new GameObject("CardPlaceholder", typeof(RectTransform));
        _placeholderRT = placeholderGO.GetComponent<RectTransform>();
        _placeholderRT.SetParent(_originalParent, false);
        _placeholderRT.SetSiblingIndex(_originalSiblingIndex);
        _placeholderRT.sizeDelta = _rt.sizeDelta;
    }

    private void ResetCardToGrid()
    {
        _rt.SetParent(_originalParent, false);
        _rt.SetSiblingIndex(_originalSiblingIndex);

        if (_placeholderRT != null)
        {
            Destroy(_placeholderRT.gameObject);
            _placeholderRT = null;
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    bool IsPlacementFree(Vector3 pos)
    {
        var hits = Physics2D.OverlapCircleAll(pos, minDistanceFromOtherUnits, unitsLayerMask);
        return hits == null || hits.Length == 0;
    }

    private void CleanupDragState()
    {
        _dragUnitInstance = null;
        _dragUnitRb = null;
        _dragUnitColliders = null;
        _dragUnitAgent = null;

        _spawnedWorldUnit = false;
        _usingCardGraphic = false;

        canvasGroup.alpha = 1f;
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
