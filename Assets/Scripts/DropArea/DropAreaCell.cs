using UnityEngine;
using UnityEngine.UI;

public class DropAreaCell : MonoBehaviour
{
    [Header("Logical grid index (0-based)")]
    public int row;
    public int column;

    [Header("Bonus data")]
    public CellBonusType bonusType = CellBonusType.None;
    [Tooltip("Percent value, e.g. 0.2 = +20%, -0.3 = -30%")]
    public float percentValue = 0f;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Image fillImage;

    [Header("Selection Visual")]
    [SerializeField] private float selectedBrightnessMul = 1.35f;

    public bool IsSpecial =>
        bonusType != CellBonusType.None && Mathf.Abs(percentValue) > 0.0001f;

    private bool isSelected;
    private Color baseColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    public void UpdateColor()
    {
        if (spriteRenderer == null)
            return;

        Color c = new Color(1, 1, 1, 0.4f);

        if (IsSpecial)
        {
            // Default logic: positive value is a good bonus
            bool isGoodBonus = percentValue > 0f;

            // Special logic for spawn time: negative value is a good bonus
            if (bonusType == CellBonusType.SpawnTimePercent)
            {
                isGoodBonus = percentValue < 0f;
            }

            // Apply green for a good bonus, red for a bad penalty
            c = isGoodBonus
                ? new Color(0f, 1f, 0f, 0.7f)
                : new Color(1f, 0f, 0f, 0.7f);
        }

        baseColor = c;
        ApplySelectionVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionVisual();
    }

    private void ApplySelectionVisual()
    {
        if (spriteRenderer == null)
            return;

        if (!isSelected)
        {
            spriteRenderer.color = baseColor;
            return;
        }

        Color boosted = baseColor;
        boosted.r = Mathf.Clamp01(boosted.r * selectedBrightnessMul);
        boosted.g = Mathf.Clamp01(boosted.g * selectedBrightnessMul);
        boosted.b = Mathf.Clamp01(boosted.b * selectedBrightnessMul);
        boosted.a = Mathf.Max(boosted.a, 0.85f);
        spriteRenderer.color = boosted;
    }

    private void OnMouseDown()
    {
        if (!Application.isPlaying)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive
            && !TutorialManager.Instance.IsActionAllowed(this))
            return;

        BattleCellSelectionController.Instance?.SelectCell(this);
        TutorialManager.Instance?.NotifyAction(TutorialAction.CellSelected, this);

        if (IsSpecial)
            TutorialManager.Instance?.NotifyAction(TutorialAction.BonusCellSelected, this);
    }
}
