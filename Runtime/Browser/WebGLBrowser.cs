
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace nseutils.unityoauth.Browser
{
    public class WebGLBrowser : IBrowser
    {
        public bool useVitualRedirectUrl => false;
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;
        WebGlOauthListener m_listener;

        public WebGLBrowser()
        {
            m_listener = WebGlOauthListener.Instance;
            m_listener.OnSignedIn += IncomingResult;
            PrepareOauth();
        }


        internal void ApplyResult(string redirectUrl)
        {
            _taskCompletionSource.SetResult(new BrowserResult(BrowserStatus.Success, redirectUrl));
        }




#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void PrepareOauth();

        [DllImport("__Internal")]
        private static extern void StartSignin(string authenticateUrl);
#else

        private void PrepareOauth()
        {

        }
        private void StartSignin(string authenticateUrl)
        {
            Debug.Log($"StartSignin ::{authenticateUrl}");
            Application.OpenURL(authenticateUrl);
        }


#endif

        public async UniTask<BrowserResult> UTask_StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            redirectUrl = AddForwardSlashIfNecessary(redirectUrl);
            Debug.Log($"StartAsync start listening... {loginUrl}");
            StartSignin(loginUrl);
            while (!m_listener.finished)
            {
                await UniTask.Yield();
            }
            await UniTask.WaitUntil(() => m_listener.finished);

            return await _taskCompletionSource.Task;
        }

        public async Task<BrowserResult> StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            redirectUrl = AddForwardSlashIfNecessary(redirectUrl);
            Debug.Log($"StartAsync start listening... {loginUrl}");
            StartSignin(loginUrl);
            while (!m_listener.finished)
                await Task.Delay(1);

            return await _taskCompletionSource.Task;
        }


        private void IncomingResult(string redirectUrl)
        {
            Debug.Log($"IncomingResult :: {redirectUrl}");
            Uri url = new Uri(redirectUrl);
            bool isUnauthorized = url.Query.Contains("error=access_denied");

            var browserResult = isUnauthorized
                ? new BrowserResult(BrowserStatus.UnknownError, url.ToString(), "Usuário não autorizado")
                : new BrowserResult(BrowserStatus.Success, url.ToString());


            _taskCompletionSource.SetResult(browserResult);
            //m_listener.OnSignedIn
            m_listener.FinishListen();
        }

        /// <summary>
        /// Prefixes must end in a forward slash ("/")
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=net-7.0#remarks" />
        private string AddForwardSlashIfNecessary(string url)
        {
            string forwardSlash = "/";
            if (!url.EndsWith(forwardSlash))
            {
                url += forwardSlash;
            }

            return url;
        }

    }
}
