using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DropZone : MonoBehaviour
{
    // List of all active DropZones
    public static readonly List<DropZone> AllZones = new List<DropZone>();

    [Header("Visual highlight")]
    public GameObject highlightObject;    // The sprite used for blinking highlight

    [Header("Blink Settings")]
    public float blinkSpeed = 2f;         // Speed of blinking
    public float minAlpha = 0.15f;
    public float maxAlpha = 0.45f;

    private BoxCollider2D _col;
    private SpriteRenderer _highlightSR;
    private bool _isBlinking = false;

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

        if (_isBlinking && highlightObject != null)
        {
            _isBlinking = false;
            highlightObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _col = GetComponent<BoxCollider2D>();
        SyncHighlightToCollider();
    }
#endif

    private void Update()
    {
        if (_isBlinking && _highlightSR != null)
        {
            // Simple pulse animation
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
            float a = Mathf.Lerp(minAlpha, maxAlpha, t);

            Color c = _highlightSR.color;
            c.a = a;
            _highlightSR.color = c;
        }
    }

    // Adjust highlight size to match collider exactly
    private void SyncHighlightToCollider()
    {
        if (_col == null || highlightObject == null)
            return;

        if (_highlightSR == null)
            _highlightSR = highlightObject.GetComponent<SpriteRenderer>();

        if (_highlightSR == null || _highlightSR.sprite == null)
            return;

        Vector2 colliderSize = _col.size;
        Vector2 offset = _col.offset;

        Vector2 spriteSize = _highlightSR.sprite.bounds.size;

        float scaleX = colliderSize.x / spriteSize.x;
        float scaleY = colliderSize.y / spriteSize.y;

        highlightObject.transform.localPosition = offset;
        highlightObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    // Turn highlight on/off for this zone
    public void SetHighlight(bool on)
    {
        if (highlightObject == null)
            return;

        highlightObject.SetActive(on);
        _isBlinking = on;

        if (!on && _highlightSR != null)
        {
            Color c = _highlightSR.color;
            c.a = minAlpha;
            _highlightSR.color = c;
        }
    }

    // Turn highlight on/off for all zones
    public static void SetAllHighlights(bool on)
    {
        foreach (var z in AllZones)
        {
            if (z != null)
                z.SetHighlight(on);
        }
    }

    // Check if point is inside this zone's collider
    public bool ContainsWorldPoint(Vector3 worldPos)
    {
        if (_col == null) return false;
        return _col.OverlapPoint(worldPos);
    }

    // Find which zone contains a given world point
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
