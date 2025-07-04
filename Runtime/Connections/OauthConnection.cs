
using nseutils.unityoauth.Browser;
using nseutils.unityoauth.Clients;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace nseutils.unityoauth
{

    public abstract class OauthConnection
    {
        public readonly AuthenticationSession session;



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


        public delegate void SignedInSuccessDelegate();
        public delegate void AccessTokenValidationResponse(bool validateSuccess);
        public delegate void SignInFailedDelegate();
        public delegate void SignedOutDelegate();
        public delegate void UserInfoReceivedDelegate(IOauthUserInfo userInfo);

        internal event SignedInSuccessDelegate singedSuccess;
        internal event SignedOutDelegate singedOut;
        internal event SignInFailedDelegate signedFail;

        protected void SignedInSuccess() => singedSuccess?.Invoke();
        protected void SignedInFailed() => signedFail?.Invoke();
        //protected void UserInfoReceived(IOauthUserInfo userInfo) => OnUserInfoReceived?.Invoke(userInfo);

        protected void SignedOut()
        {
            accessTokenResponse = null;
            session.authenticatedUser = null;
            singedOut?.Invoke();
        }


        public virtual async UniTask<bool> UTask_Authenticate()
        {
            accessTokenResponse = null;

            await UniTask.SwitchToMainThread();
            //UniTask<Cdm.Authentication.OAuth2.AccessTokenResponse> task = session.UTask_Authenticate();
            AccessTokenResponse _tokenResponse = await session.UTask_RequestAccessToken();

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
#if UNITY_EDITOR
            //Debug.Log($"SignedInSuccess..."); 
#endif

            bool _authenticateUser = await UTask_FetchUserInfo();
            if (_authenticateUser)
                SignedInSuccess();
            else
                SignedInFailed();
            return _authenticateUser;
        }

        public abstract IEnumerator Authenticate();

        public virtual void _SignOut()
        {
            accessTokenResponse = null;
            singedOut?.Invoke();
        }
        public abstract IEnumerator SignOut();


        public virtual async UniTask<bool> UTask_FetchUserInfo()
        {
            //if (!session.supportsUserInfo)
            //{
            //    Debug.LogError($"User info is not supported by this provider");
            //    return false;
            //}

            session.authenticatedUser = await session.UTask_GetUserInfo();

            bool _success = session.authenticatedUser != null && !string.IsNullOrEmpty(session.authenticatedUser.id);


            //UserInfoReceived(session.authenticatedUser);
            return _success;
        }
        //public abstract IEnumerator FetchUserInfo();

        public abstract void SignWebRequest(UnityWebRequest webRequest);

        public virtual async UniTaskVoid UTask_Refresh()
        {
            //TODO
            await UniTask.Yield();
        }
        public abstract IEnumerator Refresh();

        internal async void SetAuthenticationToken(string value)
        {
            session.codeflow.SetAuthenticationToken(value);
            bool _validadeResult = await UTask_FetchUserInfo();
            if (_validadeResult)
            {
                SignedInSuccess();
            }
            else
            {
                session.codeflow.SetAuthenticationToken(null);
                SignedInFailed();
            }
        }

        ~OauthConnection()
        {
            session.Dispose();
            singedSuccess = null;
            singedOut = null;
            signedFail = null;
            //OnUserInfoReceived = null;

        }
    }
}
