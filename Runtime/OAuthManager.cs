using Cdm.Authentication;
using Cdm.Authentication.OAuth2;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class OAuthManager : MonoBehaviour
{

    static OAuthManager _instance;
    public static OAuthManager Instance => _instance;
    public delegate void UserInfoReceived(IUserInfo userInfo);
    public delegate void DeepLinkDelegate(string deeplinkUrl);

    internal DeepLinkDelegate onDeeplinkActivated;
    private Action _signedIn;
    private UserInfoReceived _userInfoReceived;
    private Action _signInFailed;
    private Action _signedOut;

    OauthConnection connection;

    public void RegisterUserLogedCallback(UserInfoReceived handler)
    {
        _userInfoReceived = handler;
    }
    public void UnregisterUserLogedCallback() => _userInfoReceived = null;

    [SerializeField]
    private OAuthAppProfile appProfile;
    //[SerializeField] OAuthAppProfile n_session;

    [SerializeField]
    private UnityEvent onSignedIn = new();

    [SerializeField]
    private UnityEvent onSignInFailed = new();

    [SerializeField]
    private UnityEvent onSignedOut = new();

    [SerializeField]
    private UnityEvent<IUserInfo> onUserInfoReceived = new();

    public void SignIn() => StartCoroutine(_SignIn());

    public void SignOut() => StartCoroutine(_SignOut());


    IEnumerator _SignIn()
    {

        yield return connection.Authenticate();
    }

    IEnumerator _RefreshBeforeExpiry()
    {
        if (!connection.HasRefreshToken(out DateTime? expiresAt) || !expiresAt.HasValue)
            yield break;


        int seconds = (int)Math.Min(0, Math.Floor((DateTime.Now - expiresAt).Value.TotalSeconds - 60));

        yield return new WaitForSecondsRealtime(seconds);

        yield return connection.Refresh();
    }
    IEnumerator _SignOut()
    {
        yield return connection.SignOut();
    }
    IEnumerator _FetchUserInfo()
    {
        yield return connection.FetchUserInfo();
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Application.deepLinkActivated += DeepLinkActivated;

        }
        else Destroy(gameObject);
    }
    private void Start()
    {
        //n_session = OAuthAppProfile.New(new OauthAppInfos(
        //    Netherlands3D.Authentication.IdentityProvider.AzureAD,
        //    "cliendId",
        //    "cSecret",
        //    "redirectUrl",
        //    "scope"));
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        Application.deepLinkActivated -= DeepLinkActivated;

        onDeeplinkActivated = null;
        _signedIn = null;
        _userInfoReceived = null;
        _signInFailed = null;
        _signedOut = null;


    }

    private void DeepLinkActivated(string deeplinkUrl) => onDeeplinkActivated?.Invoke(deeplinkUrl);

    private void OnEnable()
    {
        if (!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
            connection = new WebGLOauthConnection(appProfile.oauthAppInfos);
        else
            connection = new StdOauthConection(appProfile.oauthAppInfos);

        //appProfile.Initialize();
        connection.OnSignedIn += OnSignInSuccess;
        connection.OnSignedOut += OnSignOut;
        connection.OnSignInFailed += OnFailSignIn;
        connection.OnUserInfoReceived += OnUserInfoReceived;
        //appProfile.OnSignedOut.AddListener(OnSignOut);
        //appProfile.OnSignInFailed.AddListener(OnFailSignIn);
        //appProfile.OnUserInfoReceived.AddListener(OnUserInfoReceived);
    }


    private void OnSignInSuccess(AccessTokenResponse accessTokenResponse)
    {
        onSignedIn?.Invoke();
        StartCoroutine(_FetchUserInfo());
        StartCoroutine(_RefreshBeforeExpiry());
    }


    private void OnSignOut()
    {
        onSignedOut?.Invoke();
    }

    private void OnFailSignIn()
    {
        onSignInFailed?.Invoke();
    }

    private void OnUserInfoReceived(IUserInfo userInfo)
    {
        onUserInfoReceived?.Invoke(userInfo);
    }
}
