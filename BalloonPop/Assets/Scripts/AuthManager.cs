using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class AuthUser
{
    public string id;
    public string name;
    public string email;
    public string picture;
    public string createdAt;
    public string lastLoginAt;
}

[Serializable]
class AuthConfigData
{
    public string apiBaseUrl = "http://localhost:3000";
    public string appId = "balloonpop";
    public string googleWebClientId = "";
}

[Serializable]
class AuthResponse
{
    public bool ok;
    public string message;
    public string token;
    public AuthUser user;
}

[Serializable]
class NativeGoogleRequest
{
    public string app;
    public string idToken;
}

/// <summary>
/// Browser-based Google OAuth client. The API owns Google secrets and MongoDB access;
/// the Unity APK receives only an opaque app session token.
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }
    public static event Action StateChanged;

    const string TokenKey = "AuthSessionToken";
    string _apiBaseUrl;
    string _appId;
    string _googleWebClientId;
    string _token;
    public AuthUser User { get; private set; }
    public bool IsLoggedIn => User != null && !string.IsNullOrEmpty(_token);
    public bool IsBusy { get; private set; }
    public string Status { get; private set; } = "Sign in to save your player profile";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var asset = Resources.Load<TextAsset>("AuthConfig");
        var config = asset != null ? JsonUtility.FromJson<AuthConfigData>(asset.text) : new AuthConfigData();
        _apiBaseUrl = (config.apiBaseUrl ?? "").TrimEnd('/');
        _appId = string.IsNullOrEmpty(config.appId) ? "balloonpop" : config.appId.Trim().ToLowerInvariant();
        _googleWebClientId = (config.googleWebClientId ?? "").Trim();
        _token = PlayerPrefs.GetString(TokenKey, "");
        Application.deepLinkActivated += OnDeepLink;

        if (!string.IsNullOrEmpty(Application.absoluteURL)) OnDeepLink(Application.absoluteURL);
        else if (!string.IsNullOrEmpty(_token)) StartCoroutine(LoadProfile());
    }

    void OnDestroy()
    {
        if (Instance == this) Application.deepLinkActivated -= OnDeepLink;
    }

    public void Login()
    {
        if (IsBusy) return;
        if (string.IsNullOrEmpty(_apiBaseUrl) || _apiBaseUrl.Contains("YOUR-API"))
        {
            Status = "Add your deployed API URL in Resources/AuthConfig.json";
            StateChanged?.Invoke();
            return;
        }
        Status = "Opening Google sign-in…";
        IsBusy = true;
        StateChanged?.Invoke();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(_googleWebClientId))
        {
            IsBusy = false;
            Status = "Google Android client is not configured";
            StateChanged?.Invoke();
            return;
        }
        try
        {
            using (var bridge = new AndroidJavaClass("com.gamixtv.balloonpop.auth.NativeGoogleAuth"))
                bridge.CallStatic("signIn", _googleWebClientId);
        }
        catch (Exception error)
        {
            IsBusy = false;
            Status = "Native Google sign-in is unavailable: " + error.Message;
            StateChanged?.Invoke();
        }
#else
        IsBusy = false;
        Application.OpenURL(_apiBaseUrl + "/auth/google?app=" + UnityWebRequest.EscapeURL(_appId));
#endif
    }

    // Called by the Android Credential Manager bridge through UnitySendMessage.
    public void OnNativeGoogleToken(string idToken)
    {
        if (string.IsNullOrEmpty(idToken)) { OnNativeGoogleError("Google returned an empty identity token"); return; }
        StartCoroutine(ExchangeNativeToken(idToken));
    }

    public void OnNativeGoogleError(string message)
    {
        IsBusy = false;
        Status = string.IsNullOrEmpty(message) ? "Google sign-in was not completed" : message;
        StateChanged?.Invoke();
    }

    IEnumerator ExchangeNativeToken(string idToken)
    {
        IsBusy = true;
        Status = "Verifying your Google account…";
        StateChanged?.Invoke();
        var payload = new NativeGoogleRequest { app = _appId, idToken = idToken };
        byte[] body = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        using (var request = new UnityWebRequest(_apiBaseUrl + "/auth/google/native", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                if (response != null && response.ok && !string.IsNullOrEmpty(response.token))
                {
                    _token = response.token;
                    User = response.user;
                    PlayerPrefs.SetString(TokenKey, _token);
                    PlayerPrefs.Save();
                    Status = "Signed in with Google";
                }
                else Status = "Google account verification failed";
            }
            else
            {
                Status = request.responseCode == 401 ? "Google could not verify this app" : "Account service is unavailable";
            }
        }
        IsBusy = false;
        StateChanged?.Invoke();
    }

    public void Logout()
    {
        if (IsBusy) return;
        StartCoroutine(LogoutRoutine());
    }

    void OnDeepLink(string url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("balloonpop://auth", StringComparison.OrdinalIgnoreCase)) return;
        string token = QueryValue(url, "token");
        string error = QueryValue(url, "error");
        if (!string.IsNullOrEmpty(error))
        {
            Status = "Google sign-in was not completed";
            StateChanged?.Invoke();
            return;
        }
        if (string.IsNullOrEmpty(token)) return;
        _token = UnityWebRequest.UnEscapeURL(token);
        PlayerPrefs.SetString(TokenKey, _token);
        PlayerPrefs.Save();
        StartCoroutine(LoadProfile());
    }

    static string QueryValue(string url, string key)
    {
        int q = url.IndexOf('?');
        if (q < 0) return "";
        string[] pairs = url.Substring(q + 1).Split('&');
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split(new[] { '=' }, 2);
            if (kv.Length == 2 && kv[0] == key) return kv[1];
        }
        return "";
    }

    IEnumerator LoadProfile()
    {
        IsBusy = true;
        Status = "Loading your profile…";
        StateChanged?.Invoke();
        using (var request = UnityWebRequest.Get(_apiBaseUrl + "/api/me"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + _token);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                User = response != null ? response.user : null;
                Status = User != null ? "Signed in with Google" : "Could not load profile";
            }
            else
            {
                ClearSession();
                Status = request.responseCode == 401 ? "Session expired — please sign in again" : "Profile service is unavailable";
            }
        }
        IsBusy = false;
        StateChanged?.Invoke();
    }

    IEnumerator LogoutRoutine()
    {
        IsBusy = true;
        StateChanged?.Invoke();
        using (var request = UnityWebRequest.PostWwwForm(_apiBaseUrl + "/auth/logout", ""))
        {
            request.SetRequestHeader("Authorization", "Bearer " + _token);
            yield return request.SendWebRequest();
        }
        ClearSession();
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var bridge = new AndroidJavaClass("com.gamixtv.balloonpop.auth.NativeGoogleAuth"))
                bridge.CallStatic("clearCredentialState");
        }
        catch (Exception) { }
#endif
        Status = "Signed out safely";
        IsBusy = false;
        StateChanged?.Invoke();
    }

    void ClearSession()
    {
        _token = "";
        User = null;
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();
    }
}
