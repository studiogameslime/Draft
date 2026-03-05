using UnityEngine;

public class SpriteSortByY : MonoBehaviour
{
    [Tooltip("Added to the Y-based sorting order. Use a large value (e.g. 1000) to render above walls.")]
    public int sortingOrderOffset = 0;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + sortingOrderOffset;
    }
}
