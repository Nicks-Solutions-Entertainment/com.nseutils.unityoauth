
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace nseutils.unityoauth
{

    public class OAuthManager : MonoBehaviour
    {

        static OAuthManager _instance;
        public static OAuthManager Instance => _instance;

        public delegate void UserInfoReceived(IOauthUserInfo userInfo);
        public delegate void DeepLinkDelegate(string deeplinkUrl);

        internal DeepLinkDelegate onDeeplinkActivated;
        public Action _signedIn;
        public UserInfoReceived _userInfoReceived;
        public Action _signInFailed;
        public Action _signedOut;


        public IOauthUserInfo logedUserInfo
        {
            get => connection.session.authenticatedUser;
        }
        public string applicationAbsoluteURL
        {
            private set; get;
        }

        internal OauthConnection connection
        {
            get; private set;
        }

        public string authenticationToken
        {
            get => connection.session.codeflow.authenticationToken;
            set {
                if (connection != null) connection.SetAuthenticationToken(value);
                else
                    Debug.Log($"connection on set yet");
            }
        }

        public void RegisterUserLogedCallback(UserInfoReceived handler)
        {
            _userInfoReceived = handler;
        }
        public void UnregisterUserLogedCallback() => _userInfoReceived = null;

        [SerializeField]
        private OAuthAppProfile appProfile;

        //public void SignIn() => StartCoroutine(_SignIn());
        public void SignIn() => UTask_SignIn();

        public void SignOut() => StartCoroutine(_SignOut());

        async void UTask_SignIn()
        {
            applicationAbsoluteURL = Application.absoluteURL;
            bool _authenticateResult = await connection.UTask_Authenticate();

        }

        [ContextMenu("TestDictionary")]
        void TestDictionary()
        {
            AuthorizationCodeRequest req = new AuthorizationCodeRequest()
            {
                clientId = "v_clientId",
                scope = "v_scope",
                redirectUri = "v_redirectUri",
                codeChallenge = "v_codeChallenge",

            };

            string reqStr = JsonConvert.SerializeObject(req);
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(reqStr);
            string _ = "{";
            foreach (var item in dict)
                _ += $"  {item.Key} : {item.Value},\n";

            _ += "}";
            Debug.Log(_);
        }

        IEnumerator _SignOut()
        {
            yield return connection.SignOut();
        }

        //void UTask_FetchUserInfo()
        //{

        //    connection.UTask_FetchUserInfo();
        //}
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

        public void SetAppProfile(OAuthAppProfile _appProfile)
        {
            if (connection != null)
            {
                Debug.LogError($"Oauth must to be Stoped first.");
            }
            appProfile = _appProfile;
        }

        public void StartOauth()
        {
            if (appProfile == null)
            {
                Debug.LogError("No OAuthAppProfile assigned");
                return;
            }

            connection = new StdOauthConection(appProfile.oauthAppInfos);

            connection.singedSuccess += OnSignInSuccess;
            connection.singedOut += OnSignOut;
            connection.signedFail += OnFailSignIn;
            //connection.OnUserInfoReceived += OnUserInfoReceived;

            Debug.Log($"Oauth Started with {appProfile.oauthAppInfos.identityProvider}");
        }

        public void StopOauth()
        {
            if (connection != null)
            {
                connection.singedSuccess -= OnSignInSuccess;
                connection.singedOut -= OnSignOut;
                connection.signedFail -= OnFailSignIn;
                //connection.OnUserInfoReceived -= OnUserInfoReceived;

            }
            connection = null;
            Debug.Log($"Oauth Stoped!");
        }

        private void OnSignInSuccess()
        {
            _signedIn?.Invoke();
            //UTask_FetchUserInfo();
        }


        private void OnSignOut()
        {
            _signedOut?.Invoke();
        }

        private void OnFailSignIn()
        {
            _signInFailed?.Invoke();
        }

        private void OnUserInfoReceived(IOauthUserInfo userInfo)
        {
            _userInfoReceived?.Invoke(userInfo);
        }
    }

}