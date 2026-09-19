using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

/// <summary>
/// Full UI manager — built-in UI.Text (no TMP), 1080×1920 canvas scaling,
/// all panels built 100% in code, no Inspector wiring required.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Panels ──────────────────────────────────────────────────────────────────
    private GameObject _homePanel;
    private GameObject _hudPanel;
    private GameObject _pausePanel;
    private GameObject _settingsPanel;
    private GameObject _gameOverPanel;
    private GameObject _levelBanner;
    private GameObject _levelSelectPanel;
    private GameObject _profilePanel;

    // ── HUD refs ────────────────────────────────────────────────────────────────
    private Text    _scoreTxt;
    private Text    _timerTxt;
    private Text    _levelTxt;
    private Image[] _lifeIcons = new Image[5];

    // ── Other text refs ─────────────────────────────────────────────────────────
    private Text _homeBestTxt;
    private Text _goScoreTxt;
    private Text _goBestTxt;
    private Text _bannerTxt;
    private Text _profileNameTxt;
    private Text _profileEmailTxt;
    private Text _profileStatusTxt;
    private Text _profileInitialTxt;
    private Image _profileAvatar;
    private GameObject _loginBtn;
    private GameObject _logoutBtn;
    private string _loadedAvatarUrl;

    // ── Cheer overlay ───────────────────────────────────────────────────────────
    private GameObject _cheerObj;
    private Text       _cheerTxt;
    private Coroutine  _cheerRoutine;

    // ── Canvas / popup ──────────────────────────────────────────────────────────
    private Canvas     _canvas;
    private Transform  _canvasT;
    private GameObject _popupPrefab;
    private Font       _font;
    private int _levelPage;
    private Text _goalTxt;
    private GameObject _nextBtn;
    private Text _badgeTxt;
    private bool _timerUrgent;
    public void ShowPause() { Show(_pausePanel); }

    // ── Level select refs ───────────────────────────────────────────────────────
    private GameObject[] _levelBtnRefs = new GameObject[LevelManager.TotalLevels];

    // ── Static colours ──────────────────────────────────────────────────────────
    static readonly Color PanelDark   = new Color(0.045f, 0.06f, 0.16f, 0.96f);
    static readonly Color BtnGreen    = new Color(0.06f, 0.68f, 0.53f, 1.00f);
    static readonly Color BtnBlue     = new Color(0.16f, 0.40f, 0.82f, 1.00f);
    static readonly Color BtnOrange   = new Color(0.93f, 0.49f, 0.13f, 1.00f);
    static readonly Color BtnRed      = new Color(0.84f, 0.25f, 0.34f, 1.00f);
    static readonly Color BtnGrey     = new Color(0.18f, 0.23f, 0.37f, 1.00f);
    static readonly Color Gold        = new Color(1.00f, 0.80f, 0.25f, 1.00f);
    static readonly Color TimerUrgent = new Color(1.00f, 0.25f, 0.25f, 1.00f);
    static readonly Color TimerNormal = new Color(0.90f, 0.95f, 1.00f, 1.00f);

    // ── Cheer data ──────────────────────────────────────────────────────────────
    static readonly string[] StreakMessages = {
        "NICE COMBO!", "3 IN A ROW!", "GREAT COMBO!", "POPPING SPREE!",
        "BOOM BOOM BOOM!", "TRIPLE POP!", "KEEP IT UP!"
    };
    static readonly string[] SpecialMessages = {
        "JACKPOT!", "SPECIAL!", "WOW!", "GOLDEN!", "RAINBOW POWER!",
        "LUCKY!", "HEART POWER!", "SWEET!"
    };
    static readonly Color[] CheerCols = {
        new Color(1.00f, 0.92f, 0.05f),
        new Color(0.30f, 1.00f, 0.40f),
        new Color(0.30f, 0.85f, 1.00f),
        new Color(1.00f, 0.40f, 0.85f),
        new Color(1.00f, 0.55f, 0.15f),
        new Color(0.75f, 0.45f, 1.00f),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Roboto", 32);

        BuildAll();
    }

    void OnEnable()
    {
        GameManager.OnGameStart    += OnGameStart;
        GameManager.OnGameOver     += OnGameOver;
        GameManager.OnScoreChanged += OnScore;
        GameManager.OnTimeChanged  += OnTime;
        GameManager.OnLevelChanged += OnLevel;
        GameManager.OnLivesChanged += OnLives;
        GameManager.OnGoToMenu     += OnGoToMenuHandler;
        AuthManager.StateChanged   += RefreshProfileUI;
    }

    void OnDisable()
    {
        GameManager.OnGameStart    -= OnGameStart;
        GameManager.OnGameOver     -= OnGameOver;
        GameManager.OnScoreChanged -= OnScore;
        GameManager.OnTimeChanged  -= OnTime;
        GameManager.OnLevelChanged -= OnLevel;
        GameManager.OnLivesChanged -= OnLives;
        GameManager.OnGoToMenu     -= OnGoToMenuHandler;
        AuthManager.StateChanged   -= RefreshProfileUI;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  BUILD ALL
    // ════════════════════════════════════════════════════════════════════════════
    void BuildAll()
    {
        // Canvas
        var cgo = new GameObject("Canvas");
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 20;

        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;
        cs.matchWidthOrHeight  = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();
        _canvasT = cgo.transform;
        cgo.AddComponent<ResponsiveGameUI>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        _homePanel        = BuildHomePage();
        _hudPanel         = BuildHUD();
        _pausePanel       = BuildPausePage();
        _settingsPanel    = BuildSettingsPage();
        _profilePanel     = BuildProfilePage();
        _gameOverPanel    = BuildGameOverPage();
        _levelBanner      = BuildLevelBanner();
        _levelSelectPanel = BuildLevelSelectPage();
        _popupPrefab      = BuildPopupTemplate();
        BuildCheerOverlay();
        BuildDimmer();

        ShowOnly(_homePanel);
        Hide(_levelBanner);
        RefreshProfileUI();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  HOME PAGE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildHomePage()
    {
        var p = FullPanel("HomePage", Color.clear);

        var wash = Panel(p.transform, "HomeWash", new Color(0.01f, 0.025f, 0.10f, 0.38f), 0, 0, 1080, 1920, 0);
        wash.GetComponent<Image>().raycastTarget = false;
        IconBtn(p.transform, "SettingsBtn", SpriteFactory.MakeGear(64),
            new Color(0.10f, 0.23f, 0.48f, 0.96f), 452, 868, 96, 96,
            () => { AudioManager.Instance?.PlayButtonClick(); Show(_settingsPanel); });
        IconBtn(p.transform, "ProfileBtn", SpriteFactory.MakeCircleGloss(64, new Color(0.25f, 0.70f, 1f), Color.white),
            new Color(0.10f, 0.23f, 0.48f, 0.96f), 338, 868, 96, 96,
            () => { AudioManager.Instance?.PlayButtonClick(); RefreshProfileUI(); Show(_profilePanel); });

        var brandChip = Panel(p.transform, "BrandChip", new Color(0.12f, 0.42f, 0.72f, 0.80f), -250, 862, 430, 58, 29);
        var chipTxt = Txt(brandChip.transform, "Text", "CALM  •  FOCUS  •  FUN", 22, Color.white, true);
        Fill(chipTxt.rectTransform);

        var heroCard = Panel(p.transform, "HeroCard", new Color(0.025f, 0.055f, 0.16f, 0.90f), 0, 585, 920, 440, 46);
        heroCard.AddComponent<BounceIn>();
        var glow = Panel(heroCard.transform, "Glow", new Color(0.20f, 0.62f, 1f, 0.12f), 230, 10, 410, 410, 205);
        glow.GetComponent<Image>().raycastTarget = false;
        Sprite heroCluster = Resources.Load<Sprite>("Premium/balloon-hero-cluster");
        if (heroCluster != null)
        {
            var hero = Img(heroCard.transform, "PremiumBalloonCluster", heroCluster);
            Pos(hero, 240, 5, 370, 420);
            hero.gameObject.AddComponent<FloatAnim>().offset = 0.4f;
        }
        var title1 = Txt(heroCard.transform, "Title1", "BALLOON", 62, Color.white, true);
        title1.alignment = TextAnchor.MiddleLeft; Pos(title1, -210, 105, 430, 82);
        var title2 = Txt(heroCard.transform, "Title2", "POP!", 78, new Color(1f, 0.76f, 0.18f), true);
        title2.alignment = TextAnchor.MiddleLeft; Pos(title2, -210, 28, 430, 94);
        var subtitle = Txt(heroCard.transform, "Subtitle", "Pop, smile, and grow your streak", 24, new Color(0.70f, 0.84f, 1f), false);
        subtitle.alignment = TextAnchor.MiddleLeft; Pos(subtitle, -210, -53, 430, 58);

        var best = Panel(heroCard.transform, "BestPill", new Color(0.06f, 0.12f, 0.28f, 0.96f), -205, -142, 420, 82, 41);
        Pos(Img(best.transform, "Trophy", SpriteFactory.MakeTrophy(48)), -160, 0, 42, 42);
        var bestLabel = Txt(best.transform, "Label", "PERSONAL BEST", 18, new Color(0.65f, 0.76f, 0.95f), true);
        bestLabel.alignment = TextAnchor.UpperLeft; Pos(bestLabel, -15, 17, 250, 26);
        _homeBestTxt = Txt(best.transform, "Value", PlayerPrefs.GetInt("BestScore", 0).ToString(), 38, Gold, true);
        _homeBestTxt.alignment = TextAnchor.MiddleLeft; Pos(_homeBestTxt, -15, -13, 250, 48);

        var adventure = Panel(p.transform, "AdventureCard", new Color(0.035f, 0.12f, 0.23f, 0.94f), 0, 155, 920, 310, 38);
        Pos(Img(adventure.transform, "Balloon", SpriteFactory.MakeBalloon(96, new Color(0.12f, 0.82f, 0.64f))), -365, 54, 78, 100);
        var advTitle = Txt(adventure.transform, "Title", "ADVENTURE", 38, Color.white, true);
        advTitle.alignment = TextAnchor.MiddleLeft; Pos(advTitle, -115, 75, 430, 54);
        var advSub = Txt(adventure.transform, "Sub", "Clear levels • earn stars • unlock more", 23, TimerNormal, false);
        advSub.alignment = TextAnchor.MiddleLeft; Pos(advSub, -63, 25, 535, 45);
        Btn(adventure.transform, "PlayBtn", "CHOOSE LEVEL", 38, BtnGreen, 70, -78, 660, 112, 32,
            () => { AudioManager.Instance?.PlayButtonClick(); ShowOnly(_levelSelectPanel); });

        var ultimate = Panel(p.transform, "UltimateCard", new Color(0.08f, 0.07f, 0.25f, 0.94f), 0, -205, 920, 285, 38);
        Pos(Img(ultimate.transform, "Star", SpriteFactory.MakeStar(72, Gold)), -365, 50, 70, 70);
        var ultTitle = Txt(ultimate.transform, "Title", "ULTIMATE RUN", 36, Color.white, true);
        ultTitle.alignment = TextAnchor.MiddleLeft; Pos(ultTitle, -105, 65, 450, 54);
        var ultSub = Txt(ultimate.transform, "Sub", "No timer • protect 3 hearts • chase your best", 22, TimerNormal, false);
        ultSub.alignment = TextAnchor.MiddleLeft; Pos(ultSub, -35, 15, 600, 44);
        Btn(ultimate.transform, "UltimateBtn", "START ENDLESS", 36, BtnBlue, 70, -75, 660, 104, 30,
            () => { AudioManager.Instance?.PlayButtonClick(); GameManager.Instance?.StartUltimate(); });

        var tip = Txt(p.transform, "Tip", "TIP  •  Rare balloons bring bigger rewards", 23, new Color(0.74f, 0.86f, 1f), true);
        Pos(tip, 0, -440, 900, 52);

        return p;
    }

    GameObject BuildProfilePage()
    {
        var p = FullPanel("ProfilePanel", new Color(0.008f, 0.018f, 0.07f, 0.99f));
        var header = Panel(p.transform, "ProfileHeader", new Color(0.04f, 0.10f, 0.25f, 0.98f), 0, 785, 920, 190, 38);
        var title = Txt(header.transform, "Title", "PLAYER PROFILE", 46, Color.white, true);
        title.alignment = TextAnchor.MiddleLeft; Pos(title, -125, 30, 560, 70);
        var sub = Txt(header.transform, "Sub", "Your Balloon Pop identity", 23, new Color(0.68f, 0.82f, 1f), false);
        sub.alignment = TextAnchor.MiddleLeft; Pos(sub, -125, -38, 560, 46);
        IconBtn(header.transform, "Close", SpriteFactory.MakeGear(48), BtnGrey, 370, 0, 82, 82,
            () => { AudioManager.Instance?.PlayButtonClick(); Hide(p); });

        var card = Panel(p.transform, "IdentityCard", new Color(0.035f, 0.075f, 0.19f, 0.98f), 0, 400, 920, 480, 42);
        var avatarBg = Panel(card.transform, "AvatarBackground", new Color(0.15f, 0.50f, 0.92f, 1f), 0, 125, 170, 170, 85);
        _profileAvatar = Img(avatarBg.transform, "Avatar", SpriteFactory.MakeCircleGloss(160, new Color(0.24f, 0.72f, 1f), Color.white));
        Fill(_profileAvatar.rectTransform);
        _profileInitialTxt = Txt(avatarBg.transform, "Initial", "?", 72, Color.white, true);
        Fill(_profileInitialTxt.rectTransform);
        _profileNameTxt = Txt(card.transform, "Name", "Guest Player", 40, Color.white, true);
        Pos(_profileNameTxt, 0, -5, 780, 58);
        _profileEmailTxt = Txt(card.transform, "Email", "Not signed in", 25, new Color(0.68f, 0.82f, 1f), false);
        Pos(_profileEmailTxt, 0, -65, 780, 48);
        _profileStatusTxt = Txt(card.transform, "Status", "Sign in to save your player profile", 22, Gold, false);
        Pos(_profileStatusTxt, 0, -142, 790, 58);

        var privacy = Panel(p.transform, "PrivacyCard", new Color(0.09f, 0.08f, 0.23f, 0.96f), 0, 65, 920, 150, 32);
        var privacyTitle = Txt(privacy.transform, "Title", "SAFE & SIMPLE", 25, new Color(0.52f, 0.85f, 1f), true);
        privacyTitle.alignment = TextAnchor.MiddleLeft; Pos(privacyTitle, -255, 34, 330, 40);
        var privacyBody = Txt(privacy.transform, "Body", "Google verifies your identity. Your password is never shared with the game.", 22, Color.white, false);
        Pos(privacyBody, 0, -25, 800, 65);

        Btn(p.transform, "GoogleLoginBtn", "CONTINUE WITH GOOGLE", 34, BtnBlue, 0, -115, 720, 116, 34,
            () => { AudioManager.Instance?.PlayButtonClick(); AuthManager.Instance?.Login(); });
        _loginBtn = p.transform.Find("GoogleLoginBtn").gameObject;
        Btn(p.transform, "LogoutBtn", "LOG OUT", 34, BtnRed, 0, -115, 620, 110, 32,
            () => { AudioManager.Instance?.PlayButtonClick(); AuthManager.Instance?.Logout(); });
        _logoutBtn = p.transform.Find("LogoutBtn").gameObject;

        Btn(p.transform, "ProfileDoneBtn", "DONE", 36, BtnGreen, 0, -280, 520, 104, 30,
            () => { AudioManager.Instance?.PlayButtonClick(); Hide(p); });
        var hint = Txt(p.transform, "Hint", "One profile is kept for each Google account", 22, new Color(0.60f, 0.72f, 0.90f), false);
        Pos(hint, 0, -390, 850, 48);
        return p;
    }

    void RefreshProfileUI()
    {
        if (_profileNameTxt == null) return;
        var auth = AuthManager.Instance;
        bool signedIn = auth != null && auth.IsLoggedIn;
        var user = signedIn ? auth.User : null;
        _profileNameTxt.text = user != null && !string.IsNullOrEmpty(user.name) ? user.name : "Guest Player";
        _profileEmailTxt.text = user != null && !string.IsNullOrEmpty(user.email) ? user.email : "Not signed in";
        _profileStatusTxt.text = auth != null ? auth.Status : "Account service is starting…";
        _profileInitialTxt.text = user != null && !string.IsNullOrEmpty(user.name) ? user.name.Substring(0, 1).ToUpperInvariant() : "?";
        _loginBtn?.SetActive(!signedIn);
        _logoutBtn?.SetActive(signedIn);
        if (user != null && !string.IsNullOrEmpty(user.picture) && user.picture != _loadedAvatarUrl)
            StartCoroutine(LoadProfileAvatar(user.picture));
        else if (!signedIn)
        {
            _loadedAvatarUrl = null;
            _profileInitialTxt.gameObject.SetActive(true);
        }
    }

    IEnumerator LoadProfileAvatar(string url)
    {
        _loadedAvatarUrl = url;
        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success || _loadedAvatarUrl != url) yield break;
            var texture = DownloadHandlerTexture.GetContent(request);
            _profileAvatar.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _profileInitialTxt.gameObject.SetActive(false);
        }
    }

    void HomeScoreRow(Transform parent, string name, string pts,
        Color balloonCol, Color nameCol, Color ptCol, float x, float y, int idx)
    {
        var row = Panel(parent, "R_" + name, new Color(1f, 1f, 1f, 0.055f), x, y, 840, 66, 16f);
        Sprite ico;
        if (idx == 3)      ico = SpriteFactory.MakeStar(48, balloonCol);
        else if (idx == 5) ico = SpriteFactory.MakeBomb(48);
        else if (idx == 6) ico = SpriteFactory.MakeHeart(48, balloonCol);
        else               ico = SpriteFactory.MakeBalloon(64, balloonCol);
        Pos(Img(row.transform, "I", ico), -365, 0, 40, 50);
        var nt = Txt(row.transform, "N", name, 27, nameCol, true);
        nt.alignment = TextAnchor.MiddleLeft; Pos(nt, -120, 0, 390, 50);
        var pt = Txt(row.transform, "P", pts, 32, ptCol, true);
        pt.fontSize = 25;
        pt.alignment = TextAnchor.MiddleRight; Pos(pt, 265, 0, 270, 50);
    }

    void KidLegendRow(Transform parent, string name, string pts,
        Color balloonCol, Color nameCol, Color ptCol, float x, float y, int idx)
        => HomeScoreRow(parent, name, pts, balloonCol, nameCol, ptCol, x, y, idx);

    // ════════════════════════════════════════════════════════════════════════════
    //  HUD
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildHUD()
    {
        var p = FullPanel("HUDPanel", Color.clear);
        p.GetComponent<Image>().raycastTarget = false;
        _goalTxt = Txt(p.transform, "RoundGoal", "", 28, Gold, true);
        Pos(_goalTxt, 130, 760, 650, 60);

        // Top bar
        var bar = Panel(p.transform, "TopBar", new Color(0f, 0f, 0f, 0.60f), 0, 880, 1080, 130, 0f);

        // Score pill
        var scorePill = Panel(bar.transform, "ScorePill", new Color(0.08f, 0.12f, 0.35f, 0.90f), -330, 0, 290, 88, 22f);
        Pos(Img(scorePill.transform, "Coin", SpriteFactory.MakeCoin(48)), -95, 0, 38, 38);
        var scoreLabel = Txt(scorePill.transform, "ScoreLabel", "SCORE", 16, new Color(0.66f, 0.78f, 1f), true);
        scoreLabel.alignment = TextAnchor.UpperCenter; Pos(scoreLabel, 25, 26, 160, 25);
        _scoreTxt = Txt(scorePill.transform, "ScoreVal", "0", 36, Gold, true);
        _scoreTxt.alignment = TextAnchor.MiddleCenter; Pos(_scoreTxt, 25, -10, 160, 60);

        // Timer pill
        var timerPill = Panel(bar.transform, "TimerPill", new Color(0.12f, 0.06f, 0.38f, 0.90f), 0, 0, 165, 94, 24f);
        var timerLabel = Txt(timerPill.transform, "TimerLabel", "TIME", 14, new Color(0.72f, 0.8f, 1f), true);
        timerLabel.alignment = TextAnchor.UpperCenter; Pos(timerLabel, 0, 28, 120, 22);
        _timerTxt = Txt(timerPill.transform, "TimerVal", "60", 54, TimerNormal, true);
        _timerTxt.alignment = TextAnchor.MiddleCenter; Pos(_timerTxt, 0, -9, 150, 66);

        // Level pill
        var levelPill = Panel(bar.transform, "LevelPill", new Color(0.12f, 0.06f, 0.38f, 0.90f), 195, 0, 185, 72, 20f);
        _levelTxt = Txt(levelPill.transform, "LevelVal", "LVL 1", 30, new Color(0.8f, 0.85f, 1f), false);
        _levelTxt.alignment = TextAnchor.MiddleCenter; Fill(_levelTxt.GetComponent<RectTransform>());

        // Pause button (decoration) + transparent overlay button
        var pauseDecor = Panel(p.transform, "PauseDecor", new Color(0.08f, 0.12f, 0.35f, 0.95f), 460, 856, 92, 92, 18f);
        var pauseII    = Txt(pauseDecor.transform, "PauseII", "II", 44, Color.white, true);
        pauseII.alignment = TextAnchor.MiddleCenter; Fill(pauseII.GetComponent<RectTransform>());

        // Transparent button on top
        Btn(p.transform, "PauseBtn", "", 1, Color.clear, 460, 856, 92, 92, 18f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.TogglePause();
                Show(_pausePanel);
            });

        // Lives row
        var livesRow = Panel(p.transform, "LivesRow", new Color(0f, 0f, 0f, 0.45f), -350, 790, 255, 58, 16f);
        for (int i = 0; i < 5; i++)
        {
            var h = Img(livesRow.transform, "Heart" + i, SpriteFactory.MakeHeart(40, Color.white));
            Pos(h, -90 + i * 46, 0, 32, 32);
            _lifeIcons[i] = h;
        }

        return p;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  PAUSE PAGE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildPausePage()
    {
        var p = FullPanel("PausePanel", new Color(0f, 0f, 0f, 0.80f));

        // Pause icon pill
        var pip = Panel(p.transform, "PauseIconPill", new Color(0.12f, 0.45f, 0.85f, 0.90f), 0, 300, 140, 140, 70f);
        var pii = Txt(pip.transform, "PII", "II", 54, Color.white, true);
        pii.alignment = TextAnchor.MiddleCenter; Fill(pii.GetComponent<RectTransform>());

        Txt(p.transform, "PausedLbl", "PAUSED", 72, Color.white, true);
        var pt = p.transform.Find("PausedLbl")?.GetComponent<Text>();
        if (pt != null) { pt.alignment = TextAnchor.MiddleCenter; Pos(pt, 0, 180, 600, 90); }

        Btn(p.transform, "ResumeBtn", "RESUME", 42, BtnGreen, 0, 40, 480, 115, 22f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.TogglePause();
                Hide(_pausePanel);
            });

        Btn(p.transform, "PauseSettingsBtn", "SETTINGS", 36, BtnBlue, 0, -100, 480, 102, 22f,
            () => { AudioManager.Instance?.PlayButtonClick(); Show(_settingsPanel); });

        Btn(p.transform, "PauseHomeBtn", "HOME", 36, BtnGrey, 0, -232, 480, 102, 22f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                if (GameManager.Instance != null && GameManager.Instance.IsPaused)
                    GameManager.Instance.TogglePause();
                Hide(_pausePanel);
                GameManager.Instance?.GoToMenu();
            });

        return p;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  SETTINGS PAGE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildSettingsPage()
    {
        var p = FullPanel("SettingsPanel", new Color(0.012f, 0.025f, 0.09f, 0.985f));
        var header = Panel(p.transform, "SettingsHeader", new Color(0.04f, 0.09f, 0.22f, 0.96f), 0, 790, 920, 190, 38);
        Pos(Img(header.transform, "SGear", SpriteFactory.MakeGear(64)), -375, 25, 60, 60);
        var st = Txt(header.transform, "SettingsTitle", "MY POP SPACE", 48, Color.white, true);
        st.alignment = TextAnchor.MiddleLeft; Pos(st, -55, 35, 540, 70);
        var ss = Txt(header.transform, "SettingsSub", "Sound, feel, and style — make it yours", 23, new Color(0.68f, 0.80f, 1f), false);
        ss.alignment = TextAnchor.MiddleLeft; Pos(ss, -35, -35, 580, 46);
        Pos(Img(header.transform, "Balloon", SpriteFactory.MakeBalloon(80, new Color(1f, 0.35f, 0.65f))), 365, 0, 74, 95);

        var playCard = Panel(p.transform, "PlaySettings", new Color(0.035f, 0.07f, 0.17f, 0.96f), 0, 340, 920, 610, 34);
        var playTitle = Txt(playCard.transform, "Title", "PLAY SETTINGS", 25, new Color(0.50f, 0.82f, 1f), true);
        playTitle.alignment = TextAnchor.MiddleLeft; Pos(playTitle, -270, 260, 300, 44);
        BuildSettingsToggle(playCard.transform, "Sound", "POP SOUNDS", 165, "SoundEnabled", true);
        BuildSettingsToggle(playCard.transform, "Music", "MUSIC", 55, "MusicEnabled", true);
        BuildSettingsToggle(playCard.transform, "Vibrate", "VIBRATION", -55, "VibrateEnabled", true);

        var lookTitle = Txt(playCard.transform, "LookTitle", "GAME LOOK", 25, new Color(0.50f, 0.82f, 1f), true);
        lookTitle.alignment = TextAnchor.MiddleLeft; Pos(lookTitle, -290, -145, 260, 44);
        BuildVisualChoice(playCard.transform, "Background", "BACKGROUND", -215, "BackgroundStyle",
            "TWILIGHT", "CLASSIC", () => KidsBackground.RefreshTheme());
        BuildVisualChoice(playCard.transform, "BalloonLook", "BALLOONS", -305, "BalloonStyle",
            "GLOSS", "CLASSIC", () => BalloonSpawner.RefreshBalloonStyle());

        var scoreCard = Panel(p.transform, "ScoreSettings", new Color(0.07f, 0.08f, 0.22f, 0.96f), 0, -130, 920, 220, 34);
        Pos(Img(scoreCard.transform, "Trophy", SpriteFactory.MakeTrophy(64)), -360, 24, 58, 58);
        var bsLbl = Txt(scoreCard.transform, "BestLbl", "PERSONAL BEST", 22, new Color(0.70f, 0.80f, 1f), true);
        bsLbl.alignment = TextAnchor.MiddleLeft; Pos(bsLbl, -160, 50, 310, 38);
        var bsVal = Txt(p.transform, "BestVal",
            (GameManager.Instance != null ? GameManager.Instance.BestScore : PlayerPrefs.GetInt("BestScore", 0)).ToString(),
            52, Gold, true);
        bsVal.transform.SetParent(scoreCard.transform, false);
        bsVal.alignment = TextAnchor.MiddleLeft; Pos(bsVal, -160, 2, 310, 65);
        Btn(scoreCard.transform, "ResetBestBtn", "RESET", 26, BtnRed, 270, 0, 250, 82, 24f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                PlayerPrefs.SetInt("BestScore", 0);
                PlayerPrefs.Save();
                bsVal.text = "0";
                if (_homeBestTxt != null) _homeBestTxt.text = "0";
            });
        Btn(p.transform, "SettingsCloseBtn", "DONE", 38, BtnGreen, 0, -340, 620, 110, 32f,
            () => { AudioManager.Instance?.PlayButtonClick(); Hide(_settingsPanel); });
        var privacy = Txt(p.transform, "SettingsHint", "Your choices are saved on this device", 22, new Color(0.60f, 0.70f, 0.88f), false);
        Pos(privacy, 0, -435, 800, 42);

        return p;
    }

    void BuildSettingsToggle(Transform parent, string id, string label, float y, string prefKey, bool defaultOn)
    {
        var row = Panel(parent, id + "Row", new Color(1f, 1f, 1f, 0.055f), 0, y, 820, 92, 24f);
        var lbl = Txt(row.transform, id + "Lbl", label, 29, Color.white, true);
        lbl.alignment = TextAnchor.MiddleLeft; Pos(lbl, -220, 0, 380, 60);
        bool on = PlayerPrefs.GetInt(prefKey, defaultOn ? 1 : 0) == 1;
        var pill = Panel(row.transform, id + "Pill", on ? BtnGreen : BtnGrey, 300, 0, 160, 58, 29);
        var val = Txt(pill.transform, id + "Val", on ? "ON" : "OFF", 24, Color.white, true);
        Fill(val.rectTransform);
        // Transparent button over row
        Btn(row.transform, id + "Btn", "", 1, Color.clear, 0, 0, 820, 92, 0f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                bool cur = PlayerPrefs.GetInt(prefKey, defaultOn ? 1 : 0) == 1;
                bool next = !cur;
                PlayerPrefs.SetInt(prefKey, next ? 1 : 0);
                PlayerPrefs.Save();
                val.text  = next ? "ON" : "OFF";
                pill.GetComponent<Image>().color = next ? BtnGreen : BtnGrey;
            });
    }

    void BuildVisualChoice(Transform parent, string id, string label, float y, string prefKey,
        string premiumLabel, string classicLabel, System.Action onChanged)
    {
        var row = Panel(parent, id + "Row", new Color(0.20f, 0.55f, 0.85f, 0.11f), 0, y, 820, 76, 22f);
        var lbl = Txt(row.transform, id + "Lbl", label, 27, Color.white, true);
        lbl.alignment = TextAnchor.MiddleLeft; Pos(lbl, -220, 0, 370, 58);

        int current = PlayerPrefs.GetInt(prefKey, 0);
        var val = Txt(row.transform, id + "Val", current == 0 ? premiumLabel : classicLabel,
            27, current == 0 ? Gold : new Color(0.72f, 0.82f, 1f), true);
        val.alignment = TextAnchor.MiddleCenter; Pos(val, 280, 0, 230, 54);

        Btn(row.transform, id + "Btn", "", 1, Color.clear, 0, 0, 820, 76, 0f, () =>
        {
            AudioManager.Instance?.PlayButtonClick();
            int next = PlayerPrefs.GetInt(prefKey, 0) == 0 ? 1 : 0;
            PlayerPrefs.SetInt(prefKey, next);
            PlayerPrefs.Save();
            val.text = next == 0 ? premiumLabel : classicLabel;
            val.color = next == 0 ? Gold : new Color(0.72f, 0.82f, 1f);
            onChanged?.Invoke();
        });
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  GAME OVER PAGE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildGameOverPage()
    {
        var p = FullPanel("GameOverPanel", new Color(0f, 0f, 0f, 0.90f));

        // Trophy
        var trophyImg = Img(p.transform, "GOTrophy", SpriteFactory.MakeTrophy(120));
        Pos(trophyImg, 0, 490, 120, 120);
        trophyImg.gameObject.AddComponent<BounceIn>();
        trophyImg.gameObject.AddComponent<PulseAnim>();

        // GAME OVER text
        var goTxt = Txt(p.transform, "GOTitle", "SESSION COMPLETE", 54, Color.white, true);
        goTxt.alignment = TextAnchor.MiddleCenter; Pos(goTxt, 0, 350, 760, 90);
        goTxt.gameObject.AddComponent<BounceIn>().delay = 0.15f;

        // Score card
        var sc = Panel(p.transform, "GOScoreCard", new Color(0.06f, 0.06f, 0.28f, 0.95f), 0, 155, 720, 200, 24f);
        _badgeTxt = Txt(p.transform, "EarnedBadge", "", 28, Gold, true);
        Pos(_badgeTxt, 0, 280, 900, 44);
        sc.AddComponent<BounceIn>().delay = 0.25f;

        _goScoreTxt = Txt(sc.transform, "GOScore", "SCORE: 0", 40, Color.white, true);
        _goScoreTxt.alignment = TextAnchor.MiddleCenter; Pos(_goScoreTxt, 0, 42, 660, 56);
        _goBestTxt = Txt(sc.transform, "GOBest", "BEST: 0", 32, Gold, false);
        _goBestTxt.alignment = TextAnchor.MiddleCenter; Pos(_goBestTxt, 0, -32, 660, 46);

        // Play Again button
        Btn(p.transform, "PlayAgainBtn", "PLAY AGAIN", 44, BtnGreen, 0, -75, 540, 120, 24f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.StartGame();
            });
        var paB = p.transform.Find("PlayAgainBtn");
        if (paB != null)
        {
            paB.gameObject.AddComponent<BounceIn>().delay = 0.35f;
            paB.gameObject.AddComponent<PulseAnim>();
        }

        // Advance only after earning a star in this round.
        Btn(p.transform, "NextLevelBtn", "NEXT LEVEL", 34, BtnBlue, 0, -220, 540, 102, 22f,
            () =>
            {
                if (GameManager.Instance == null || !GameManager.Instance.Cleared) return;
                LevelManager.SelectLevel(LevelManager.CurrentLevel + 1);
                GameManager.Instance.StartAdventure();
            });
        _nextBtn = p.transform.Find("NextLevelBtn").gameObject;

        // Home button
        Btn(p.transform, "GOHomeBtn", "HOME", 34, BtnGrey, 0, -358, 390, 92, 20f,
            () =>
            {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.GoToMenu();
            });
        p.transform.Find("GOHomeBtn")?.gameObject.AddComponent<BounceIn>();

        return p;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  LEVEL BANNER
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildLevelBanner()
    {
        var go = new GameObject("LevelBanner");
        go.transform.SetParent(_canvasT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(780, 130);
        rt.anchoredPosition = new Vector2(0, 120);

        var bg = go.AddComponent<Image>();
        bg.sprite = SpriteFactory.MakeRoundedRect(780, 130, 22f, new Color(0.12f, 0.06f, 0.38f, 0.95f));

        _bannerTxt = Txt(go.transform, "BannerTxt", "LEVEL UP!", 52, Gold, true);
        _bannerTxt.alignment = TextAnchor.MiddleCenter;
        Fill(_bannerTxt.GetComponent<RectTransform>());

        var lue = go.AddComponent<LevelUpEffect>();
        lue.levelUpTextLegacy = _bannerTxt;

        return go;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CHEER OVERLAY
    // ════════════════════════════════════════════════════════════════════════════
    void BuildCheerOverlay()
    {
        _cheerObj = new GameObject("CheerOverlay");
        _cheerObj.transform.SetParent(_canvasT, false);
        var rt = _cheerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700, 120);
        rt.anchoredPosition = new Vector2(0, 200);

        // Drop shadow
        var shadowGo = new GameObject("CheerShadow");
        shadowGo.transform.SetParent(_cheerObj.transform, false);
        var shadowRt = shadowGo.AddComponent<RectTransform>();
        shadowRt.anchorMin = Vector2.zero; shadowRt.anchorMax = Vector2.one;
        shadowRt.offsetMin = new Vector2(4, -4); shadowRt.offsetMax = new Vector2(4, -4);
        var shadowTxt = shadowGo.AddComponent<Text>();
        shadowTxt.font      = _font;
        shadowTxt.fontSize  = 58;
        shadowTxt.fontStyle = FontStyle.Bold;
        shadowTxt.color     = new Color(0f, 0f, 0f, 0.55f);
        shadowTxt.alignment = TextAnchor.MiddleCenter;

        _cheerTxt = _cheerObj.AddComponent<Text>();
        _cheerTxt.font      = _font;
        _cheerTxt.fontSize  = 58;
        _cheerTxt.fontStyle = FontStyle.Bold;
        _cheerTxt.color     = Color.white;
        _cheerTxt.alignment = TextAnchor.MiddleCenter;
        _cheerTxt.raycastTarget = false;
        shadowTxt.raycastTarget = false;

        var sync = _cheerObj.AddComponent<CheerShadowSync>();
        sync.shadow = shadowTxt;

        _cheerObj.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  POPUP TEMPLATE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildPopupTemplate()
    {
        var go = new GameObject("ScorePopup");
        go.transform.SetParent(_canvasT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 60);
        var txt = go.AddComponent<Text>();
        txt.font      = _font;
        txt.fontSize  = 46;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = Gold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        go.AddComponent<ScorePopupLegacy>();
        go.SetActive(false);
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  BACKGROUND DIMMER
    // ════════════════════════════════════════════════════════════════════════════
    void BuildDimmer()
    {
        var go = new GameObject("BgDimOverlay");
        go.transform.SetParent(_canvasT, false);
        go.transform.SetSiblingIndex(0); // behind everything
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = SpriteFactory.MakeWhiteSquare();
        img.color  = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        var bd = go.AddComponent<BackgroundDimmer>();
        bd.overlayImage = img;
        bd.dimAmount    = 0.35f;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  LEVEL SELECT PAGE
    // ════════════════════════════════════════════════════════════════════════════
    GameObject BuildLevelSelectPage()
    {
        var p = FullPanel("LevelSelectPanel", new Color(0f, 0f, 0f, 0.82f));

        // Header bar
        var header = Panel(p.transform, "LSHeader", new Color(0.06f, 0.06f, 0.22f, 0.95f), 0, 860, 1080, 130, 0f);
        var ht = Txt(header.transform, "LSTitle", "SELECT LEVEL", 46, Color.white, true);
        ht.alignment = TextAnchor.MiddleCenter; Fill(ht.GetComponent<RectTransform>());

        // Back button
        Btn(p.transform, "LSBackBtn", "BACK", 32, BtnGrey, -400, 860, 160, 80, 16f,
            () => { AudioManager.Instance?.PlayButtonClick(); ShowOnly(_homePanel); });

        // 10 level cards in 2 columns
        float[] colX = { -230f, 230f };
        float[] rowY = { 630f, 420f, 210f, 0f, -210f };
        for (int i = 0; i < LevelManager.TotalLevels; i++)
        {
            int level = i + 1;
            float x   = colX[i % 2];
            float y   = rowY[i / 2];
            _levelBtnRefs[i] = BuildLevelCard(p.transform, level, x, y);
        }

        Btn(p.transform, "PreviousPage", "PREVIOUS", 28, BtnGrey, -230, -420, 420, 100, 24,
            () => { _levelPage = Mathf.Max(0, _levelPage - 1); RefreshLevelSelect(); });
        Btn(p.transform, "NextPage", "MORE LEVELS", 28, BtnBlue, 230, -420, 420, 100, 24,
            () => { if (LevelManager.IsUnlocked((_levelPage + 1) * 10 + 1)) { _levelPage++; RefreshLevelSelect(); } });
        Pos(Txt(p.transform, "UnlockHint", "Earn 1 star to unlock the next level.\nComplete this page to discover more!", 26, TimerNormal, false), 0, -580, 900, 90);
        return p;
    }

    GameObject BuildLevelCard(Transform parent, int level, float x, float y)
    {
        bool unlocked = LevelManager.IsUnlocked(level);
        int  stars    = LevelManager.GetStars(level);
        int  target   = LevelManager.GetStar3Score(level);

        Color cardCol = unlocked
            ? new Color(0.08f, 0.18f, 0.50f, 0.95f)
            : new Color(0.18f, 0.18f, 0.28f, 0.95f);

        var card = Panel(parent, "LvlCard_" + level, cardCol, x, y, 420, 185, 22f);

        if (unlocked)
        {
            // Level number
            Color numCol = level <= 3 ? BtnGreen : level <= 6 ? Gold : BtnOrange;
            var numTxt = Txt(card.transform, "LvlNum", level.ToString(), 60, numCol, true);
            numTxt.alignment = TextAnchor.MiddleCenter; Pos(numTxt, -90, 30, 120, 80);

            // Star row
            BuildStarRow(card.transform, stars, 60, 30, 200);

            // Target score hint
            var hint = Txt(card.transform, "LvlHint", "★★★ = " + target, 22, new Color(0.7f, 0.7f, 0.9f), false);
            hint.alignment = TextAnchor.MiddleCenter; Pos(hint, 0, -60, 380, 36);

            // Transparent play button
            int lvlCapture = level;
            Btn(card.transform, "LvlPlayBtn", "", 1, Color.clear, 0, 0, 420, 185, 22f,
                () =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    LevelManager.SelectLevel(lvlCapture);
                    GameManager.Instance?.StartAdventure();
                });
        }
        else
        {
            // Locked card
            var lockLbl = Txt(card.transform, "LvlLocked", "LOCKED", 36, new Color(0.5f, 0.5f, 0.6f), true);
            lockLbl.alignment = TextAnchor.MiddleCenter; Pos(lockLbl, 0, 20, 360, 50);
            var locNum = Txt(card.transform, "LvlNum", level.ToString(), 52, new Color(0.4f, 0.4f, 0.5f), true);
            locNum.alignment = TextAnchor.MiddleCenter; Pos(locNum, 0, -40, 200, 70);
        }

        return card;
    }

    void BuildStarRow(Transform parent, int filled, float x, float y, float width)
    {
        float spacing = width / 3f;
        for (int i = 0; i < 3; i++)
        {
            bool isFilled = i < filled;
            Color sc = isFilled ? Gold : new Color(0.3f, 0.3f, 0.4f);
            var star = Img(parent, "Star" + i, SpriteFactory.MakeStar(40, sc));
            Pos(star, x - spacing + i * spacing, y, 34, 34);
        }
    }

    void RefreshLevelSelect()
    {
        if (_levelSelectPanel == null) return;
        var p = _levelSelectPanel.transform;
        for (int i = 0; i < LevelManager.TotalLevels; i++)
        {
            if (_levelBtnRefs[i] != null) Destroy(_levelBtnRefs[i]);
            int level = _levelPage * 10 + i + 1;
            float x   = (i % 2 == 0) ? -230f : 230f;
            float y   = new float[] { 630f, 420f, 210f, 0f, -210f }[i / 2];
            _levelBtnRefs[i] = BuildLevelCard(p, level, x, y);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  EVENT HANDLERS
    // ════════════════════════════════════════════════════════════════════════════
    void OnGameStart()
    {
        ShowOnly(_hudPanel);
        Hide(_levelBanner);
        Hide(_pausePanel);
        RefreshLives(GameManager.Instance != null ? GameManager.Instance.Lives : 3);
        int best = GameManager.Instance != null
            ? GameManager.Instance.BestScore
            : PlayerPrefs.GetInt("BestScore", 0);
        if (_homeBestTxt != null) _homeBestTxt.text = best.ToString();
    }

    void OnGoToMenuHandler()
    {
        if (_cheerObj != null) _cheerObj.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
        int best = GameManager.Instance != null
            ? GameManager.Instance.BestScore
            : PlayerPrefs.GetInt("BestScore", 0);
        if (_homeBestTxt != null) _homeBestTxt.text = best.ToString();
        ShowOnly(_homePanel);
    }

    void OnGameOver()
    {
        ShowOnly(_gameOverPanel);
        if (_cheerObj != null) _cheerObj.SetActive(false);
        RefreshLevelSelect();

        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        int best  = GameManager.Instance != null
            ? GameManager.Instance.BestScore
            : PlayerPrefs.GetInt("BestScore", 0);

        if (_goScoreTxt != null) _goScoreTxt.text = "SCORE: " + score;
        if (_goBestTxt  != null) _goBestTxt.text  = "BEST: "  + best;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            _gameOverPanel.transform.Find("GOTitle").GetComponent<Text>().text = gm.Cleared ? "LEVEL COMPLETE!" : "GREAT POPPING!";
            _goBestTxt.text = "BEST " + best + "  |  " + gm.Pops + " POPS  |  STREAK " + gm.BestStreak;
            _nextBtn.SetActive(gm.Cleared);
            string badge = gm.BestStreak >= 10 ? "STREAK SUPERSTAR" : gm.Pops >= 20 ? "BALLOON EXPLORER" : "LITTLE POP HERO";
            _badgeTxt.text = gm.Pops > 0 ? badge + "  -  Well done!" : "Ready when you are. Try another round!";
        }
        if (_homeBestTxt != null) _homeBestTxt.text = best.ToString();
    }

    void OnScore(int s)
    {
        if (_scoreTxt != null) _scoreTxt.text = s.ToString();
        if (_goalTxt != null && GameManager.Instance != null)
            _goalTxt.text = GameManager.Instance.IsUltimate ? "ULTIMATE RUN  |  SAVE YOUR HEARTS" :
                "1 STAR: " + s + " / " + LevelManager.GetClearScore(GameManager.Instance.Level);
    }

    void OnTime(float t)
    {
        if (_timerTxt == null) return;
        _timerTxt.text = GameManager.Instance != null && GameManager.Instance.IsUltimate ? "RUN" : Mathf.CeilToInt(Mathf.Max(0, t)).ToString();
        bool urgent = t <= 10f;
        _timerTxt.color = urgent ? TimerUrgent : TimerNormal;
        if (urgent && !_timerUrgent)
        {
            _timerTxt.transform.localScale = Vector3.one * 1.08f;
            StopCoroutine("TimerPulse");
            StartCoroutine(TimerPulse());
        }
        _timerUrgent = urgent;
    }

    IEnumerator TimerPulse()
    {
        float t = 0f;
        while (t < 0.18f)
        {
            float s = Mathf.Lerp(1.08f, 1f, t / 0.18f);
            if (_timerTxt != null) _timerTxt.transform.localScale = Vector3.one * s;
            t += Time.deltaTime;
            yield return null;
        }
        if (_timerTxt != null) _timerTxt.transform.localScale = Vector3.one;
    }

    void OnLevel(int lvl)
    {
        if (_levelTxt != null) _levelTxt.text = "LVL " + lvl;
    }

    void OnLives(int l)
    {
        RefreshLives(l);
    }

    void RefreshLives(int lives)
    {
        Color active   = new Color(1f, 0.4f, 0.6f);
        Color inactive = new Color(0.3f, 0.3f, 0.4f);
        for (int i = 0; i < _lifeIcons.Length; i++)
        {
            if (_lifeIcons[i] != null)
                _lifeIcons[i].color = i < lives ? active : inactive;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  SCORE POPUP
    // ════════════════════════════════════════════════════════════════════════════
    public void ShowScorePopup(int points, Vector3 worldPos, string message = null)
    {
        if (_popupPrefab == null) return;
        var go = Instantiate(_popupPrefab, _canvasT);
        go.SetActive(true);

        // World → canvas position
        Vector2 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPos)
            : (Vector2)worldPos;
        var rt = go.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out Vector2 localPt);
        rt.anchoredPosition = localPt;

        var txt = go.GetComponent<Text>();
        if (txt != null)
        {
            txt.text  = message ?? ((points >= 0 ? "+" : "") + points.ToString());
            txt.color = points >= 0 ? Gold : BtnRed;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CHEER SYSTEM
    // ════════════════════════════════════════════════════════════════════════════
    public void ShowCheer(Vector3 worldPos, int streak = 1)
    {
        if (_cheerObj == null || _cheerTxt == null) return;

        string msg;
        if      (streak >= 20) msg = "UNSTOPPABLE!";
        else if (streak >= 15) msg = "LEGENDARY!";
        else if (streak >= 10) msg = "ON FIRE!";
        else if (streak >=  8) msg = "BLAZING!";
        else if (streak >=  5) msg = "AMAZING!";
        else if (streak >=  3) msg = StreakMessages[Random.Range(0, StreakMessages.Length)];
        else                   msg = SpecialMessages[Random.Range(0, SpecialMessages.Length)];

        Color col = CheerCols[Random.Range(0, CheerCols.Length)];
        _cheerTxt.text  = msg;
        _cheerTxt.color = col;

        // Position near world pos if valid
        if (worldPos != Vector3.zero && Camera.main != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            var rt = _cheerObj.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(), screenPos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                out Vector2 localPt);
            rt.anchoredPosition = new Vector2(localPt.x, localPt.y + 60f);
        }

        if (_cheerRoutine != null) StopCoroutine(_cheerRoutine);
        _cheerRoutine = StartCoroutine(AnimateCheer());
    }

    IEnumerator AnimateCheer()
    {
        _cheerObj.SetActive(true);
        var rt = _cheerObj.GetComponent<RectTransform>();
        Vector2 basePos = rt.anchoredPosition;

        // Pop in: 0 → 1.2 → 1.0
        float t = 0f;
        while (t < 0.15f)
        {
            _cheerObj.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, t / 0.15f);
            t += Time.deltaTime; yield return null;
        }
        t = 0f;
        while (t < 0.1f)
        {
            _cheerObj.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1.0f, t / 0.1f);
            t += Time.deltaTime; yield return null;
        }
        _cheerObj.transform.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(0.55f);

        // Fade out + float up 40px
        t = 0f;
        Color startCol = _cheerTxt.color;
        while (t < 0.35f)
        {
            float pct = t / 0.35f;
            Color c = startCol; c.a = 1f - pct;
            _cheerTxt.color = c;
            rt.anchoredPosition = basePos + Vector2.up * (40f * pct);
            t += Time.deltaTime; yield return null;
        }

        _cheerObj.SetActive(false);
        rt.anchoredPosition = basePos;
        Color fc = startCol; fc.a = 1f;
        _cheerTxt.color = fc;
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  BUILDER HELPERS
    // ════════════════════════════════════════════════════════════════════════════

    GameObject FullPanel(string name, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_canvasT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        if (bg.a > 0f)
        {
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.MakeWhiteSquare();
            img.color  = bg;
        }
        else
        {
            go.AddComponent<Image>().color = Color.clear;
        }
        return go;
    }

    GameObject Panel(Transform parent, string name, Color bg, float x, float y, float w, float h, float corner)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        var img = go.AddComponent<Image>();
        img.sprite = corner > 0
            ? SpriteFactory.MakeRoundedRect((int)w, (int)h, corner, Color.white)
            : SpriteFactory.MakeWhiteSquare();
        img.color = bg;
        img.type  = corner > 0 ? Image.Type.Simple : Image.Type.Sliced;
        img.raycastTarget = false;
        if (bg.a > 0.12f && corner > 0f)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.78f, 1f, 0.16f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.01f, 0.08f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -7f);
        }
        return go;
    }

    Text Txt(Transform parent, string name, string content, int size, Color col, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font      = _font;
        t.text      = content;
        t.fontSize  = size;
        t.color     = col;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        if (bold)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.02f, 0.09f, 0.68f);
            shadow.effectDistance = new Vector2(1.5f, -2f);
        }
        return t;
    }

    Image Img(Transform parent, string name, Sprite spr)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.sprite = spr;
        img.preserveAspect = true;
        img.raycastTarget  = false;
        return img;
    }

    void Btn(Transform parent, string name, string label, int fontSize, Color col,
             float x, float y, float w, float h, float corner, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);

        var img = go.AddComponent<Image>();
        img.sprite = corner > 0
            ? SpriteFactory.MakeRoundedRect((int)w, (int)h, corner, Color.white)
            : SpriteFactory.MakeWhiteSquare();
        img.color = col;
        if (col.a > 0.12f && corner > 0f)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.92f, 1f, 0.26f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.01f, 0.08f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -8f);
        }

        if (col.a > 0.12f && corner > 0f)
        {
            // Shared Balloon Pop finish: soft top gloss, grounded shade and a tiny orb.
            var shine = Panel(go.transform, "TopGloss", new Color(1f, 1f, 1f, 0.095f), 0, h * 0.22f, w - 18f, h * 0.42f, corner * 0.72f);
            shine.GetComponent<Image>().raycastTarget = false;
            var shade = Panel(go.transform, "BottomShade", new Color(0f, 0.02f, 0.10f, 0.10f), 0, -h * 0.31f, w - 14f, h * 0.25f, corner * 0.55f);
            shade.GetComponent<Image>().raycastTarget = false;
            if (!string.IsNullOrEmpty(label) && w >= 300f)
            {
                var orb = Img(go.transform, "BalloonDot", SpriteFactory.MakeCircleGloss(40, Color.Lerp(col, Color.white, 0.35f), Color.white));
                Pos(orb, -w * 0.5f + 48f, 0, 28, 28);
            }
        }

        if (!string.IsNullOrEmpty(label))
        {
            var t = Txt(go.transform, "Lbl", label, fontSize, Color.white, true);
            Fill(t.GetComponent<RectTransform>());
        }

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colours = btn.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colours.pressedColor = new Color(0.72f, 0.86f, 1f, 0.90f);
        colours.fadeDuration = 0.08f;
        btn.colors = colours;
        go.AddComponent<PremiumButtonFeedback>();
        btn.onClick.AddListener(action);
    }

    void IconBtn(Transform parent, string name, Sprite icon, Color bg,
                 float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);

        var img = go.AddComponent<Image>();
        img.sprite = SpriteFactory.MakeRoundedRect((int)w, (int)h, 20f, Color.white);
        img.color  = bg;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.01f, 0.08f, 0.7f);
        shadow.effectDistance = new Vector2(0, -7);

        var ico = Img(go.transform, "Icon", icon);
        Fill(ico.GetComponent<RectTransform>());

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        go.AddComponent<PremiumButtonFeedback>();
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.85f, 1f, 0.3f);
        outline.effectDistance = new Vector2(2, -2);
        ico.rectTransform.offsetMin = new Vector2(18, 18);
        ico.rectTransform.offsetMax = new Vector2(-18, -18);
    }

    // Pos overloads
    void Pos(Image img, float x, float y, float w, float h)
        => Pos(img.GetComponent<RectTransform>(), x, y, w, h);

    void Pos(Text txt, float x, float y, float w, float h)
        => Pos(txt.GetComponent<RectTransform>(), x, y, w, h);

    void Pos(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public void Show(GameObject go)    { if (go != null) go.SetActive(true); }
    public void Hide(GameObject go)    { if (go != null) go.SetActive(false); }

    public void ShowOnly(GameObject target)
    {
        GameObject[] panels = {
            _homePanel, _hudPanel, _pausePanel, _settingsPanel,
            _gameOverPanel, _levelSelectPanel, _profilePanel
        };
        foreach (var panel in panels)
            if (panel != null) panel.SetActive(panel == target);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  ANCILLARY COMPONENTS  (outside UIManager class)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Gently floats a UI element up and down via a sin wave.</summary>
public class FloatAnim : MonoBehaviour
{
    public float offset = 0f;
    private float _speed = 1.2f;
    private float _amount = 12f;
    private Vector2 _base;
    private RectTransform _rt;

    void Start()
    {
        _rt   = GetComponent<RectTransform>();
        _base = _rt.anchoredPosition;
    }

    void Update()
    {
        if (_rt == null) return;
        float y = Mathf.Sin((Time.time + offset) * _speed) * _amount;
        _rt.anchoredPosition = _base + Vector2.up * y;
    }
}

/// <summary>Scales a UI element from 0 to 1 with a light overshoot.</summary>
public class BounceIn : MonoBehaviour
{
    public float delay = 0f;

    void OnEnable()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        while (t < 0.35f)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.18f, t / 0.35f);
            t += Time.unscaledDeltaTime; yield return null;
        }
        t = 0f;
        while (t < 0.20f)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, t / 0.20f);
            t += Time.unscaledDeltaTime; yield return null;
        }
        transform.localScale = Vector3.one;
    }
}

/// <summary>Continuously pulses a UI element's scale with a sin wave.</summary>
public class PulseAnim : MonoBehaviour
{
    public float speed  = 1.4f;
    public float amount = 0.06f;

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * amount;
        transform.localScale = Vector3.one * s;
    }
}

/// <summary>Keeps a shadow Text element in sync with the main cheer Text.</summary>
public class CheerShadowSync : MonoBehaviour
{
    public Text shadow;

    void LateUpdate()
    {
        if (shadow == null) return;
        var main = GetComponent<Text>();
        if (main != null) shadow.text = main.text;
    }
}
