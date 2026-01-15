using UnityEngine;

public class BackgroundWanderUI : MonoBehaviour
{
    // Background moving UI element (animal/environment object)
    [Header("References")]
    [SerializeField] private RectTransform mover;

    // Movement area UI element (any RectTransform, for example a Panel)
    // The mover will move inside this rect, in the same parent space
    [SerializeField] private RectTransform movementArea;

    [Header("Movement")]
    [SerializeField] private float speed = 180f;            // Pixels per second
    [SerializeField] private float arriveDistance = 8f;     // How close is considered "arrived"
    [SerializeField] private bool startAtRandomPosition = true;

    [Header("Rotation")]
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float rotationLerp = 12f;      // Higher = snappier
    [SerializeField] private float angleOffset = 0f;        // Adjust if sprite faces a different direction

    [Header("Layering")]
    // If true, the mover will be moved behind other siblings under the same parent
    [SerializeField] private bool renderBehindOthers = true;

    // Optional: makes motion feel less robotic without ever stopping
    [Header("Steering")]
    [SerializeField] private float directionChangeLerp = 6f; // Higher = changes direction faster

    private Vector2 targetAnchoredPos;
    private Vector2 currentDir = Vector2.right;

    void Reset()
    {
        mover = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (mover == null) mover = GetComponent<RectTransform>();

        if (renderBehindOthers)
        {
            // Put behind other UI elements that share the same parent
            mover.SetAsFirstSibling();
        }

        if (startAtRandomPosition)
        {
            mover.anchoredPosition = GetRandomPointInsideArea();
        }

        targetAnchoredPos = GetRandomPointInsideArea();
    }

    void Update()
    {
        if (mover == null || movementArea == null) return;

        float dt = Time.unscaledDeltaTime;

        Vector2 currentPos = mover.anchoredPosition;
        Vector2 toTarget = targetAnchoredPos - currentPos;
        float dist = toTarget.magnitude;

        // If we reached the target, immediately pick a new one (no waiting, never stop)
        if (dist <= arriveDistance)
        {
            targetAnchoredPos = GetRandomPointInsideArea();
            toTarget = targetAnchoredPos - currentPos;
            dist = toTarget.magnitude;
        }

        // Desired direction toward target
        Vector2 desiredDir = (dist > 0.0001f) ? (toTarget / dist) : currentDir;

        // Smoothly steer toward desired direction to avoid sharp snaps
        currentDir = Vector2.Lerp(currentDir, desiredDir, directionChangeLerp * dt);
        if (currentDir.sqrMagnitude < 0.000001f) currentDir = desiredDir;
        currentDir.Normalize();

        // Move every frame (constant motion)
        currentPos += currentDir * speed * dt;
        mover.anchoredPosition = currentPos;

        // Rotate toward movement direction
        if (rotateToMoveDirection)
        {
            float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg + angleOffset;
            float currentZ = mover.localEulerAngles.z;
            float newZ = Mathf.LerpAngle(currentZ, angle, rotationLerp * dt);
            mover.localEulerAngles = new Vector3(0f, 0f, newZ);
        }
    }

    private Vector2 GetRandomPointInsideArea()
    {
        if (movementArea == null)
            return Vector2.zero;

        Rect area = movementArea.rect;

        // Keep the mover fully inside the movement area by padding with half of its size
        Vector2 halfSize = mover.rect.size * 0.5f;

        float minX = area.xMin + halfSize.x;
        float maxX = area.xMax - halfSize.x;
        float minY = area.yMin + halfSize.y;
        float maxY = area.yMax - halfSize.y;

        // If the area is too small, clamp so we do not create invalid ranges
        if (maxX < minX) { float mid = (minX + maxX) * 0.5f; minX = mid; maxX = mid; }
        if (maxY < minY) { float mid = (minY + maxY) * 0.5f; minY = mid; maxY = mid; }

        return new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (speed < 0f) speed = 0f;
        if (arriveDistance < 0f) arriveDistance = 0f;
        if (rotationLerp < 0f) rotationLerp = 0f;
        if (directionChangeLerp < 0f) directionChangeLerp = 0f;
    }
#endif
}
