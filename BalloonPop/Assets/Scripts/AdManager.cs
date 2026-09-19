using UnityEngine;

/// <summary>
/// Ad integration manager — ready for Google AdMob (Unity package).
/// 
/// HOW TO ACTIVATE ADS:
///   1. Install "Google Mobile Ads Unity Plugin" from the Unity Package Manager
///      or download from: https://github.com/googleads/googleads-mobile-unity
///   2. Replace "ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX" with your real App ID.
///   3. Uncomment the #define USE_ADMOB line below.
///   4. Fill in your real Ad Unit IDs.
/// 
/// Until AdMob is integrated the methods simply call the reward callback
/// immediately so you can test the full game loop.
/// </summary>

// Uncomment when AdMob package is installed:
// #define USE_ADMOB

#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

public enum RewardType
{
    ExtraTime,      // +30 seconds (Game Over screen)
    DoubleScore,    // 2× score multiplier for next game
    ContinueGame    // Continue from where you left off
}

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    // ── Ad Unit IDs ───────────────────────────────────────────────────────────
    [Header("AdMob IDs (replace with your real IDs)")]
    public string appId             = "ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX";
    public string rewardedAdUnitId  = "ca-app-pub-3940256099942544/5224354917"; // Test ID
    public string interstitialAdId  = "ca-app-pub-3940256099942544/1033173712"; // Test ID
    public string bannerAdUnitId    = "ca-app-pub-3940256099942544/6300978111"; // Test ID

    [Header("Settings")]
    [Tooltip("Show interstitial every N games.")]
    public int interstitialEveryNGames = 3;

    // ── State ─────────────────────────────────────────────────────────────────
    private int            _gamesPlayed;
    private RewardType     _pendingReward;
    private System.Action  _onRewardGranted;

#if USE_ADMOB
    private RewardedAd     _rewardedAd;
    private InterstitialAd _interstitialAd;
    private BannerView     _bannerView;
#endif

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        GameManager.OnGameOver += HandleGameOver;
    }

    void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
    }

    void Start()
    {
#if USE_ADMOB
        InitAdMob();
#else
        Debug.Log("[AdManager] Running without AdMob. Define USE_ADMOB to enable real ads.");
#endif
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show a rewarded ad. On completion the appropriate reward is granted.</summary>
    public void ShowRewardedAd(RewardType rewardType, System.Action onComplete = null)
    {
        _pendingReward   = rewardType;
        _onRewardGranted = onComplete;

#if USE_ADMOB
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show(OnUserEarnedReward);
        }
        else
        {
            Debug.LogWarning("[AdManager] Rewarded ad not ready. Granting reward directly (dev mode).");
            GrantReward(rewardType);
            LoadRewardedAd();
        }
#else
        // No ads — grant reward immediately for testing
        GrantReward(rewardType);
#endif
    }

    /// <summary>Show interstitial if loaded.</summary>
    public void ShowInterstitialAd()
    {
#if USE_ADMOB
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            LoadInterstitialAd();
        }
#else
        Debug.Log("[AdManager] Interstitial skipped (AdMob not active).");
#endif
    }

    /// <summary>Show or hide the banner.</summary>
    public void ShowBanner(bool show)
    {
#if USE_ADMOB
        if (_bannerView != null)
        {
            if (show) _bannerView.Show();
            else      _bannerView.Hide();
        }
#endif
    }

    // ── AdMob Init (compiled only when USE_ADMOB is defined) ──────────────────

#if USE_ADMOB
    void InitAdMob()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdManager] AdMob initialised.");
            LoadRewardedAd();
            LoadInterstitialAd();
            LoadBannerAd();
        });
    }

    // ── Rewarded Ad ───────────────────────────────────────────────────────────

    void LoadRewardedAd()
    {
        _rewardedAd?.Destroy();
        var adRequest = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, adRequest, (ad, error) =>
        {
            if (error != null) { Debug.LogError($"[AdManager] Rewarded load failed: {error}"); return; }
            _rewardedAd = ad;
            _rewardedAd.OnAdFullScreenContentClosed += LoadRewardedAd;
        });
    }

    void OnUserEarnedReward(Reward reward)
    {
        GrantReward(_pendingReward);
        _onRewardGranted?.Invoke();
        _onRewardGranted = null;
    }

    // ── Interstitial ──────────────────────────────────────────────────────────

    void LoadInterstitialAd()
    {
        _interstitialAd?.Destroy();
        var adRequest = new AdRequest();
        InterstitialAd.Load(interstitialAdId, adRequest, (ad, error) =>
        {
            if (error != null) { Debug.LogError($"[AdManager] Interstitial load failed: {error}"); return; }
            _interstitialAd = ad;
        });
    }

    // ── Banner ────────────────────────────────────────────────────────────────

    void LoadBannerAd()
    {
        _bannerView?.Destroy();
        _bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        _bannerView.LoadAd(new AdRequest());
    }
#endif

    // ── Reward Logic ─────────────────────────────────────────────────────────

    void GrantReward(RewardType type)
    {
        if (GameManager.Instance == null) return;

        switch (type)
        {
            case RewardType.ExtraTime:
                // Resume timer if game is over
                GameManager.Instance.AddTime(30f);
                GameManager.Instance.StartGame(); // restart with bonus time
                Debug.Log("[AdManager] Reward granted: +30 seconds");
                break;

            case RewardType.DoubleScore:
                // TODO: set a double-score flag in GameManager for next game
                Debug.Log("[AdManager] Reward granted: Double Score next game");
                break;

            case RewardType.ContinueGame:
                GameManager.Instance.AddTime(15f);
                Debug.Log("[AdManager] Reward granted: Continue game");
                break;
        }
    }

    // ── Game Over Hook ────────────────────────────────────────────────────────

    void HandleGameOver()
    {
        _gamesPlayed++;
        if (_gamesPlayed % interstitialEveryNGames == 0)
            ShowInterstitialAd();
    }
}
