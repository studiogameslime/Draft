using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeTreeUIController : MonoBehaviour
{
    [Header("Runtime (set from outside)")]
    [SerializeField] private UnitDefinition unit; // יכול להישאר ריק באינספקטור

    [Header("Layers")]
    [SerializeField] private RectTransform nodesLayer;
    [SerializeField] private RectTransform linesLayer;

    [Header("UI")]
    [SerializeField] private TMP_Text availableSkillPointsText;

    [Header("Prefabs")]
    [SerializeField] private UnitUpgradeNodeView nodePrefab;
    [SerializeField] private Image linePrefab;

    [Header("Auto Layout")]
    [SerializeField] private float startY = -80f;       // Unlock (tier 0)
    [SerializeField] private float tierSpacingY = 180f; // מרווח בין tiers (למטה)
    [SerializeField] private float laneSpacingX = 260f; // מרווח שמאל/ימין
    [SerializeField] private float lineThickness = 10f;

    private readonly Dictionary<UnitUpgradeNodeDefinition, UnitUpgradeNodeView> _views = new();
    private readonly List<Image> _lines = new();
    private UnitProgressData _progress;

    // חדש: אירוע לבחירת נוד (בשביל לפתוח חלון)
    public System.Action<UnitUpgradeNodeDefinition, bool, bool> OnNodeSelected;
    // (node, unlocked, canUnlock)

    // ====== API חיצוני: תקרא לזה מהחלון שפותח את היחידה ======
    public void SetUnit(UnitDefinition newUnit)
    {
        unit = newUnit;
        Rebuild();
    }

    private void OnEnable()
    {
        if (unit != null)
            Rebuild();
    }

    public void Rebuild()
    {
        if (unit == null)
        {
            Debug.LogWarning("[UpgradeTreeUIController] unit is null. Call SetUnit(unitDef) from your UnitDetails window.");
            return;
        }

        if (nodesLayer == null || linesLayer == null || nodePrefab == null || linePrefab == null)
        {
            Debug.LogError("[UpgradeTreeUIController] Missing references (layers/prefabs).");
            return;
        }

        // חשוב: קווים מאחורי הנודים
        linesLayer.SetAsFirstSibling();
        nodesLayer.SetAsLastSibling();

        _progress = UnitUpgradeProgressService.GetOrCreateUnitProgress(unit.id);

        ClearChildren(nodesLayer);
        ClearChildren(linesLayer);
        _views.Clear();
        _lines.Clear();

        if (availableSkillPointsText != null)
        {
            int availableSkillPoints = (_progress != null) ? _progress.skillPoints : 0;
            availableSkillPointsText.text = $"{unit.displayName} skill points: {availableSkillPoints}";

        }

        // 1) Spawn node views
        for (int i = 0; i < unit.nodes.Count; i++)
        {
            var node = unit.nodes[i];
            if (node == null) continue;

            var view = Instantiate(nodePrefab, nodesLayer);
            _views[node] = view;
        }

        // 2) Position
        AutoPositionNodes();

        // 3) Lines
        Canvas.ForceUpdateCanvases();
        DrawAllLines();

        // 4) States
        RefreshStates();
    }

    private void RefreshStates()
    {
        if (_progress == null) return;

        foreach (var kvp in _views)
        {
            var node = kvp.Key;
            var view = kvp.Value;

            bool unlocked = UnitUpgradeProgressService.IsUnlocked(_progress, node.nodeId);
            bool isAvailable = UnitUpgradeProgressService.IsAvailableToUnlock(unit, _progress, node);
            bool canAfford = UnitUpgradeProgressService.CanAfford(_progress, node);

            view.Bind(node, unlocked, isAvailable, canAfford, HandleNodeClicked);
        }
    }


    private void HandleNodeClicked(UnitUpgradeNodeDefinition node)
    {
        if (node == null || unit == null) return;

        // פותחים חלון פרטים במקום לנסות לפתוח ישר
        if (UnitUpgradeDetailsPopupController.Instance != null)
        {
            UnitUpgradeNodeView view = null;
            _views.TryGetValue(node, out view);

            UnitUpgradeDetailsPopupController.Instance.Show(
                unit,
                node,
                treeUI: this,
                sourceRect: view != null ? view.Rect : null
            );
        }
    }


    // =========================
    // AUTO LAYOUT (דינאמי)
    // =========================
    private void AutoPositionNodes()
    {
        float centerX = 0f;

        foreach (var kvp in _views)
        {
            var node = kvp.Key;
            var view = kvp.Value;
            var rt = view.Rect;

            // Bottom-Center anchored positioning
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = GetNodePosition(node, centerX);
        }
    }

    private Vector2 GetNodePosition(UnitUpgradeNodeDefinition node, float centerX)
    {
        float x = centerX;

        switch (node.lane)
        {
            case UpgradeLane.Left: x = centerX - laneSpacingX; break;
            case UpgradeLane.Right: x = centerX + laneSpacingX; break;
            case UpgradeLane.Center: x = centerX; break;
        }

        float y = startY + ((float)node.tier * tierSpacingY);
        return new Vector2(x, y);
    }

    // =========================
    // LINES
    // =========================
    private void DrawAllLines()
    {
        ClearChildren(linesLayer);
        _lines.Clear();

        for (int n = 0; n < unit.nodes.Count; n++)
        {
            var node = unit.nodes[n];
            if (node == null) continue;
            if (!_views.TryGetValue(node, out var toView)) continue;
            if (node.prerequisites == null) continue;

            for (int i = 0; i < node.prerequisites.Count; i++)
            {
                var prereq = node.prerequisites[i];
                if (prereq == null) continue;
                if (!_views.TryGetValue(prereq, out var fromView)) continue;

                CreateLine(fromView.Rect, toView.Rect);
            }
        }
    }

    private void CreateLine(RectTransform from, RectTransform to)
    {
        var img = Instantiate(linePrefab, linesLayer);

        Vector3 fromWorld = from.TransformPoint(from.rect.center);
        Vector3 toWorld = to.TransformPoint(to.rect.center);
        Vector3 fromLocal = linesLayer.InverseTransformPoint(fromWorld);
        Vector3 toLocal = linesLayer.InverseTransformPoint(toWorld);

        Vector3 dir = (toLocal - fromLocal);
        float dist = dir.magnitude;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = fromLocal + dir * 0.5f;
        rt.sizeDelta = new Vector2(dist, lineThickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        _lines.Add(img);
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
