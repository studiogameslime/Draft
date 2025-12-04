using UnityEngine;

public class DropZonesSpacePreview : MonoBehaviour
{
    [Tooltip("כמה זמן צריך להחזיק את ה-Space עד שהאזוריים יופיעו (בשניות).")]
    public float holdDelay = 0.15f;

    private bool _isShowing = false;
    private float _holdTimer = 0f;

    private void Update()
    {
        // אם מחזיקים Space
        if (Input.GetKey(KeyCode.Space))
        {
            _holdTimer += Time.deltaTime;

            // אחרי שהחזקנו מספיק זמן – נדליק היילייט
            if (!_isShowing && _holdTimer >= holdDelay)
            {
                _isShowing = true;
                DropZone.SetAllHighlights(true);
            }
        }
        else 
        {
            // שחררנו Space – לאפס ולהסתיר
            _holdTimer = 0f ;

            if (_isShowing)
            {
                _isShowing = false;
                DropZone.SetAllHighlights(false);
            }
        }
    }

    private void OnDisable()
    {
        // לוודא שלא נשארים היילייטים דולקים בטעות
        if (_isShowing)
        {
            _isShowing = false;
            DropZone.SetAllHighlights(false);
        }
        _holdTimer = 0f;
    }
}
