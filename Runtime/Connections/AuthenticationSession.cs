using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using nseutils.unityoauth.Browser;
using nseutils.unityoauth.Clients;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace nseutils.unityoauth
{
    public class AuthenticationSession : IDisposable
    {
        public readonly AuthorizationCodeFlow codeflow;
        private readonly IBrowser _browser;

        internal IOauthUserInfo authenticatedUser;

        public bool useVitualRedirectUrl => _browser.useVitualRedirectUrl;

        public TimeSpan loginTimeout { get; set; } = TimeSpan.FromMinutes(10);

        public AuthenticationSession(AuthorizationCodeFlow _codeflow, IBrowser browser)
        {
            codeflow = _codeflow;
            _browser = browser;

        }

        public bool supportsUserInfo => codeflow.supportsUserInfo;

        public bool ShouldAuthenticate()
        {
            return codeflow.ShouldRequestAuthorizationCode();
        }


        public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync()
        {
            var tokenResponse = await codeflow.GetOrRefreshTokenAsync();
            return tokenResponse.GetAuthenticationHeader();
        }

        internal async UniTask<AccessTokenResponse> UTask_RequestAccessToken()
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(loginTimeout);

            try
            {
                // 1. Create authorization request URL.
                //Debug.Log("Making authorization request...");

                string redirectUrl = codeflow.configuration.redirectUri;
                string visutlRedirectUri = codeflow.configuration.virtualRedirectUri;
                string authorizationUrl = codeflow.GetAuthorizationUrl();
                //Debug.Log($"AuthenticateAsync :: {authorizationUrl}");



                if (string.IsNullOrEmpty(authorizationUrl))
                {
                    Debug.LogException(new AuthenticationException(AuthenticationError.Other,
                        "Authorization URL is not set."));
                    return null;
                }

                var browserResult =
                    await _browser.UTask_StartAsync(authorizationUrl, redirectUrl, visutlRedirectUri);
                Debug.Log($"AuthenticateAsync :: result {browserResult.status}");
                if (browserResult.status == BrowserStatus.Success)
                {
                    // 3. Exchange authorization code for access and refresh tokens.
                    //Debug.Log("Exchanging authorization code for access and refresh tokens...");

#if UNITY_EDITOR
                    //Debug.Log($"Redirect URL: {browserResult.redirectUrl}");
#endif
                    return await codeflow.UTask_ExchangeCodeForAccessToken(browserResult.redirectUrl);
                }

                if (browserResult.status == BrowserStatus.UserCanceled)
                {
                    throw new AuthenticationException(AuthenticationError.Cancelled, browserResult.error);
                }

                throw new AuthenticationException(AuthenticationError.Other, browserResult.error);
            }
            catch (TaskCanceledException e)
            {
                if (timeoutCancellationTokenSource.IsCancellationRequested)
                    throw new AuthenticationException(AuthenticationError.Timeout, "Operation timed out.");

                throw new AuthenticationException(AuthenticationError.Cancelled, "Operation was cancelled.", e);
            }
        }

        /// <summary>
        /// Asynchronously authorizes the installed application to access user's protected data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <exception cref="AuthorizationCodeRequestException"></exception>
        /// <exception cref="AccessTokenRequestException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        internal async Task<AccessTokenResponse> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(loginTimeout);

            try
            {
                // 1. Create authorization request URL.
                //Debug.Log("Making authorization request...");

                string redirectUrl = codeflow.configuration.redirectUri;
                string visutlRedirectUri = codeflow.configuration.virtualRedirectUri;
                string authorizationUrl = codeflow.GetAuthorizationUrl();
                //Debug.Log($"AuthenticateAsync :: {authorizationUrl}");



                if (string.IsNullOrEmpty(authorizationUrl))
                {
                    Debug.LogException(new AuthenticationException(AuthenticationError.Other,
                        "Authorization URL is not set."));
                    return null;
                }

                // 2. Get authorization code grant using login form in the browser.
                //Debug.Log("Getting authorization grant using browser login...");


                using var loginCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCancellationTokenSource.Token);

                var browserResult =
                    await _browser.StartAsync(authorizationUrl, redirectUrl, visutlRedirectUri, loginCancellationTokenSource.Token);
                Debug.Log($"AuthenticateAsync :: result {browserResult.status}");
                if (browserResult.status == BrowserStatus.Success)
                {
                    // 3. Exchange authorization code for access and refresh tokens.
                    //Debug.Log("Exchanging authorization code for access and refresh tokens...");

#if UNITY_EDITOR
                    //Debug.Log($"Redirect URL: {browserResult.redirectUrl}");
#endif
                    return await codeflow.UTask_ExchangeCodeForAccessToken(browserResult.redirectUrl);
                }

                if (browserResult.status == BrowserStatus.UserCanceled)
                {
                    throw new AuthenticationException(AuthenticationError.Cancelled, browserResult.error);
                }

                throw new AuthenticationException(AuthenticationError.Other, browserResult.error);
            }
            catch (TaskCanceledException e)
            {
                if (timeoutCancellationTokenSource.IsCancellationRequested)
                    throw new AuthenticationException(AuthenticationError.Timeout, "Operation timed out.");

                throw new AuthenticationException(AuthenticationError.Cancelled, "Operation was cancelled.", e);
            }
        }

        /// <inheritdoc cref="AuthorizationCodeFlow.GetOrRefreshTokenAsync"/>
        internal async Task<AccessTokenResponse> GetOrRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await codeflow.GetOrRefreshTokenAsync(cancellationToken);
        }

        /// <inheritdoc cref="AuthorizationCodeFlow.RefreshTokenAsync(System.Threading.CancellationToken)"/>
        internal async Task<AccessTokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await codeflow.RefreshTokenAsync(cancellationToken);
        }

        /// <inheritdoc cref="AuthorizationCodeFlow.RefreshTokenAsync(string,System.Threading.CancellationToken)"/>
        internal async Task<AccessTokenResponse> RefreshTokenAsync(string refreshToken,
            CancellationToken cancellationToken = default)
        {
            return await codeflow.RefreshTokenAsync(refreshToken, cancellationToken);
        }

        internal async UniTask<IOauthUserInfo> UTask_GetUserInfo() 
        {
            if (!supportsUserInfo)
                return null;
            return await (codeflow as IOauthUserInfoCompatible).GetUserInfos();
        }


        public void Dispose()
        {
            codeflow?.Dispose();
        }
    }
}