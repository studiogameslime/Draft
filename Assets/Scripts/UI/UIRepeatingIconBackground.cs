using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UIRepeatingIconBackground : MonoBehaviour
{
    [Header("Icon Source")]
    public Sprite iconSprite;

    [Header("Pattern Look")]
    [Range(0.01f, 1f)] public float iconAlpha = 0.08f;

    [Tooltip("יותר = חרבות יותר קרובות (בלי לשנות את גודל החרב)")]
    [Range(1, 16)] public int tilesPerAxis = 5;

    [Tooltip("Vertical spacing multiplier between rows (1 = original, lower = denser)")]
    [Range(0.4f, 1f)] public float rowHeightMultiplier = 0.8f;

    [Tooltip("ריווח בתוך התא (פיקסלים) סביב החרב. קטן = קרוב יותר")]
    [Range(0, 64)] public int padding = 0;

    [Tooltip("גודל הטקסטורה שנוצרת. חזקה של 2 מומלץ")]
    public int textureSize = 1024;

    [Header("Scroll")]
    public Vector2 speed = new Vector2(0.006f, 0.003f);

    [Tooltip("כדי שלא יהיו 'קבוצות' – תשאיר 1,1")]
    public Vector2 uvSize = new Vector2(1f, 1f);

    private RawImage img;
    private Rect uv;
    private Texture2D generated;

    void Awake()
    {
        img = GetComponent<RawImage>();
        img.raycastTarget = false;

        uv = img.uvRect;
        uv.size = uvSize;
        img.uvRect = uv;

        Rebuild();
    }

    void Update()
    {
        uv.position += speed * Time.unscaledDeltaTime;
        img.uvRect = uv;
    }

    public void SetIcon(Sprite newIcon)
    {
        iconSprite = newIcon;
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (iconSprite == null) return;

        if (generated != null) Destroy(generated);

        generated = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        generated.wrapMode = TextureWrapMode.Repeat;
        generated.filterMode = FilterMode.Bilinear;

        // clear
        var clear = new Color32(0, 0, 0, 0);
        var pixels = new Color32[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        generated.SetPixels32(pixels);

        // sprite pixels (Read/Write enabled on source texture)
        var sprTex = iconSprite.texture;
        var r = iconSprite.textureRect;
        int w = (int)r.width;
        int h = (int)r.height;

        Color[] sprPixels = sprTex.GetPixels(
            Mathf.RoundToInt(r.x),
            Mathf.RoundToInt(r.y),
            w, h
        );

        // cell size (controls spacing between icons)
        float cell = (float)textureSize / tilesPerAxis;

        int drawW = Mathf.Min(w, Mathf.FloorToInt(cell) - padding * 2);
        int drawH = Mathf.Min(h, Mathf.FloorToInt(cell) - padding * 2);

        int row = 0;
        float y = 0f;

        while (y < textureSize)
        {
            bool isOffsetRow = (row % 2 == 1);
            int rowOffsetX = isOffsetRow ? Mathf.RoundToInt(cell * 0.5f) : 0;

            for (int tx = 0; tx < tilesPerAxis; tx++)
            {
                int startX = Mathf.RoundToInt(tx * cell) + rowOffsetX + padding;
                int startY = Mathf.RoundToInt(y) + padding;

                BlitScaledWrapped(
                    sprPixels,
                    w,
                    h,
                    generated,
                    startX,
                    startY,
                    drawW,
                    drawH,
                    iconAlpha
                );
            }

            y += cell * rowHeightMultiplier;
            row++;
        }



        generated.Apply(false);

        img.texture = generated;
        img.color = Color.white;
    }

    static void BlitScaledWrapped(Color[] src, int srcW, int srcH,
        Texture2D dst, int dstX, int dstY, int dstW, int dstH, float alpha)
    {
        int tw = dst.width;
        int th = dst.height;

        for (int y = 0; y < dstH; y++)
        {
            int sy = Mathf.FloorToInt((y / (float)dstH) * srcH);
            for (int x = 0; x < dstW; x++)
            {
                int sx = Mathf.FloorToInt((x / (float)dstW) * srcW);
                Color c = src[sy * srcW + sx];
                c.a *= alpha;
                if (c.a <= 0.001f) continue;

                int px = dstX + x;
                int py = dstY + y;

                // wrap to avoid any seam between repeated tiles
                px %= tw; if (px < 0) px += tw;
                py %= th; if (py < 0) py += th;

                dst.SetPixel(px, py, c);
            }
        }
    }
}
