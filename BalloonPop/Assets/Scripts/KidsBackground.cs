using UnityEngine;

/// <summary>
/// Builds a fully animated kids-friendly sky background:
///   - Bright sky blue gradient
///   - Rolling green hills at bottom
///   - Animated sun with rotating rays
///   - Fluffy white clouds drifting across
///   - Hot air balloons floating up
///   - Aeroplanes flying across
///   - Birds (V-shape) flapping and flying
///   - Rainbow arc
/// All drawn with procedural sprites — no art assets needed.
/// </summary>
public class KidsBackground : MonoBehaviour
{
    // ── Spawn settings ─────────────────────────────────────────────────────
    [Header("Clouds")]
    public int   cloudCount     = 5;
    public float cloudMinSpeed  = 0.4f;
    public float cloudMaxSpeed  = 0.9f;

    [Header("Hot Air Balloons")]
    public int   hotBalloonCount = 0;
    public float hbMinSpeed      = 0.25f;
    public float hbMaxSpeed      = 0.55f;

    [Header("Aeroplanes")]
    public int   planeCount     = 0;
    public float planeMinSpeed  = 1.2f;
    public float planeMaxSpeed  = 2.2f;

    [Header("Birds")]
    public int   birdCount      = 0;
    public float birdMinSpeed   = 0.6f;
    public float birdMaxSpeed   = 1.4f;

    // ── Cached camera bounds ───────────────────────────────────────────────
    private float _camH, _camW;
    private Camera _cam;

    // ── Sprite cache ───────────────────────────────────────────────────────
    private Sprite _cloudSpr;
    private Sprite _planeSpr;
    private Sprite _birdSpr;

    // ── Sun reference ──────────────────────────────────────────────────────
    private Transform _sunRays;
    private bool _usingPremiumSky;

    void Awake()
    {
        _cam  = Camera.main;
        _camH = _cam.orthographicSize;
        _camW = _camH * _cam.aspect;

        // Pre-generate sprites
        _cloudSpr = MakeCloudSprite();
        _planeSpr = MakePlaneSprite();
        _birdSpr  = MakeBirdSprite();

        BuildSky();
        if (!_usingPremiumSky)
        {
            BuildStars();
            BuildHills();
        }
        SpawnClouds();
        SpawnHotAirBalloons();
        SpawnPlanes();
        SpawnBirds();
    }

    void Start() { } // intentionally empty — all built in Awake

