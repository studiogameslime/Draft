using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UIRepeatingIconBackground : MonoBehaviour
{
    public Sprite iconSprite;

    private const float IconAlpha = 0.25f;
    private const int TilesPerAxis = 6;
    private const float IconScale = 0.45f;
    private const float ScaleVariation = 0.25f;
    private const float AlphaVariation = 0.3f;
    private const int TextureSize = 1024;
    private static readonly Vector2 ScrollSpeed = new Vector2(0.03f, 0.015f);
    private static readonly Vector2 UvSize = new Vector2(1f, 1f);

    private RawImage img;
    private Rect uv;
    private Texture2D generated;

    void Awake()
    {
        img = GetComponent<RawImage>();
        img.raycastTarget = false;

        uv = img.uvRect;
        uv.size = UvSize;
        img.uvRect = uv;

        Rebuild();
    }

    void Update()
    {
        uv.position += ScrollSpeed * Time.unscaledDeltaTime;
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

        generated = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        generated.wrapMode = TextureWrapMode.Repeat;
        generated.filterMode = FilterMode.Bilinear;

        // clear
        var clear = new Color32(0, 0, 0, 0);
        var pixels = new Color32[TextureSize * TextureSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        generated.SetPixels32(pixels);

        // sprite pixels
        var sprTex = iconSprite.texture;
        var r = iconSprite.textureRect;
        int w = (int)r.width;
        int h = (int)r.height;

        Color[] sprPixels = sprTex.GetPixels(
            Mathf.RoundToInt(r.x),
            Mathf.RoundToInt(r.y),
            w, h
        );

        // cell size — uniform grid
        float cell = (float)TextureSize / TilesPerAxis;

        // fit icon inside cell while preserving aspect ratio
        int maxDraw = Mathf.RoundToInt(cell * IconScale);
        float aspect = (float)w / h;
        int drawW, drawH;
        if (aspect >= 1f)
        {
            drawW = Mathf.Min(w, maxDraw);
            drawH = Mathf.RoundToInt(drawW / aspect);
        }
        else
        {
            drawH = Mathf.Min(h, maxDraw);
            drawW = Mathf.RoundToInt(drawH * aspect);
        }

        // chess-like grid: uniform rows, odd rows offset by half a cell
        for (int row = 0; row * cell < TextureSize; row++)
        {
            int rowOffsetX = (row % 2 == 1) ? Mathf.RoundToInt(cell * 0.5f) : 0;

            for (int col = 0; col < TilesPerAxis + 1; col++)
            {
                // seeded random per icon for stable variation
                int seed = row * 7919 + col * 6271 + 1031;
                System.Random rng = new System.Random(seed);

                float scaleMul = 1f - ScaleVariation * (float)rng.NextDouble() * 0.5f;
                float alphaMul = 1f - AlphaVariation * (float)rng.NextDouble() * 0.6f;

                int iconW = Mathf.RoundToInt(drawW * scaleMul);
                int iconH = Mathf.RoundToInt(drawH * scaleMul);

                // center icon within its cell
                int cellX = Mathf.RoundToInt(col * cell) + rowOffsetX;
                int cellY = Mathf.RoundToInt(row * cell);
                int startX = cellX + Mathf.RoundToInt((cell - iconW) * 0.5f);
                int startY = cellY + Mathf.RoundToInt((cell - iconH) * 0.5f);

                BlitScaledWrapped(
                    sprPixels, w, h,
                    generated,
                    startX, startY,
                    iconW, iconH,
                    IconAlpha * alphaMul
                );
            }
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

                px %= tw; if (px < 0) px += tw;
                py %= th; if (py < 0) py += th;

                dst.SetPixel(px, py, c);
            }
        }
    }
}
