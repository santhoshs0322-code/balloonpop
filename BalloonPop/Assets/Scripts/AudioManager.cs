using UnityEngine;

/// <summary>
/// Centralised audio manager.
/// Generates procedural sound effects from math (no AudioClip assets needed).
/// Drop real AudioClips into the public fields and they will be preferred over
/// the generated ones if assigned.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Optional: assign real clips in Inspector ───────────────────────────
    public AudioClip popSound;
    public AudioClip bombSound;
    public AudioClip heartSound;
    public AudioClip specialSound;
    public AudioClip buttonClickSound;
    public AudioClip levelUpSound;
    public AudioClip gameOverSound;
    public AudioClip backgroundMusic;

    [Range(0f, 1f)] public float musicVolume = 0.35f;

    // ── Procedural clips (generated at startup) ────────────────────────────
    private AudioClip _procPop;
    private AudioClip _procBomb;
    private AudioClip _procHeart;
    private AudioClip _procSpecial;
    private AudioClip _procClick;
    private AudioClip _procLevelUp;
    private AudioClip _procGameOver;

    // ── Sources ────────────────────────────────────────────────────────────
    private AudioSource _sfx;
    private AudioSource _music;

    private const int SampleRate = 44100;

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx   = gameObject.AddComponent<AudioSource>();
        _music = gameObject.AddComponent<AudioSource>();
        _music.loop   = true;
        _music.volume = musicVolume;

        GenerateProceduralClips();
    }

    void OnEnable()
    {
        GameManager.OnGameStart    += OnGameStart;
        GameManager.OnGameOver     += OnGameOver;
        GameManager.OnLevelChanged += OnLevelUp;
    }

    void OnDisable()
    {
        GameManager.OnGameStart    -= OnGameStart;
        GameManager.OnGameOver     -= OnGameOver;
        GameManager.OnLevelChanged -= OnLevelUp;
    }

    // ── Public API ──────────────────────────────────────────────────────────
    public void PlayPop(BalloonType type)
    {
        switch (type)
        {
            case BalloonType.Bomb:    Play(bombSound    ?? _procBomb);    break;
            case BalloonType.Heart:   Play(heartSound   ?? _procHeart);   break;
            case BalloonType.Gold:
            case BalloonType.Rainbow: Play(specialSound ?? _procSpecial); break;
            default:                  Play(popSound     ?? _procPop);     break;
        }
    }

    public void PlayButtonClick() => Play(buttonClickSound ?? _procClick);
    public void PlayLevelUp()     => Play(levelUpSound     ?? _procLevelUp);

    public void SetMusicEnabled(bool on)
    {
        if (_music == null) return;
        if (on && !_music.isPlaying && backgroundMusic != null) _music.Play();
        else if (!on) _music.Stop();
    }

    // ── Private ─────────────────────────────────────────────────────────────
    void Play(AudioClip clip, float vol = 1f)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, vol);
    }

    void OnGameStart()
    {
        if (backgroundMusic != null && !_music.isPlaying)
        {
            _music.clip = backgroundMusic;
            _music.Play();
        }
    }

    void OnGameOver()
    {
        Play(gameOverSound ?? _procGameOver);
        _music.Stop();
    }

    void OnLevelUp(int _) => PlayLevelUp();

    // ══ Procedural audio generation ══════════════════════════════════════════

    void GenerateProceduralClips()
    {
        _procPop      = MakePop(600f,  0.08f);
        _procBomb     = MakeBoom(0.22f);
        _procHeart    = MakeChime(880f, 0.18f);
        _procSpecial  = MakeChime(1047f,0.22f);
        _procClick    = MakePop(900f,  0.04f);
        _procLevelUp  = MakeFanfare();
        _procGameOver = MakeDescend();
    }

    // ── Short pop (frequency sweep down) ──────────────────────────────────
    static AudioClip MakePop(float startFreq, float dur)
    {
        int    samples = Mathf.RoundToInt(SampleRate * dur);
        float[] data   = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, startFreq * 0.4f, t);
            float env  = Mathf.Exp(-t * 18f);
            data[i]    = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * env * 0.7f;
        }

        return MakeClip("Pop", data);
    }

    // ── Explosion noise burst ──────────────────────────────────────────────
    static AudioClip MakeBoom(float dur)
    {
        int    samples = Mathf.RoundToInt(SampleRate * dur);
        float[] data   = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / samples;
            float env = Mathf.Exp(-t * 10f);
            data[i]   = Random.Range(-1f, 1f) * env * 0.9f;
        }

        return MakeClip("Boom", data);
    }

    // ── Bell / chime ───────────────────────────────────────────────────────
    static AudioClip MakeChime(float freq, float dur)
    {
        int    samples = Mathf.RoundToInt(SampleRate * dur);
        float[] data   = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / samples;
            float env = Mathf.Exp(-t * 7f);
            // Fundamental + 2nd harmonic
            data[i]   = (Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * 0.6f
                       + Mathf.Sin(2f * Mathf.PI * freq * 2f * i / SampleRate) * 0.3f) * env;
        }

        return MakeClip("Chime", data);
    }

    // ── Rising fanfare (level up) ──────────────────────────────────────────
    static AudioClip MakeFanfare()
    {
        float[] notes  = { 523f, 659f, 784f, 1047f };
        float   noteDur = 0.10f;
        int     noteSamples = Mathf.RoundToInt(SampleRate * noteDur);
        int     total   = noteSamples * notes.Length;
        float[] data    = new float[total];

        for (int n = 0; n < notes.Length; n++)
        {
            for (int i = 0; i < noteSamples; i++)
            {
                float t   = (float)i / noteSamples;
                float env = Mathf.Sin(Mathf.PI * t);
                data[n * noteSamples + i] =
                    Mathf.Sin(2f * Mathf.PI * notes[n] * i / SampleRate) * env * 0.6f;
            }
        }

        return MakeClip("Fanfare", data);
    }

    // ── Descending tone (game over) ────────────────────────────────────────
    static AudioClip MakeDescend()
    {
        float dur     = 0.6f;
        int   samples = Mathf.RoundToInt(SampleRate * dur);
        float[] data  = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / samples;
            float freq = Mathf.Lerp(500f, 150f, t);
            float env  = Mathf.Exp(-t * 3f);
            data[i]    = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * env * 0.65f;
        }

        return MakeClip("Descend", data);
    }

    // ── Helper: wrap float[] into AudioClip ────────────────────────────────
    static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
