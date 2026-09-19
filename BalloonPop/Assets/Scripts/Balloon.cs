using UnityEngine;

/// <summary>
/// Controls a single balloon: floating upward, wobble, tap detection, and pop.
/// All visual data is injected by BalloonPrefabFactory.Build() — no Inspector wiring needed.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Balloon : MonoBehaviour
{
    // ── Set by BalloonPrefabFactory ────────────────────────────────────────
    [HideInInspector] public Sprite[]    typeSprites;
    [HideInInspector] public Color[]     typeColors;
    [HideInInspector] public GameObject  popEffectPrefab;

    // ── Tuning ─────────────────────────────────────────────────────────────
    public float baseSpeed        = 1.6f;
    public float wobbleAmplitude  = 0.28f;
    public float wobbleFrequency  = 1.6f;
    public float punchDuration    = 0.08f;

    // ── State ──────────────────────────────────────────────────────────────
    public  BalloonType    BalloonType { get; private set; }
    public  static float   DestroyY   { get; set; } = 7f;
    public static void ResetStreak() { PopStreak = 0; LastPopTime = -99f; }

    // ── Streak tracking (shared across all balloons) ───────────────────────
    public static int   PopStreak    { get; private set; } = 0;
    public static float LastPopTime  { get; private set; } = -99f;
    private const float StreakWindow = 2.2f; // max gap between pops to keep streak

    private float          _speed;
    private float          _wobbleOffset;
    private float          _startX;
    private bool           _popped;
    private SpriteRenderer _sr;
    private Vector3        _baseScale;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    /// <summary>Called by BalloonSpawner immediately after cloning the template.</summary>
    public void Init(BalloonType type)
    {
        BalloonType   = type;
        _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
        _startX       = transform.position.x;
        _speed        = baseSpeed * (GameManager.Instance?.GetSpeedMultiplier() ?? 1f);
        ApplyVisual(type);
    }

    // ──────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_popped) return;

        transform.position += Vector3.up * _speed * Time.deltaTime;

        float wobble      = Mathf.Sin(Time.time * wobbleFrequency + _wobbleOffset) * wobbleAmplitude;
        Vector3 p         = transform.position;
        p.x               = _startX + wobble;
        transform.position = p;

        if (transform.position.y > DestroyY)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying &&
                GameManager.Instance.IsUltimate && BalloonType != BalloonType.Bomb)
                GameManager.Instance.LoseLife();
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        if (BalloonType == BalloonType.Rainbow && _sr != null)
            _sr.color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.55f, 1f), 1f, 1f);
    }

    // ── Input ──────────────────────────────────────────────────────────────
    void OnMouseDown()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        Pop();
    }

    // ── Pop ────────────────────────────────────────────────────────────────
    public void Pop()
    {
        if (_popped || GameManager.Instance == null || !GameManager.Instance.IsPlaying || GameManager.Instance.IsPaused) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() ||
             (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))) return;
        _popped = true;

        // ── Score / time ──────────────────────────────────────────────────
        if (GameManager.Instance != null)
        {
            int pts = BalloonType.GetPoints();
            if (pts != 0)
            {
                GameManager.Instance.AddScore(pts);
                UIManager.Instance?.ShowScorePopup(pts, transform.position);
            }
            float bonus = BalloonType.GetBonusTime();
            if (bonus > 0f)
            {
                GameManager.Instance.AddTime(bonus);
                if (GameManager.Instance.IsUltimate) GameManager.Instance.AddLife();
                UIManager.Instance?.ShowScorePopup(5, transform.position, GameManager.Instance.IsUltimate ? "+1 heart" : "+5 sec");
            }
        }

        // ── Streak tracking ───────────────────────────────────────────────
        if (BalloonType != BalloonType.Bomb)
        {
            float now = Time.time;
            if (now - LastPopTime <= StreakWindow)
                PopStreak++;
            else
                PopStreak = 1;
            LastPopTime = now;
            GameManager.Instance.RecordPop(PopStreak);

            // Cheer only at streak milestones: 3, 5, 8, 10, 15, 20...
            bool isCheerStreak = PopStreak == 3 || PopStreak == 5 ||
                                 PopStreak == 8 || (PopStreak >= 10 && PopStreak % 5 == 0);
            // Also cheer for special balloons
            bool isSpecial = BalloonType == BalloonType.Gold   ||
                             BalloonType == BalloonType.Rainbow ||
                             BalloonType == BalloonType.Heart;

            if (isCheerStreak || isSpecial)
                UIManager.Instance?.ShowCheer(transform.position, PopStreak);
        }
        else
        {
            // Bomb resets streak
            PopStreak = 0;
        }

        AudioManager.Instance?.PlayPop(BalloonType);

        // ── Blast effect ──────────────────────────────────────────────────
        if (popEffectPrefab != null)
        {
            GameObject fx = Instantiate(popEffectPrefab,
                transform.position, Quaternion.identity);
            fx.SetActive(true);
            var fxSr = fx.GetComponent<SpriteRenderer>();
            if (fxSr != null) fxSr.color = _sr != null ? _sr.color : Color.white;
        }

        StartCoroutine(PopAnimation());
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    void ApplyVisual(BalloonType type)
    {
        int idx = (int)type;

        if (typeSprites != null && idx < typeSprites.Length && typeSprites[idx] != null)
            _sr.sprite = typeSprites[idx];

        _sr.color = Color.white;

        if (type == BalloonType.Bomb)
            transform.localScale = _baseScale * 0.9f;
        else if (type == BalloonType.Gold || type == BalloonType.Rainbow)
            transform.localScale = _baseScale * 1.1f;
    }

    System.Collections.IEnumerator PopAnimation()
    {
        float elapsed = 0f;
        Vector3 big   = _baseScale * 1.5f;
        while (elapsed < punchDuration)
        {
            transform.localScale = Vector3.Lerp(_baseScale, big, elapsed / punchDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
