using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DropAreaGrid : MonoBehaviour
{
    [Header("Grid layout")]
    [Min(1)] public int rows = 2;
    [Min(1)] public int columns = 3;
    public Vector2 cellSize = new Vector2(1f, 1f);
    public Vector2 cellSpacing = new Vector2(0.1f, 0.1f);
    public bool centerGridAroundThisTransform = true;

    [Header("Cell prefab & parent")]
    public DropAreaCell cellPrefab;      // prefab that will be instantiated per cell
    public Transform cellsParent;        // optional, if null will use this.transform

    [Header("Background / Bounds (optional)")]
    public SpriteRenderer backgroundSprite;   // optional visual background
    public BoxCollider2D areaCollider;        // optional collider that matches grid bounds

    [Header("Special cells config (by logical row/column)")]
    public List<SpecialCellConfig> specialCells = new List<SpecialCellConfig>();

    // Internal map row/col -> cell instance
    private DropAreaCell[,] _cells;

    // so we won't rebuild inside OnValidate (אסור DestroyImmediate שם)
    private bool _pendingEditorRebuild = false;

    [System.Serializable]
    public class SpecialCellConfig
    {
        [Tooltip("0-based row index")]
        public int row;

        [Tooltip("0-based column index")]
        public int column;

        [Header("Bonus")]
        public CellBonusType bonusType = CellBonusType.None;

        [Tooltip("Percent value, e.g. 0.2 = +20%, -0.3 = -30%")]
        public float percentValue = 0f;
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        if (rows <= 0 || columns <= 0)
            return;

        if (cellPrefab == null)
            return;

        _pendingEditorRebuild = true;
    }

    private void Update()
    {
        if (!Application.isPlaying && _pendingEditorRebuild)
        {
            _pendingEditorRebuild = false;
            RebuildGrid();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            RebuildGrid();
        }
    }

    /// <summary>
    /// Clears existing cells under cellsParent and rebuilds them according
    /// to rows/columns/cellSize/cellSpacing. Also applies special cell config.
    /// </summary>
    public void RebuildGrid()
    {
        if (cellPrefab == null)
            return;

        Transform parent = cellsParent != null ? cellsParent : transform;

        // 1) Clear ALL previous children under parent
        ClearExistingCells(parent);

        // 2) Allocate array
        _cells = new DropAreaCell[rows, columns];

        // 3) Compute total grid size
        float totalWidth = columns * cellSize.x + (columns - 1) * cellSpacing.x;
        float totalHeight = rows * cellSize.y + (rows - 1) * cellSpacing.y;

        Vector2 origin;
        if (centerGridAroundThisTransform)
        {
            // center around this transform
            origin = new Vector2(
                -totalWidth * 0.5f + cellSize.x * 0.5f,
                 totalHeight * 0.5f - cellSize.y * 0.5f
            );
        }
        else
        {
            // origin at top-left relative to this transform
            origin = new Vector2(
                cellSize.x * 0.5f,
               -cellSize.y * 0.5f
            );
        }

        // 4) Instantiate cells
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector2 localPos = new Vector2(
                    origin.x + c * (cellSize.x + cellSpacing.x),
                    origin.y - r * (cellSize.y + cellSpacing.y)
                );

                DropAreaCell cell = Instantiate(cellPrefab, parent);
                cell.transform.localPosition = localPos;
                cell.transform.localRotation = Quaternion.identity;
                cell.transform.localScale = Vector3.one;

                cell.row = r;
                cell.column = c;
                cell.name = $"Cell_{r}_{c}";

                // Apply special config if exists
                ApplySpecialConfigToCell(cell, r, c);

                _cells[r, c] = cell;
            }
        }

        // 5) Update background & collider to match grid bounds if assigned
        UpdateBackgroundAndCollider(totalWidth, totalHeight);
    }

    /// <summary>
    /// Destroys all children under the given parent transform.
    /// </summary>
    private void ClearExistingCells(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(child.gameObject);
            else
                Object.Destroy(child.gameObject);
#else
            Object.Destroy(child.gameObject);
#endif
        }
    }

    private void ApplySpecialConfigToCell(DropAreaCell cell, int row, int column)
    {
        foreach (var conf in specialCells)
        {
            if (conf.row == row && conf.column == column)
            {
                cell.bonusType = conf.bonusType;
                cell.percentValue = conf.percentValue;
                cell.UpdateColor();
                return;
            }
        }

        // If nothing found, make sure it's a normal cell
        cell.bonusType = CellBonusType.None;
        cell.percentValue = 0f;
        cell.UpdateColor();
    }

    private void UpdateBackgroundAndCollider(float totalWidth, float totalHeight)
    {
        if (backgroundSprite != null)
        {
            backgroundSprite.size = new Vector2(totalWidth, totalHeight);
            backgroundSprite.transform.localPosition = Vector3.zero;
        }

        if (areaCollider != null)
        {
            areaCollider.size = new Vector2(totalWidth, totalHeight);
            areaCollider.offset = Vector2.zero;
        }
    }

    public DropAreaCell GetCell(int row, int column)
    {
        if (_cells == null)
            return null;
        if (row < 0 || row >= rows || column < 0 || column >= columns)
            return null;
        return _cells[row, column];
    }

    public DropAreaCell GetClosestCell(Vector3 worldPos, out float distance)
    {
        distance = float.MaxValue;
        DropAreaCell best = null;

        if (_cells == null)
            return null;

        foreach (var cell in _cells)
        {
            if (cell == null) continue;
            float d = Vector3.Distance(worldPos, cell.transform.position);
            if (d < distance)
            {
                distance = d;
                best = cell;
            }
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        if (_cells == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (var cell in _cells)
        {
            if (cell == null) continue;
            Vector3 pos = cell.transform.position;
            Gizmos.DrawWireCube(pos, new Vector3(cellSize.x, cellSize.y, 0.01f));
        }
    }
}
