using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace nseutils.unityoauth.Browser
{
    public class WKWebViewAuthenticationSessionBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;
        public bool useVitualRedirectUrl => false;

        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            // Discard URL parameters. They are not valid for iOS URL Scheme.
            redirectUrl = redirectUrl.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries)[0];
            virtualRedirectUrl = redirectUrl.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries)[0];

            using var authenticationSession =
                new WKWebViewAuthenticationSession(loginUrl, redirectUrl, AuthenticationSessionCompletionHandler);

            cancellationToken.Register(() => { _taskCompletionSource?.TrySetCanceled(); });

            try
            {
                if (!authenticationSession.Start())
                {
                    _taskCompletionSource.SetResult(
                        new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                }

                return await _taskCompletionSource.Task;
            }
            catch (TaskCanceledException)
            {
                // In case of timeout cancellation.
                authenticationSession?.Cancel();
                throw;
            }
        }

        public async UniTask<BrowserResult> UTask_StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl)
        {
            if (string.IsNullOrEmpty(loginUrl))
                throw new ArgumentNullException(nameof(loginUrl));

            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            // Discard URL parameters. They are not valid for iOS URL Scheme.
            redirectUrl = redirectUrl.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];
            virtualRedirectUrl = redirectUrl.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];

            using var authenticationSession =
                new WKWebViewAuthenticationSession(loginUrl, redirectUrl, AuthenticationSessionCompletionHandler);

            try
            {
                if (!authenticationSession.Start())
                {
                    _taskCompletionSource.SetResult(
                        new BrowserResult(BrowserStatus.UnknownError, "Browser could not be started."));
                }

                return await _taskCompletionSource.Task;
            }
            catch (TaskCanceledException)
            {
                // In case of timeout cancellation.
                authenticationSession?.Cancel();
                throw;
            }
        }

        private void AuthenticationSessionCompletionHandler(string callbackUrl,
            WKWebViewAuthenticationSessionError error)
        {
            if (error.code == WKWebViewAuthenticationSessionErrorCode.None)
            {
                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.Success, callbackUrl));
            }
            else if (error.code == WKWebViewAuthenticationSessionErrorCode.CanceledLogin)
            {
                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.UserCanceled, callbackUrl, error.message));
            }
            else
            {
                _taskCompletionSource.SetResult(
                    new BrowserResult(BrowserStatus.UnknownError, callbackUrl, error.message));
            }
        }
    }
}