using Cdm.Authentication;
using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class OauthConnection
{
    protected readonly AuthenticationSession session;
    protected AccessTokenResponse accessTokenResponse;

    public OauthConnection(OauthAppInfos oauthProfileInfos)
    {
        CrossPlatformBrowser _browser = new CrossPlatformBrowser();
        //Editor
        {
            _browser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
            _browser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
            _browser.platformBrowsers.Add(RuntimePlatform.LinuxEditor, new StandaloneBrowser());
        }
        //Computer
        {
            _browser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
            _browser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
            _browser.platformBrowsers.Add(RuntimePlatform.LinuxPlayer, new StandaloneBrowser());
        }
        //Mobile
        {
            _browser.platformBrowsers.Add(RuntimePlatform.Android, new DeepLinkBrowser());
            _browser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new DeepLinkBrowser());
        }
        //WebGL
        {
            _browser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new StandaloneBrowser());
        }

        AuthorizationCodeFlow.Configuration oauthConfig = new AuthorizationCodeFlow.Configuration()
        {
            clientId = oauthProfileInfos.clientId,
            clientSecret = oauthProfileInfos.clientSecret,
            redirectUri = oauthProfileInfos.redirectUri,
            scope = oauthProfileInfos.scope,

        };

        AuthorizationCodeFlow _codeFlow = oauthProfileInfos.identityProvider switch
        {
            OauthProvider.MS_EntraID => new MSEntraIDAuth(oauthConfig, oauthProfileInfos.entraIdTenant),
            OauthProvider.Google => new GoogleAuth(oauthConfig),
            OauthProvider.GitHub => new GitHubAuth(oauthConfig),
            OauthProvider.Facebook => new FacebookAuth(oauthConfig),
            _ => throw new System.NotImplementedException($" This Package has no support to this OauthProvider ({oauthProfileInfos.identityProvider}) yet. Sorry"),
        };

        session = new AuthenticationSession(_codeFlow, _browser);
    }

    internal bool HasRefreshToken(out DateTime? expiresAt)
    {
        expiresAt = accessTokenResponse.expiresAt;
        return accessTokenResponse.HasRefreshToken() && accessTokenResponse.expiresAt != null;
    }


    public delegate void OnSignedInDelegate(AccessTokenResponse accessTokenResponse);
    public delegate void OnSignInFailedDelegate();
    public delegate void OnSignedOutDelegate();
    public delegate void OnUserInfoReceivedDelegate(IUserInfo userInfo);

    internal event OnSignedInDelegate OnSignedIn;
    internal event OnSignedOutDelegate OnSignedOut;
    internal event OnSignInFailedDelegate OnSignInFailed;
    internal event OnUserInfoReceivedDelegate OnUserInfoReceived;

    protected void SignedInSuccess(AccessTokenResponse accessTokenResponse) => OnSignedIn?.Invoke(accessTokenResponse);
    protected void SignedInFailed() => OnSignInFailed?.Invoke();
    protected void UserInfoReceived(IUserInfo userInfo) => OnUserInfoReceived?.Invoke(userInfo);

    protected void SignedOut()
    {
        accessTokenResponse = null;
        OnSignedOut?.Invoke();
    }


    public abstract IEnumerator Authenticate();

    public abstract IEnumerator SignOut();

    public abstract IEnumerator FetchUserInfo();

    public abstract void SignWebRequest(UnityWebRequest webRequest);

    public abstract IEnumerator Refresh();


    ~OauthConnection()
    {
        session.Dispose();
        OnSignedIn = null;
        OnSignedOut = null;
        OnSignInFailed = null;
        OnUserInfoReceived = null;

    }
}
