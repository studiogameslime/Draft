using UnityEngine;

public enum CellBonusType
{
    None,
    HpPercent,
    AttackPercent
    // אפשר להרחיב בהמשך: AttackSpeedPercent, DefensePercent וכו'
}

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

    /// <summary>
    /// True if this cell has any meaningful bonus.
    /// </summary>
    public bool IsSpecial =>
        bonusType != CellBonusType.None && Mathf.Abs(percentValue) > 0.0001f;

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        UpdateColor();
    }

    public void UpdateColor()
    {
        if (spriteRenderer == null)
            return;
        Color c = new Color(1,1,1,0.4f);
        if (IsSpecial)
        {
        c = percentValue >= 0f
            ? new Color(0f, 1f, 0f, 0.7f)   // green with some alpha
            : new Color(1f, 0f, 0f, 0.7f);  // red with some alpha
        }

        spriteRenderer.color = c;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!IsSpecial)
            return;

        Color c = percentValue >= 0f ? Color.green : Color.red;
        c.a = 0.35f;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
#endif
}
