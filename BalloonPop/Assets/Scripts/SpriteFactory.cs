using UnityEngine;

/// <summary>
/// Generates all game sprites procedurally using Texture2D pixel math.
/// No external art files needed.
/// </summary>
public static class SpriteFactory
{
    // ── Balloon (circle + BIG string/tail + gloss highlight) ─────────────
    public static Sprite MakeBalloon(int size, Color fillColor)
    {
        bool premium = PlayerPrefs.GetInt("BalloonStyle", 0) == 0;
        // Use extra height for the long tail
        int texH = (int)(size * 1.5f);
        Texture2D tex = NewTex(size, texH);
        Color[] px    = new Color[size * texH];

        float cx = size * 0.5f;
        float cy = texH * 0.60f;   // balloon centre — upper portion
        float rx = size * 0.42f;
        float ry = size * 0.44f;

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float d  = dx * dx + dy * dy;

                if (d <= 1f)
                {
                    // Balloon body
                    // The premium finish can be switched to a simpler classic look in Settings.
                    Color c   = premium
                        ? Color.Lerp(fillColor, new Color(0.05f, 0.08f, 0.20f), Mathf.Clamp01(d) * 0.20f)
                        : fillColor;
                    float edge = Mathf.Clamp01((1f - d) * 6f);
                    float gx  = (x - cx * 0.62f) / (rx * 0.45f);
                    float gy  = (y - cy * 1.28f) / (ry * 0.45f);
                    float gl  = Mathf.Clamp01(1f - gx*gx - gy*gy) * 0.62f;
                    c = Color.Lerp(c, Color.white, gl);
                    float shadow = Mathf.Clamp01((dy + 0.4f) * 0.5f) * 0.28f;
                    c = Color.Lerp(c, Color.black, shadow);
                    if (premium)
                    {
                        // Subtle cool rim light separates the balloon from the twilight backdrop.
                        float rim = Mathf.Clamp01((d - 0.72f) / 0.28f) * 0.22f;
                        c = Color.Lerp(c, new Color(0.62f, 0.86f, 1f), rim);
                    }
                    c.a = edge;
                    px[y * size + x] = c;
                }
                else
                {
                    px[y * size + x] = Color.clear;
                }
            }
        }

        // Knot
        int kx = Mathf.RoundToInt(cx);
        int ky = Mathf.RoundToInt(cy - ry * 0.87f);
        FillCircle(px, size, texH, kx, ky, 4, fillColor * 0.7f);

        // BIG wavy tail/string — multiple pixels wide, curvy
        Color stringCol = new Color(0.75f, 0.86f, 1.00f, 0.78f);
        int tailStart = ky - 2;
        int tailEnd   = (int)(texH * 0.05f);
        for (int y = tailEnd; y < tailStart; y++)
        {
            // Sinusoidal wave
            float t   = (float)(tailStart - y) / (tailStart - tailEnd);
            float wave = Mathf.Sin(t * Mathf.PI * 3.5f) * (size * 0.07f);
            int tx = Mathf.RoundToInt(cx + wave);

            // Draw 2-pixel wide string
            for (int sx = tx - 1; sx <= tx + 1; sx++)
                if (sx >= 0 && sx < size)
                    px[y * size + sx] = stringCol;
        }

        return Apply(tex, px, size, texH);
    }

    // ── Heart balloon ─────────────────────────────────────────────────────
    public static Sprite MakeHeartBalloon(int size, Color fillColor)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx =  (x - size * 0.5f)  / (size * 0.36f);
                float ny = -(y - size * 0.48f)  / (size * 0.36f);
                float v  = nx * nx + ny * ny - 1f;
                float h  = v * v * v - nx * nx * ny * ny * ny;

                if (h <= 0.05f)
                {
                    float inside = Mathf.Clamp01(-h * 5f);
                    Color c      = Color.Lerp(fillColor, Color.white, inside * 0.4f);
                    c.a          = Mathf.Clamp01(inside * 3f);
                    px[y * size + x] = c;
                }
                else px[y * size + x] = Color.clear;
            }
        }

        return Apply(tex, px, size, size);
    }

    // ── Bomb ──────────────────────────────────────────────────────────────
    public static Sprite MakeBomb(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];

        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= r)
                {
                    Color c = new Color(0.12f, 0.12f, 0.12f);
                    // Highlight
                    float hd = Mathf.Sqrt((x - cx * 0.72f) * (x - cx * 0.72f) +
                                          (y - cy * 1.22f) * (y - cy * 1.22f));
                    c = Color.Lerp(c, new Color(0.45f, 0.45f, 0.45f),
                        Mathf.Clamp01(1f - hd / (r * 0.28f)));
                    c.a = Mathf.Clamp01((r - d) / 2f);
                    px[y * size + x] = c;
                }
                else px[y * size + x] = Color.clear;
            }
        }

        // Orange fuse sparks
        int fx = Mathf.RoundToInt(cx + r * 0.6f);
        int fy = Mathf.RoundToInt(cy + r * 0.85f);
        FillCircle(px, size, size, fx, fy, 5, new Color(1f, 0.6f, 0f));
        FillCircle(px, size, size, fx + 4, fy + 6, 3, new Color(1f, 0.9f, 0.1f));

        return Apply(tex, px, size, size);
    }

    // ── Star (gold balloon) ───────────────────────────────────────────────
    public static Sprite MakeStar(int size, Color fillColor)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];

        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.46f, innerR = size * 0.20f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float angle = Mathf.Atan2(dy, dx);
                float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                float sector = Mathf.PI * 2f / 5f;
                float a      = Mathf.Repeat(angle + Mathf.PI / 2f, sector);
                float a2     = Mathf.Abs(a - sector * 0.5f);
                float starR  = outerR * innerR / Mathf.Lerp(innerR, outerR,
                                   Mathf.Abs(Mathf.Cos(a2 * 5f)));

                if (dist <= starR)
                {
                    Color c = Color.Lerp(fillColor, Color.white, 0.35f * (1f - dist / starR));
                    c.a = 1f;
                    px[y * size + x] = c;
                }
                else px[y * size + x] = Color.clear;
            }
        }

        return Apply(tex, px, size, size);
    }

    // ── Rainbow circle (animated by script) ───────────────────────────────
    public static Sprite MakeRainbowCircle(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.44f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= r)
                {
                    float hue = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f;
                    Color c   = Color.HSVToRGB(hue, 0.9f, 1f);
                    c.a = Mathf.Clamp01((r - d) / 3f);
                    px[y * size + x] = c;
                }
                else px[y * size + x] = Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Coin (for score display) ──────────────────────────────────────────
    public static Sprite MakeCoin(int size)
    {
        return MakeCircleGloss(size, new Color(1f, 0.82f, 0.1f),
                                     new Color(0.9f, 0.6f, 0f));
    }

    // ── Heart (for lives display) ─────────────────────────────────────────
    public static Sprite MakeHeart(int size, Color col)
    {
        return MakeHeartBalloon(size, col);
    }

    // ── Filled glossy circle ──────────────────────────────────────────────
    public static Sprite MakeCircleGloss(int size, Color fill, Color rim)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.46f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= r)
                {
                    Color c = Color.Lerp(fill, rim, d / r);
                    float gx = (x - cx * 0.65f), gy = (y - cy * 1.3f);
                    float gd = Mathf.Sqrt(gx * gx + gy * gy);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - gd / (r * 0.42f)) * 0.55f);
                    c.a = Mathf.Clamp01((r - d) / 2f);
                    px[y * size + x] = c;
                }
                else px[y * size + x] = Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Rounded rectangle (panels, buttons) ──────────────────────────────
    public static Sprite MakeRoundedRect(int w, int h, float corner, Color col)
    {
        // UI sprites are stretched; cap texture size to avoid large startup allocations.
        float factor = Mathf.Min(1f, 192f / Mathf.Max(w, h));
        w = Mathf.Max(4, Mathf.RoundToInt(w * factor));
        h = Mathf.Max(4, Mathf.RoundToInt(h * factor));
        corner *= factor;
        Texture2D tex = NewTex(w, h);
        Color[] px    = new Color[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nearX = Mathf.Clamp(x, corner, w - corner);
                float nearY = Mathf.Clamp(y, corner, h - corner);
                float dx = x - nearX, dy = y - nearY;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                Color c  = col;
                c.a = col.a * Mathf.Clamp01(corner - d + 1f);
                px[y * w + x] = c;
            }

        return Apply(tex, px, w, h);
    }

    // ── Gradient background ────────────────────────────────────────────────
    public static Sprite MakeGradient(int w, int h, Color top, Color bottom)
    {
        Texture2D tex = NewTex(w, h);
        Color[] px    = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, (float)y / h);
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }

        return Apply(tex, px, w, h);
    }

    // ── Gear icon (settings) ──────────────────────────────────────────────
    public static Sprite MakeGear(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.44f, innerR = size * 0.26f, toothR = size * 0.36f;
        int teeth = 8;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Atan2(dy, dx);

                bool inTooth = false;
                float sector = Mathf.PI * 2f / teeth;
                float local  = Mathf.Repeat(a, sector) / sector;
                if (local < 0.4f || local > 0.6f)
                    inTooth = d > toothR && d < outerR;

                bool inBody = d >= innerR && d <= toothR;
                bool inHole = d < innerR * 0.6f;

                if ((inBody || inTooth) && !inHole)
                    px[y * size + x] = new Color(0.85f, 0.85f, 0.85f, 1f);
                else
                    px[y * size + x] = Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Home icon ─────────────────────────────────────────────────────────
    public static Sprite MakeHomeIcon(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];
        Color  col    = Color.white;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;
                bool roof = ny > 0.25f && ny < 0.55f &&
                            Mathf.Abs(nx - 0.5f) < (ny - 0.25f) * 1.2f;
                bool wall = ny >= 0.52f && ny < 0.82f &&
                            nx > 0.2f && nx < 0.8f;
                bool door = ny >= 0.57f && ny < 0.82f &&
                            nx > 0.38f && nx < 0.62f;

                px[y * size + x] = (roof || (wall && !door)) ? col : Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Play triangle ──────────────────────────────────────────────────────
    public static Sprite MakePlayIcon(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;
                float distFromLeft = nx - 0.22f;
                float midDist = Mathf.Abs(ny - 0.5f);
                bool inside = distFromLeft >= 0f &&
                              distFromLeft * 0.85f > midDist;
                px[y * size + x] = inside ? Color.white : Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Trophy ────────────────────────────────────────────────────────────
    public static Sprite MakeTrophy(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];
        Color  gold   = new Color(1f, 0.82f, 0.1f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size - 0.5f;
                float ny = (float)y / size;

                // Cup body
                bool cup = Mathf.Abs(nx) < 0.32f - (1f - ny) * 0.1f
                           && ny > 0.15f && ny < 0.65f;
                // Stem
                bool stem = Mathf.Abs(nx) < 0.07f && ny >= 0.63f && ny < 0.75f;
                // Base
                bool bas  = Mathf.Abs(nx) < 0.3f && ny >= 0.73f && ny < 0.82f;

                px[y * size + x] = (cup || stem || bas) ? gold : Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── Pause icon (two bars) ─────────────────────────────────────────────
    public static Sprite MakePauseIcon(int size)
    {
        Texture2D tex = NewTex(size, size);
        Color[] px    = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;
                bool bar1 = nx > 0.22f && nx < 0.42f && ny > 0.2f && ny < 0.8f;
                bool bar2 = nx > 0.58f && nx < 0.78f && ny > 0.2f && ny < 0.8f;
                px[y * size + x] = (bar1 || bar2) ? Color.white : Color.clear;
            }

        return Apply(tex, px, size, size);
    }

    // ── White square (UI background fallback) ─────────────────────────────
    public static Sprite MakeWhiteSquare()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] px    = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // ══ Helpers ═══════════════════════════════════════════════════════════

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static Sprite Apply(Texture2D tex, Color[] px, int w, int h)
    {
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                             Mathf.Max(w, h));
    }

    static void FillCircle(Color[] px, int tw, int th, int cx, int cy, int r, Color col)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r)
                {
                    int xi = cx + dx, yi = cy + dy;
                    if (xi >= 0 && xi < tw && yi >= 0 && yi < th)
                        px[yi * tw + xi] = col;
                }
    }
}
