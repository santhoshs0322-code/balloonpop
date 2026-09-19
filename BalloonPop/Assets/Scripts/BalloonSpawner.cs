using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns balloons procedurally. Self-contained — builds its own prefab
/// via BalloonPrefabFactory. No Inspector assignments needed.
/// </summary>
public class BalloonSpawner : MonoBehaviour
{
    // ── Tuning ─────────────────────────────────────────────────────────────
    public float baseSpawnInterval      = 1.2f;
    public float minSpawnInterval       = 0.28f;
    public float intervalDecreasePerLevel = 0.07f;
    public int   maxBalloonsOnScreen    = 22;
    public float spawnYOffset           = 1.2f;   // below the bottom camera edge
    public float horizontalPadding      = 0.6f;

    // ── Runtime ────────────────────────────────────────────────────────────
    private GameObject        _balloonTemplate;
    private float             _spawnTimer;
    private float             _currentInterval;
    private Camera            _cam;
    private List<GameObject>  _active = new List<GameObject>();

    /// <summary>Rebuilds the template after the player changes balloon style.</summary>
    public static void RefreshBalloonStyle()
    {
        var spawner = FindObjectOfType<BalloonSpawner>();
        if (spawner == null) return;
        if (spawner._balloonTemplate != null) Destroy(spawner._balloonTemplate);
        spawner._balloonTemplate = BalloonPrefabFactory.Build();
    }

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = Camera.main;
        Balloon.DestroyY = _cam.orthographicSize + 1.5f;

        // Build balloon prefab entirely in code
        _balloonTemplate = BalloonPrefabFactory.Build();
    }

    void OnEnable()
    {
        GameManager.OnGameStart    += OnGameStart;
        GameManager.OnGameOver     += OnGameOver;
        GameManager.OnLevelChanged += OnLevelChanged;
        GameManager.OnGoToMenu += OnGameOver;
    }

    void OnDisable()
    {
        GameManager.OnGameStart    -= OnGameStart;
        GameManager.OnGameOver     -= OnGameOver;
        GameManager.OnLevelChanged -= OnLevelChanged;
        GameManager.OnGoToMenu -= OnGameOver;
    }

    void Start()
    {
        _currentInterval = baseSpawnInterval;
        _spawnTimer      = 0.5f;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        _active.RemoveAll(b => b == null);
        if (_active.Count >= maxBalloonsOnScreen) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnBalloon();
            _spawnTimer = _currentInterval;
        }
    }

    // ── Spawning ────────────────────────────────────────────────────────────
    void SpawnBalloon()
    {
        BalloonType type = PickRandomType();
        Vector3     pos  = RandomSpawnPos();

        // Clone from template
        _balloonTemplate.SetActive(false); // keep off until after Init
        GameObject go = Instantiate(_balloonTemplate, pos, Quaternion.identity);
        go.SetActive(true);

        Balloon b = go.GetComponent<Balloon>();
        b?.Init(type);

        _active.Add(go);
    }

    Vector3 RandomSpawnPos()
    {
        float halfW = _cam.orthographicSize * _cam.aspect - horizontalPadding;
        float botY  = -_cam.orthographicSize - spawnYOffset;
        return new Vector3(Random.Range(-halfW, halfW), botY, 0f);
    }

    BalloonType PickRandomType()
    {
        bool sp = GameManager.Instance.SpecialsEnabled();
        bool bo = GameManager.Instance.BombsEnabled();

        var entries = new List<(BalloonType t, float w)>();
        foreach (BalloonType t in System.Enum.GetValues(typeof(BalloonType)))
        {
            float w = t.GetWeight(sp, bo);
            if (w > 0f) entries.Add((t, w));
        }

        float total = 0f;
        foreach (var e in entries) total += e.w;

        float rand = Random.Range(0f, total);
        float cum  = 0f;
        foreach (var e in entries)
        {
            cum += e.w;
            if (rand <= cum) return e.t;
        }
        return BalloonType.Red;
    }

    // ── Event Handlers ──────────────────────────────────────────────────────
    void OnGameStart()
    {
        foreach (var b in _active) if (b) Destroy(b);
        _active.Clear();
        _currentInterval = Mathf.Max(0.55f, baseSpawnInterval - (GameManager.Instance.Level - 1) * intervalDecreasePerLevel);
        _spawnTimer      = 0.4f;
    }

    void OnGameOver()
    {
        foreach (var b in _active) if (b) Destroy(b);
        _active.Clear();
    }

    void OnLevelChanged(int level)
    {
        _currentInterval = Mathf.Max(0.55f,
            baseSpawnInterval - (level - 1) * intervalDecreasePerLevel);

        if (level >= 3) StartCoroutine(ExtraSpawn());
    }

    IEnumerator ExtraSpawn()
    {
        yield return new WaitForSeconds(0.18f);
        if (GameManager.Instance.IsPlaying && !GameManager.Instance.IsPaused) SpawnBalloon();
    }
}