    void Update()
    {
        // Rotate sun rays
        if (_sunRays != null)
            _sunRays.Rotate(0, 0, -12f * Time.deltaTime);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SKY GRADIENT
    // ══════════════════════════════════════════════════════════════════════
    void BuildSky()
    {
        // Premium authored art is used when present; the procedural sky below is a safe fallback.
        Sprite premiumSky = Resources.Load<Sprite>("Premium/twilight-sky-background");
        if (premiumSky != null && PlayerPrefs.GetInt("BackgroundStyle", 0) == 0)
        {
            float scale = Mathf.Max((_camW * 2.05f) / premiumSky.bounds.size.x,
                                    (_camH * 2.05f) / premiumSky.bounds.size.y);
            SpriteSR("PremiumTwilightSky", premiumSky, 0f, 0f, -15,
                Vector3.one * scale);
            _usingPremiumSky = true;
            return;
        }

        // Rich, low-contrast twilight gradient keeps the playfield calm and premium.
        SpriteSR("Sky", MakeGradientSprite(32, 256,
            new Color(0.025f, 0.035f, 0.13f),
            new Color(0.15f, 0.075f, 0.28f)),
            0f, 0f, -15, new Vector3(_camW * 2.2f, _camH * 2.2f, 1f));

        // A soft aurora band gives depth without competing with tappable balloons.
        SpriteSR("AuroraGlow", MakeGradientSprite(32, 128,
            new Color(0.18f, 0.12f, 0.42f, 0f),
            new Color(0.18f, 0.72f, 0.78f, 0.22f)),
            0f, -_camH * 0.18f, -14, new Vector3(_camW * 2.1f, _camH * 0.62f, 1f));
    }

    /// <summary>Rebuilds only the backdrop after the player changes its style.</summary>
    public static void RefreshTheme()
    {
        var existing = FindObjectOfType<KidsBackground>();
        if (existing != null) Destroy(existing.gameObject);
        new GameObject("KidsBackground").AddComponent<KidsBackground>();
    }

    void BuildStars()
    {
        for (int i = 0; i < 54; i++)
        {
            // Deterministic placement keeps the backdrop stable and leaves gameplay RNG alone.
            float a = i * 12.9898f;
            float x = Mathf.Lerp(-_camW * 1.05f, _camW * 1.05f, Mathf.Repeat(Mathf.Sin(a) * 43758.5f, 1f));
            float y = Mathf.Lerp(-_camH * 0.15f, _camH * 1.02f, Mathf.Repeat(Mathf.Sin(a * 1.71f) * 24634.6f, 1f));
            float s = Mathf.Lerp(0.025f, 0.075f, Mathf.Repeat(Mathf.Sin(a * 2.13f) * 13579.1f, 1f));
            Color c = Color.Lerp(new Color(0.48f, 0.84f, 1f, 0.35f), Color.white,
                Mathf.Repeat(Mathf.Sin(a * 0.77f) * 9381.3f, 1f));
            SpriteSR("Starfield_" + i, MakeCircleSprite(32, c), x, y, -13, Vector3.one * s);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ROLLING GREEN HILLS AT BOTTOM
    // ══════════════════════════════════════════════════════════════════════
    void BuildHills()
    {
        // Layered silhouette hills frame the playfield without the old cartoon look.
        SpriteSR("Ground", MakeSolidSprite(new Color(0.035f, 0.055f, 0.13f)),
            0f, -_camH - 0.5f, -6, new Vector3(_camW * 2.5f, 2.5f, 1f));

        // 3 rounded hills
        var hillCols = new Color[] {
            new Color(0.055f, 0.09f, 0.19f),
            new Color(0.075f, 0.14f, 0.25f),
            new Color(0.10f, 0.20f, 0.30f),
        };
        float[] xPos   = { -_camW * 0.55f, 0f, _camW * 0.55f };
        float[] yPos   = { -_camH + 0.5f, -_camH + 0.8f, -_camH + 0.3f };
        float[] scales = { 4.5f, 5.5f, 4.0f };

        for (int i = 0; i < 3; i++)
        {
            var hill = SpriteSR("Hill" + i,
                MakeCircleSprite(128, hillCols[i]),
                xPos[i], yPos[i], -7, Vector3.one * scales[i]);
            hill.transform.localScale = new Vector3(scales[i] * 1.8f, scales[i] * 0.7f, 1f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  RAINBOW ARC
    // ══════════════════════════════════════════════════════════════════════
    void BuildRainbow()
    {
        Color[] rCols = {
            new Color(1f,0.15f,0.15f,0.65f), new Color(1f,0.55f,0.05f,0.65f),
            new Color(1f,0.95f,0.10f,0.65f), new Color(0.15f,0.85f,0.15f,0.65f),
            new Color(0.10f,0.40f,1.00f,0.65f), new Color(0.55f,0.05f,1.00f,0.65f),
        };

        for (int i = 0; i < rCols.Length; i++)
        {
            float s = 6.5f + i * 0.55f;
            var arc = SpriteSR("Rainbow" + i,
                MakeRainbowArcSprite(128, rCols[i]),
                0f, -_camH * 0.5f, -9, Vector3.one * s);
            arc.transform.localScale = new Vector3(s * 1.6f, s * 0.8f, 1f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SUN
    // ══════════════════════════════════════════════════════════════════════
    void BuildSun()
    {
        float sx = _camW * 0.7f, sy = _camH * 0.75f;

        // Glow halo
        SpriteSR("SunGlow", MakeCircleSprite(128, new Color(1f, 0.95f, 0.5f, 0.3f)),
            sx, sy, -12, Vector3.one * 2.8f);

        // Rotating rays parent
        var raysGO = new GameObject("SunRays");
        raysGO.transform.position = new Vector3(sx, sy, 0f);
        _sunRays = raysGO.transform;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            var ray = SpriteSR("Ray" + i,
                MakeRaySprite(new Color(1f, 0.92f, 0.35f, 0.85f)),
                0f, 0f, -11, new Vector3(0.25f, 1.2f, 1f));
            ray.transform.SetParent(raysGO.transform, false);
            ray.transform.localPosition = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * 0.85f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * 0.85f, 0f);
            ray.transform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        // Sun circle
        SpriteSR("Sun", MakeCircleSprite(128, new Color(1f, 0.92f, 0.22f)),
            sx, sy, -10, Vector3.one * 1.4f);
        SpriteSR("SunFace", MakeCircleSprite(64, new Color(1f, 0.82f, 0.1f)),
            sx, sy, -9, Vector3.one * 1.2f);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CLOUDS
    // ══════════════════════════════════════════════════════════════════════
    void SpawnClouds()
    {
        for (int i = 0; i < cloudCount; i++)
        {
            float y     = Random.Range(-_camH * 0.1f, _camH * 0.85f);
            float x     = Random.Range(-_camW, _camW);
            float scale = Random.Range(0.8f, 1.8f);
            float speed = Random.Range(cloudMinSpeed, cloudMaxSpeed);

            var go = SpriteSR("Cloud" + i, _cloudSpr, x, y, -8,
                Vector3.one * scale);
            go.GetComponent<SpriteRenderer>().color = new Color(0.48f, 0.68f, 1f, 0.13f);
            go.AddComponent<ScrollRight>().Init(speed, _camW, y);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HOT AIR BALLOONS
    // ══════════════════════════════════════════════════════════════════════
    void SpawnHotAirBalloons()
    {
        Color[] cols = {
            new Color(1f,0.25f,0.25f), new Color(0.25f,0.6f,1f),
            new Color(0.25f,0.85f,0.35f), new Color(1f,0.75f,0.1f),
        };
        for (int i = 0; i < hotBalloonCount; i++)
        {
            float x = Random.Range(-_camW * 0.9f, _camW * 0.9f);
            float y = Random.Range(-_camH * 0.6f, -_camH * 0.1f);
            float speed = Random.Range(hbMinSpeed, hbMaxSpeed);

            Color col = cols[i % cols.Length];

            var parent = new GameObject("HotAirBalloon" + i);
            parent.transform.position = new Vector3(x, y, 0f);

            // Balloon envelope (big coloured oval)
            var env = SpriteSR("Env", MakeHotAirBalloonSprite(128, col), 0f, 0.9f, -5,
                Vector3.one * 1.1f);
            env.transform.SetParent(parent.transform, false);

            // Basket (small brown rect)
            var basket = SpriteSR("Basket",
                MakeSolidRectSprite(new Color(0.6f, 0.38f, 0.15f), 40, 28),
                0f, 0f, -5, Vector3.one);
            basket.transform.SetParent(parent.transform, false);

            // Ropes (two thin lines)
            for (int r = 0; r < 2; r++)
            {
                var rope = SpriteSR("Rope" + r,
                    MakeSolidRectSprite(new Color(0.5f, 0.35f, 0.18f), 4, 40),
                    -0.15f + r * 0.3f, 0.45f, -5, Vector3.one);
                rope.transform.SetParent(parent.transform, false);
            }

            // Stripe decorations on envelope
            for (int s = 0; s < 4; s++)
            {
                float angle = 30f + s * 30f;
                Color sc = Color.Lerp(col, Color.white, 0.55f);
                var stripe = SpriteSR("Stripe" + s,
                    MakeSolidRectSprite(sc, 8, 80), 0f, 0.9f, -4,
                    new Vector3(0.08f, 0.8f, 1f));
                stripe.transform.SetParent(env.transform, false);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            parent.AddComponent<FloatUp>().Init(speed, _camH, x);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  AEROPLANES
    // ══════════════════════════════════════════════════════════════════════
    void SpawnPlanes()
    {
        for (int i = 0; i < planeCount; i++)
        {
            float y     = Random.Range(_camH * 0.2f, _camH * 0.85f);
            float x     = -_camW - 2f;
            float speed = Random.Range(planeMinSpeed, planeMaxSpeed);

            var go = SpriteSR("Plane" + i, _planeSpr, x, y, -4,
                Vector3.one * (0.6f + i * 0.15f));
            var sc = go.AddComponent<ScrollRight>();
            sc.Init(speed, _camW, y);
            sc.startLeft = true;

            // Smoke trail (4 small circles fading behind)
            for (int s = 0; s < 4; s++)
            {
                float alpha = 0.4f - s * 0.08f;
                var smoke = SpriteSR("Smoke" + s,
                    MakeCircleSprite(32, new Color(1f, 1f, 1f, alpha)),
                    -(s + 1) * 0.35f, 0f, -5, Vector3.one * (0.18f + s * 0.04f));
                smoke.transform.SetParent(go.transform, false);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BIRDS
    // ══════════════════════════════════════════════════════════════════════
    void SpawnBirds()
    {
        for (int i = 0; i < birdCount; i++)
        {
            float y     = Random.Range(_camH * 0.3f, _camH * 0.9f);
            float x     = Random.Range(-_camW, _camW);
            float speed = Random.Range(birdMinSpeed, birdMaxSpeed);
            float scale = Random.Range(0.18f, 0.32f);

            var go = SpriteSR("Bird" + i, _birdSpr, x, y, -3,
                Vector3.one * scale);
            var sc = go.AddComponent<ScrollRight>();
            sc.Init(speed, _camW, y);
            sc.startLeft = Random.value > 0.5f;

            go.AddComponent<BirdFlap>();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SPRITE FACTORY METHODS
    // ══════════════════════════════════════════════════════════════════════

    static Sprite MakeCircleSprite(int size, Color col)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.46f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx*dx+dy*dy);
                Color c  = col;
                c.a = col.a * Mathf.Clamp01((r - d) / 2f);
                px[y*size+x] = c;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
    }

    static Sprite MakeGradientSprite(int w, int h, Color top, Color bot)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px  = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bot, top, (float)y / h);
            for (int x = 0; x < w; x++) px[y*w+x] = c;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f), Mathf.Max(w,h));
    }

    static Sprite MakeSolidSprite(Color col)
    {
        var tex = new Texture2D(4, 4);
        var px  = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = col;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 4f);
    }

    static Sprite MakeSolidRectSprite(Color col, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px  = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = col;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f), Mathf.Max(w,h));
    }

    // Fluffy cloud: 5 overlapping soft circles
    static Sprite MakeCloudSprite()
    {
        int W = 256, H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[W * H];

        // Cloud blob centres (normalised 0-1)
        float[] bx = { 0.20f, 0.38f, 0.55f, 0.70f, 0.45f };
        float[] by = { 0.45f, 0.35f, 0.30f, 0.45f, 0.62f };
        float[] br = { 0.16f, 0.20f, 0.22f, 0.16f, 0.14f };

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = (float)x / W, ny = (float)y / H;
                float maxA = 0f;
                for (int b = 0; b < bx.Length; b++)
                {
                    float dx = nx - bx[b], dy = ny - by[b];
                    float d  = Mathf.Sqrt(dx*dx + dy*dy);
                    float a  = Mathf.Clamp01((br[b] - d) / (br[b] * 0.4f));
                    maxA = Mathf.Max(maxA, a);
                }
                px[y*W+x] = new Color(1f, 1f, 1f, maxA);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f,0.5f), W);
    }

    // Hot air balloon envelope
    static Sprite MakeHotAirBalloonSprite(int size, Color col)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];
        float cx = size*0.5f, cy = size*0.52f, rx = size*0.44f, ry = size*0.46f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx)/rx, dy = (y - cy)/ry;
                float d  = dx*dx + dy*dy;
                if (d <= 1f)
                {
                    // Colour stripes
                    float angle = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f;
                    int   stripe = Mathf.FloorToInt(angle * 8) % 2;
                    Color c = stripe == 0 ? col : Color.Lerp(col, Color.white, 0.45f);
                    // Gloss
                    float gx = (x - cx*0.65f)/(rx*0.4f), gy = (y - cy*1.3f)/(ry*0.4f);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - gx*gx - gy*gy)*0.5f);
                    c.a = Mathf.Clamp01((1f - d) * 5f);
                    px[y*size+x] = c;
                }
                else px[y*size+x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
    }

    // Simple aeroplane silhouette
    static Sprite MakePlaneSprite()
    {
        int W = 256, H = 80;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[W * H];
        Color white = Color.white;

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = (float)x / W, ny = (float)y / H;
                bool body      = nx > 0.05f && nx < 0.90f &&
                                 ny > 0.38f && ny < 0.62f;
                bool nose      = nx >= 0.88f && ny > 0.44f && ny < 0.56f &&
                                 (nx - 0.88f) > Mathf.Abs(ny - 0.5f) * 2.5f;
                bool wing      = nx > 0.25f && nx < 0.65f &&
                                 ny > 0.15f && ny < 0.85f &&
                                 Mathf.Abs(ny - 0.5f) > 0.13f;
                bool tailFin   = nx > 0.06f && nx < 0.24f &&
                                 ny > 0.58f && ny < 0.88f;
                bool window    = nx > 0.45f && nx < 0.55f &&
                                 ny > 0.42f && ny < 0.58f;

                if (body || nose || wing || tailFin)
                    px[y*W+x] = window ? new Color(0.55f, 0.85f, 1f) : white;
                else
                    px[y*W+x] = Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f,0.5f), W);
    }

    // Bird V-shape
    static Sprite MakeBirdSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size - 0.5f;
                float ny = (float)y / size - 0.5f;
                // Two curved wings forming a V
                float wing1 = Mathf.Abs(ny - (-Mathf.Abs(nx) * 0.55f));
                float wing2 = Mathf.Abs(ny - (-Mathf.Abs(nx) * 0.55f + 0.06f));
                bool onWing = wing1 < 0.045f && Mathf.Abs(nx) < 0.45f;
                px[y*size+x] = onWing
                    ? new Color(0.15f, 0.12f, 0.08f, Mathf.Clamp01(1f - wing1 / 0.045f))
                    : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
    }

    // Single ray/spike
    static Sprite MakeRaySprite(Color col)
    {
        int W = 16, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px  = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = Mathf.Abs((float)x/W - 0.5f) * 2f;
                float ny = (float)y / H;
                float a  = Mathf.Clamp01((1f - nx) * (1f - ny * 0.6f));
                Color c  = col; c.a *= a;
                px[y*W+x] = c;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f,0f), W);
    }

    // Rainbow arc — top half of ring
    static Sprite MakeRainbowArcSprite(int size, Color col)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];
        float cx = size*0.5f, cy = size*0.1f;
        float rOuter = size*0.48f, rInner = size*0.40f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d  = Mathf.Sqrt(dx*dx + dy*dy);
                bool  arc = d >= rInner && d <= rOuter && dy >= 0f;
                px[y*size+x] = arc ? col : Color.clear;
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.1f), size);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPER: create a SpriteRenderer in the scene — returns GameObject
    // ══════════════════════════════════════════════════════════════════════
    GameObject SpriteSR(string name, Sprite spr,
        float x, float y, int order, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position   = new Vector3(x, y, 0f);
        go.transform.localScale = scale;
        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = spr;
        sr.sortingOrder = order;
        return go;
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  MOVEMENT BEHAVIOURS
// ══════════════════════════════════════════════════════════════════════════

/// <summary>Scrolls an object from left to right (or right to left) and wraps.</summary>
public class ScrollRight : MonoBehaviour
{
    public bool startLeft = false;

    private float _speed;
    private float _wrapX;
    private float _y;
    private float _dir = 1f;

    public void Init(float speed, float halfW, float y)
    {
        _speed = speed;
        _wrapX = halfW + 3f;
        _y     = y;
    }

    void Start()
    {
        _dir = startLeft ? -1f : 1f;
        // Flip sprite if going left
        if (_dir < 0f)
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
    }

    void Update()
    {
        transform.position += Vector3.right * _speed * _dir * Time.deltaTime;

        // Wrap around
        if (_dir > 0f && transform.position.x > _wrapX)
        {
            transform.position = new Vector3(-_wrapX,
                Random.Range(_y - 0.5f, _y + 0.5f), 0f);
        }
        else if (_dir < 0f && transform.position.x < -_wrapX)
        {
            transform.position = new Vector3(_wrapX,
                Random.Range(_y - 0.5f, _y + 0.5f), 0f);
        }
    }
}

/// <summary>Floats an object upward and wraps back to the bottom.</summary>
public class FloatUp : MonoBehaviour
{
    private float _speed;
    private float _topY;
    private float _botY;
    private float _startX;

    public void Init(float speed, float camH, float startX)
    {
        _speed  = speed;
        _topY   = camH + 3f;
        _botY   = -camH - 2f;
        _startX = startX;
    }

    void Update()
    {
        transform.position += Vector3.up * _speed * Time.deltaTime;
        // Gentle sway
        float sway = Mathf.Sin(Time.time * 0.4f + _startX) * 0.4f;
        Vector3 p = transform.position;
        p.x = _startX + sway;
        transform.position = p;

        if (transform.position.y > _topY)
        {
            Camera cam = Camera.main;
            float halfW = cam.orthographicSize * cam.aspect;
            _startX = Random.Range(-halfW * 0.85f, halfW * 0.85f);
            transform.position = new Vector3(_startX, _botY, 0f);
        }
    }
}

/// <summary>Makes a bird flap its wings by scaling Y up and down.</summary>
public class BirdFlap : MonoBehaviour
{
    private float _offset;
    void Start() => _offset = Random.Range(0f, Mathf.PI * 2f);
    void Update()
    {
        float flap = Mathf.Sin(Time.time * 5f + _offset);
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y) * (0.6f + flap * 0.4f),
            1f);
    }
}
