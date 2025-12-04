using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DropZone : MonoBehaviour
{
    // רשימה סטטית של כל אזורי הדרופ בסצנה
    public static readonly List<DropZone> AllZones = new List<DropZone>();

    [Header("Visual highlight")]
    public GameObject highlightObject;   // הילד עם SpriteRenderer לבן

    [Header("Blink Settings")]
    public float blinkSpeed = 2f;        // מהירות ההבהוב
    public float minAlpha = 0.15f;       // אלפא מינימלית
    public float maxAlpha = 0.45f;       // אלפא מקסימלית

    private BoxCollider2D _col;
    private SpriteRenderer _highlightSR;
    private bool _isBlinking = false;

    // ----------------------------------------------------
    // LIFECYCLE
    // ----------------------------------------------------
    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();

        if (highlightObject != null)
        {
            _highlightSR = highlightObject.GetComponent<SpriteRenderer>();
            highlightObject.SetActive(false);
        }

        SyncHighlightToCollider();
    }

    private void OnEnable()
    {
        if (!AllZones.Contains(this))
            AllZones.Add(this);
    }

    private void OnDisable()
    {
        AllZones.Remove(this);

        // שלא יישארו היילייטים במקרה
        if (_isBlinking)
        {
            _isBlinking = false;
            if (highlightObject != null)
                highlightObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    // יתעדכן גם כשמשנים גודל/אופסט של הקוליידר באינספקטור
    private void OnValidate()
    {
        _col = GetComponent<BoxCollider2D>();
        SyncHighlightToCollider();
    }
#endif

    private void Update()
    {
        // הבהוב רך לפי סינוס
        if (_isBlinking && _highlightSR != null)
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f; // 0..1
            float a = Mathf.Lerp(minAlpha, maxAlpha, t);

            Color c = _highlightSR.color;
            c.a = a;
            _highlightSR.color = c;
        }
    }

    // ----------------------------------------------------
    // HIGHLIGHT + SYNC
    // ----------------------------------------------------
    private void SyncHighlightToCollider()
    {
        if (_col == null || highlightObject == null)
            return;

        if (_highlightSR == null)
            _highlightSR = highlightObject.GetComponent<SpriteRenderer>();

        if (_highlightSR == null || _highlightSR.sprite == null)
            return;

        // גודל הקוליידר בלוקאל
        Vector2 colliderSize = _col.size;
        Vector2 offset = _col.offset;

        // גודל הספרייט ביחידות עולם כשהסקייל = 1
        Vector2 spriteSize = _highlightSR.sprite.bounds.size;

        // יחס סקייל כדי שהספרייט יכסה בדיוק את הקוליידר
        float scaleX = colliderSize.x / spriteSize.x;
        float scaleY = colliderSize.y / spriteSize.y;

        // למרכז לפי ה-offset של הקוליידר
        highlightObject.transform.localPosition = offset;

        // התאמת סקייל
        highlightObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    public void SetHighlight(bool on)
    {
        if (highlightObject == null)
            return;

        highlightObject.SetActive(on);
        _isBlinking = on;

        // כשמכבים – מחזירים את האלפא למינימום שיהיה עקבי
        if (!on && _highlightSR != null)
        {
            Color c = _highlightSR.color;
            c.a = minAlpha;
            _highlightSR.color = c;
        }
    }

    public static void SetAllHighlights(bool on)
    {
        foreach (var z in AllZones)
        {
            if (z != null)
            {
                if (!on) 
                z.SetHighlight(on);
            }

        }
    }

    // ----------------------------------------------------
    // LOGIC
    // ----------------------------------------------------
    /// בדיקה אם נקודת עולם נמצאת בתוך האזור
    public bool ContainsWorldPoint(Vector3 worldPos)
    {
        if (_col == null) return false;
        return _col.OverlapPoint(worldPos);
    }

    /// חיפוש DropZone שמתאים לנקודת עולם
    public static DropZone GetZoneAtWorldPoint(Vector3 worldPos)
    {
        foreach (var z in AllZones)
        {
            if (z != null && z.ContainsWorldPoint(worldPos))
                return z;
        }
        return null;
    }
}
