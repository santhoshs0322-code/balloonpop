using UnityEngine;

/// <summary>
/// Central game controller: score, timer, lives, levels, best score.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────
    public static event System.Action<int>   OnScoreChanged;
    public static event System.Action<float> OnTimeChanged;
    public static event System.Action<int>   OnLevelChanged;
    public static event System.Action<int>   OnLivesChanged;
    public static event System.Action        OnGameOver;
    public static event System.Action        OnGameStart;

    // ── Settings ──────────────────────────────────────────────────────────
    public float startTime          = 60f;
    public int   startLives         = 3;
    public int   maxLives           = 5;
    public int   pointsPerLevel     = 150;
    public float speedIncreasePerLevel = 0.12f;

    // ── State ─────────────────────────────────────────────────────────────
    public int   Score     { get; private set; }
    public int   BestScore { get; private set; }
    public float TimeLeft  { get; private set; }
    public int   Level     { get; private set; } = 1;
    public int   Lives     { get; private set; }
    public bool  IsPlaying { get; private set; }
    public bool  IsPaused  { get; private set; }
    public bool IsUltimate { get; private set; }
    public int Pops { get; private set; }
    public int BestStreak { get; private set; }
    public bool Cleared => !IsUltimate && Score >= LevelManager.GetClearScore(Level);
    public void StartUltimate() { IsUltimate = true; StartGame(); }
    public void StartAdventure() { IsUltimate = false; StartGame(); }
    public void RecordPop(int streak) { Pops++; BestStreak = Mathf.Max(BestStreak, streak); }

    private const string BestKey = "BestScore";

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BestScore = PlayerPrefs.GetInt(BestKey, 0);
    }

    void Update()
    {
        if (!IsPlaying || IsPaused) return;
        if (IsUltimate) return;
        TimeLeft -= Time.deltaTime;
        OnTimeChanged?.Invoke(TimeLeft);
        if (TimeLeft <= 0f) { TimeLeft = 0f; TriggerGameOver(); }
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void StartGame()
    {
        Score    = 0;
        Time.timeScale = 1f;
        Pops = BestStreak = 0;
        Balloon.ResetStreak();
        Level    = IsUltimate ? 1 : LevelManager.CurrentLevel;
        Lives    = startLives;
        TimeLeft = LevelManager.GetLevelTime(Level);
        IsPlaying = true;
        IsPaused  = false;
        OnScoreChanged?.Invoke(Score);
        OnTimeChanged?.Invoke(TimeLeft);
        OnLevelChanged?.Invoke(Level);
        OnLivesChanged?.Invoke(Lives);
        OnGameStart?.Invoke();
    }

    public void AddScore(int pts)
    {
        Score = Mathf.Max(0, Score + pts);
        OnScoreChanged?.Invoke(Score);
        if (IsUltimate)
        {
            int newLvl = Mathf.Max(1, Score / pointsPerLevel + 1);
            if (newLvl > Level) { Level = newLvl; OnLevelChanged?.Invoke(Level); }
        }
    }

    public void AddTime(float secs)
    {
        TimeLeft += secs;
        OnTimeChanged?.Invoke(TimeLeft);
    }

    public void LoseLife()
    {
        Lives = Mathf.Max(0, Lives - 1);
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0) TriggerGameOver();
    }

    public void AddLife()
    {
        Lives = Mathf.Min(maxLives, Lives + 1);
        OnLivesChanged?.Invoke(Lives);
    }

    public void TogglePause()
    {
        if (!IsPlaying) return;
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    public float GetSpeedMultiplier() =>
        LevelManager.GetLevelSpeed(Level);

    public bool BombsEnabled()    => Level >= 10;
    public bool SpecialsEnabled() => Level >= 5;

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Reset state and fire OnGameStart so UIManager rebuilds the HUD
        StartGame();
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        IsPlaying = false;
        IsPaused  = false;
        // Tell UI to go back to home — no scene reload needed
        OnGoToMenu?.Invoke();
    }

    public static event System.Action OnGoToMenu;

    void TriggerGameOver()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        IsPaused = false;
        Time.timeScale = 1f;
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestKey, BestScore);
            PlayerPrefs.Save();
        }
        // Save level stars and unlock next
        if (!IsUltimate) LevelManager.SaveResult(LevelManager.CurrentLevel, Score);
        OnGameOver?.Invoke();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && IsPlaying && !IsPaused)
        {
            TogglePause();
            UIManager.Instance?.ShowPause();
        }
    }
}
