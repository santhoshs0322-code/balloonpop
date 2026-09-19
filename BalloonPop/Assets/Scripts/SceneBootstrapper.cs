using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-bootstraps the game every time a scene loads.
/// Works without any GameObject in the scene.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    // ── Auto-run on every scene load ───────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        // Always create a fresh bootstrapper for this scene
        if (FindObjectOfType<SceneBootstrapper>() != null) return;
        new GameObject("Bootstrapper").AddComponent<SceneBootstrapper>();
    }

    void Awake()
    {
        // Register for future scene loads so Home button always works
        SceneManager.sceneLoaded += OnSceneLoaded;
        Boot();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Boot();
    }

    // ── Main build ─────────────────────────────────────────────────────────
    void Boot()
    {
        // If managers already exist (DontDestroyOnLoad survivors), skip them
        bool hasCamera  = Camera.main != null && Camera.main.orthographic;
        bool hasGM      = GameManager.Instance != null;
        bool hasUI      = UIManager.Instance    != null;
        bool hasBG      = FindObjectOfType<KidsBackground>() != null;
        bool hasSpawner = FindObjectOfType<BalloonSpawner>() != null;

        if (!hasCamera)  SetupCamera();
        else             ReconfigCamera();

        if (!hasBG)      new GameObject("KidsBackground").AddComponent<KidsBackground>();
        if (!hasGM)      new GameObject("GameManager").AddComponent<GameManager>();
        if (!FindObjectOfType<AudioManager>())
                         new GameObject("AudioManager").AddComponent<AudioManager>();
        if (!FindObjectOfType<AdManager>())
                         new GameObject("AdManager").AddComponent<AdManager>();
        if (!FindObjectOfType<AuthManager>())
                         new GameObject("AuthManager").AddComponent<AuthManager>();
        if (!hasSpawner) new GameObject("BalloonSpawner").AddComponent<BalloonSpawner>();
        if (!hasUI)      new GameObject("UIManager").AddComponent<UIManager>();

        // Resume timescale (in case it was paused)
        Time.timeScale = 1f;

        Debug.Log("[SceneBootstrapper] Scene ready.");
    }

    // ── Camera ─────────────────────────────────────────────────────────────
    void SetupCamera()
    {
        var go  = new GameObject("Main Camera");
        go.tag  = "MainCamera";
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        ConfigureCam(cam);
    }

    void ReconfigCamera()
    {
        ConfigureCam(Camera.main);
    }

    static void ConfigureCam(Camera cam)
    {
        cam.orthographic      = true;
        cam.orthographicSize  = 5f;
        cam.clearFlags        = CameraClearFlags.SolidColor;
        // Deep indigo fallback; the procedural backdrop adds the atmospheric layers.
        cam.backgroundColor   = new Color(0.025f, 0.035f, 0.12f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.nearClipPlane     = 0.1f;
        cam.farClipPlane      = 100f;
    }
}
