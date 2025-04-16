using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace nseutils.unityoauth.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser that waits for a call with
    /// the authorization verification code through a custom scheme (aka protocol).
    /// </summary>
    /// <see href="https://docs.unity3d.com/ScriptReference/Application-deepLinkActivated.html"/>
    public class DeepLinkBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        public bool useVitualRedirectUrl => false;

        public async Task<BrowserResult> StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default)
        {
            OAuthManager.Instance.onDeeplinkActivated += OnDeepLinkActivated;

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });
            //OAuthManager.Instance.onDeeplinkActivated += OnDeepLinkActivated;
            //Application.deepLinkActivated += OnDeepLinkActivated;

            try
            {
                Application.OpenURL(loginUrl);
                return await _taskCompletionSource.Task;
            }
            finally
            {

            }
        }

        private void OnDeepLinkActivated(string url)
        {

            Debug.Log($"OnDeepLinkActivated :: {url}");

            _taskCompletionSource.SetResult(
                new BrowserResult(BrowserStatus.Success, url));

            if (OAuthManager.Instance != null)
                OAuthManager.Instance.onDeeplinkActivated -= OnDeepLinkActivated;

        }

        public async UniTask<BrowserResult> UTask_StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl)
        {
            OAuthManager.Instance.onDeeplinkActivated += OnDeepLinkActivated;

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            //OAuthManager.Instance.onDeeplinkActivated += OnDeepLinkActivated;
            //Application.deepLinkActivated += OnDeepLinkActivated;

            try
            {
                Application.OpenURL(loginUrl);
                return await _taskCompletionSource.Task;
            }
            finally
            {

            }
        }

        //constuctor class when GC destry it
        ~DeepLinkBrowser()
        {
            if (OAuthManager.Instance != null)
                OAuthManager.Instance.onDeeplinkActivated -= OnDeepLinkActivated;

        }
    }

}