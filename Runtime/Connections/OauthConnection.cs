
using nseutils.unityoauth.Browser;
using nseutils.unityoauth.Clients;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace nseutils.unityoauth
{

    public abstract class OauthConnection
    {
        protected readonly AuthenticationSession session;
        protected AccessTokenResponse accessTokenResponse;

        const string virtualRedirectUrl = "http://localhost:5000/auth/callback.html";

        internal bool useVitualRedirectUrl => session != null && session.useVitualRedirectUrl;

        public OauthConnection(OauthAppInfos oauthProfileInfos)
        {
            CrossPlatformBrowser _browser = new CrossPlatformBrowser();
            //Editor
            {
                _browser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser(true));
                _browser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser(true));
                _browser.platformBrowsers.Add(RuntimePlatform.LinuxEditor, new StandaloneBrowser(true));
            }
            //Computer
            {
                _browser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser(true));
                _browser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser(true));
                _browser.platformBrowsers.Add(RuntimePlatform.LinuxPlayer, new StandaloneBrowser(true));
            }
            //Mobile
            {
                _browser.platformBrowsers.Add(RuntimePlatform.Android, new DeepLinkBrowser());
                _browser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new DeepLinkBrowser());
            }
            //WebGL
            {
                //_browser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new StandaloneBrowser());
                _browser.platformBrowsers.Add(RuntimePlatform.WebGLPlayer, new WebGLBrowser());
            }

            OauthAppConfiguration oauthConfig = new OauthAppConfiguration()
            {
                useVirtualRedirectUrl = _browser.useVitualRedirectUrl,
                clientId = oauthProfileInfos.clientId,
                clientSecret = oauthProfileInfos.clientSecret,
                virtualRedirectUri = virtualRedirectUrl,
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

            Debug.Log($"session created :: suports userInfos:{session.supportsUserInfo}");

        }

        internal bool HasRefreshToken(out DateTime? expiresAt)
        {
            expiresAt = accessTokenResponse.expiresAt;
            return accessTokenResponse.HasRefreshToken() && accessTokenResponse.expiresAt != null;
        }


        public delegate void OnSignedInDelegate(AccessTokenResponse accessTokenResponse);
        public delegate void OnSignInFailedDelegate();
        public delegate void OnSignedOutDelegate();
        public delegate void OnUserInfoReceivedDelegate(IOauthUserInfo userInfo);

        internal event OnSignedInDelegate OnSignedIn;
        internal event OnSignedOutDelegate OnSignedOut;
        internal event OnSignInFailedDelegate OnSignInFailed;
        internal event OnUserInfoReceivedDelegate OnUserInfoReceived;

        protected void SignedInSuccess(AccessTokenResponse accessTokenResponse) => OnSignedIn?.Invoke(accessTokenResponse);
        protected void SignedInFailed() => OnSignInFailed?.Invoke();
        protected void UserInfoReceived(IOauthUserInfo userInfo) => OnUserInfoReceived?.Invoke(userInfo);

        protected void SignedOut()
        {
            accessTokenResponse = null;
            OnSignedOut?.Invoke();
        }


        public virtual async UniTask<bool> UTask_Authenticate()
        {
            accessTokenResponse = null;

            await UniTask.SwitchToMainThread();
            //UniTask<Cdm.Authentication.OAuth2.AccessTokenResponse> task = session.UTask_Authenticate();
            AccessTokenResponse _tokenResponse = await session.UTask_Authenticate();

            //await UniTask.WaitUntil(() => task.Status.IsCompleted() || task.Status.IsCanceled());
            //await UniTask.WaitUntil(() => task.IsCompleted || task.IsCanceled);

            //if (task.Status != UniTaskStatus.Pending)
            if (_tokenResponse == null)
            //if (task.Status != TaskStatus.RanToCompletion)
            {
                SignedInFailed();
                return false;
            }
            accessTokenResponse = _tokenResponse;
            Debug.Log($"SignedInSuccess...");

            SignedInSuccess(accessTokenResponse);
            return true;
        }

        public abstract IEnumerator Authenticate();

        public virtual void _SignOut()
        {
            accessTokenResponse = null;
            OnSignedOut?.Invoke();
        }
        public abstract IEnumerator SignOut();


        public virtual async void UTask_FetchUserInfo()
        {
            if (!session.supportsUserInfo)
            {
                Debug.LogError($"User info is not supported by this provider");
                return;
            }

            var _userInfo = await session.UTask_GetUserInfo();
            UserInfoReceived(_userInfo);
        }
        //public abstract IEnumerator FetchUserInfo();

        public abstract void SignWebRequest(UnityWebRequest webRequest);

        public virtual async UniTaskVoid UTask_Refresh()
        {
            //TODO
            await UniTask.Yield();
        }
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
}
