using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using nseutils.unityoauth.Utils;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace nseutils.unityoauth
{
    /// <summary>
    /// Supports 'Authorization Code' flow. Enables user sign-in and access to web APIs on behalf of the user.
    ///
    /// The OAuth 2.0 authorization code grant type, enables a client application to obtain
    /// authorized access to protected resources like web APIs. The auth code flow requires a user-agent that supports
    /// redirection from the authorization server back to your application. For example, a web browser, desktop,
    /// or mobile application operated by a user to sign in to your app and access their data.
    /// </summary>
    public abstract class AuthorizationCodeFlow : IDisposable
    {

        /// <summary>
        /// The endpoint for authorization server. This is used to get the authorization code.
        /// </summary>
        public abstract string authorizationUrl
        {
            get;
        }


        /// <summary>
        /// The endpoint for authentication server. This is used to exchange the authorization code for an access token.
        /// </summary>
        public abstract string accessTokenUrl
        {
            get;
        }

        //public abstract string userInfoUrl
        //{
        //    get;
        //}

        internal virtual bool supportsUserInfo => this is IOauthUserInfoCompatible;

        /// <summary>
        /// The state; any additional information that was provided by application and is posted back by service.
        /// </summary>
        /// <seealso cref="AuthorizationCodeRequest.state"/>
        public string state
        {
            get; private set;
        }


        /// <summary>
        /// Gets the client configuration for the authentication method.
        /// </summary>
        internal OauthAppConfiguration configuration
        {
            get;
        }

        internal AccessTokenResponse accessTokenResponse
        {
            get; private set;
        }
        protected HttpClient httpClient
        {
            get; set;
        }

        protected string codeVerifier
        {
            get; set;
        }
        protected static TUserInfo DefaultUserInfo<TUserInfo>(IOauthUserInfo info) where TUserInfo : IOauthUserInfo
        {
            return (TUserInfo)info;
        }


        public AuthorizationCodeFlow(OauthAppConfiguration _configuration)
        {
            configuration = _configuration;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true
            };
            codeVerifier = GenerateCodeVerifier();
        }

        internal string authenticationToken
        {
            get
            {
                var authenticationHeader = accessTokenResponse.GetAuthenticationHeader();
                var authHeader_string = authenticationHeader.ToString();
                return authHeader_string;
            }
        }

        //protected abstract IOauthUserInfo DefaultUserInfo()

        protected async UniTask<TOauthUserInfo> FetchUserInfo<TOauthUserInfo>(string userInfoUrl) where TOauthUserInfo : IOauthUserInfo
        {

            if (supportsUserInfo)
            {
                if (accessTokenResponse == null)
                {
                    Debug.Assert(accessTokenResponse == null, $"accessTokenResponse null {accessTokenResponse == null}");
                    var exception = 
                    new AccessTokenRequestException(new AccessTokenRequestError()
                    {
                        code = AccessTokenRequestErrorCode.InvalidGrant,
                        description = "Authentication required."
                    },  HttpStatusCode.BadRequest);

                    Debug.LogError(exception);
                    return default;
                }

                var authHeader_string = authenticationToken;

                using var request = UnityWebRequest.Get(userInfoUrl);
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", authHeader_string);

#if UNITY_EDITOR
                //Debug.Log($"FetchUserInfo :: {request}\n\n" +
                    //$"{authHeader_string}");
#endif
                var operation = await request.SendWebRequest();

                var responseJson = request.downloadHandler.text;
#if UNITY_EDITOR
                //Debug.Log($"GetAccessTokenInternalAsync :: {responseJson}"); 
#endif

                try
                {
                    if (request.result == UnityWebRequest.Result.Success && request.responseCode >= 200 && request.responseCode < 300)
                    {
                        var _userInfoResponse = JsonConvert.DeserializeObject<TOauthUserInfo>(responseJson);
                        return _userInfoResponse;
                    }
                }
                catch (Exception _e)
                {
                    throw _e;
                }
                return default;
                //return await ((IOauthUserCompatible)this).UTask_FetchUserInfo<TOauthUserInfo>(accessTokenResponse);
            }
            return default;
        }

        public static string GenerateCodeVerifier()
        {
            const int length = 64;
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~";
            var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            var result = new StringBuilder(length);
            foreach (var b in bytes)
                result.Append(chars[b % chars.Length]);
            return result.ToString();
        }

        public static string GenerateCodeChallenge(string _codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.ASCII.GetBytes(_codeVerifier);
                var hash = sha256.ComputeHash(bytes);
                return Base64UrlEncode(hash);
            }
        }
        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// Determines the need for retrieval of a new authorization code.
        /// </summary>
        /// <returns>Indicates if a new authorization code needs to be retrieved.</returns>
        public bool ShouldRequestAuthorizationCode()
        {
            return accessTokenResponse == null || !accessTokenResponse.HasRefreshToken();
        }

        /// <summary>
        ///  Determines the need for retrieval of a new access token using the refresh token.
        /// </summary>
        /// <remarks>
        /// If <see cref="accessTokenResponse"/> does not exist, then get new authorization code first.
        /// </remarks>
        /// <returns>Indicates if a new access token needs to be retrieved.</returns>
        /// <seealso cref="ShouldRequestAuthorizationCode"/>
        public bool ShouldRefreshToken()
        {
            return accessTokenResponse.IsNullOrExpired();
        }

        /// <summary>,
        /// Gets an authorization code request URI with the specified <see cref="configuration"/>.
        /// </summary>
        /// <returns>The authorization code request URI.</returns>
        public string GetAuthorizationUrl()
        {
            // Generate new state.
            state = Guid.NewGuid().ToString("D");

            var parameters = GetAuthorizationUrlParameters();
            if (string.IsNullOrEmpty(authorizationUrl)) return authorizationUrl;
            return UrlBuilder.New(authorizationUrl).SetQueryParameters(parameters).ToString();
        }

        protected virtual Dictionary<string, string> GetAuthorizationUrlParameters()
        {
            //string clientId = configuration.clientId;
            //string redirectUrl = configuration.redirectUri;
            //string scope = configuration.scope;
            string _redirect_url = configuration.useVirtualRedirectUrl ? configuration.virtualRedirectUri : configuration.redirectUri;
            string _redirectScaped = Uri.EscapeDataString(configuration.redirectUri);
            var req = new AuthorizationCodeRequest()
            {
                clientId = configuration.clientId,
                redirectUri = _redirect_url,
                scope = configuration.scope,
                state = state,
                codeChallenge = GenerateCodeChallenge(codeVerifier),
            };
            string reqStr = JsonConvert.SerializeObject(req);
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(reqStr);
            return dict;

            //return JsonHelper.ToDictionary(new AuthorizationCodeRequest()
            //{
            //    clientId = configuration.clientId,
            //    redirectUri = configuration.useVirtualRedirectUrl? configuration.virtualRedirectUri : configuration.redirectUri,
            //    scope = configuration.scope,
            //    state = state
            //});
        }

        public virtual async UniTask<AccessTokenResponse> UTask_ExchangeCodeForAccessToken(string redirectUrl)
        {
            var authorizationResponseUri = new Uri(redirectUrl);
            var query = HttpUtility.ParseQueryString(authorizationResponseUri.Query);

            // Is there any error?
            if (JsonHelper.TryGetFromNameValueCollection<AuthorizationCodeRequestError>(query, out var authorizationError))
                throw new AuthorizationCodeRequestException(authorizationError);

            if (!JsonHelper.TryGetFromNameValueCollection<AuthorizationCodeResponse>(query, out var authorizationResponse))
                throw new Exception("Authorization code could not get.");

            // Validate authorization response state.
            if (!string.IsNullOrEmpty(state) && state != authorizationResponse.state)
                throw new SecurityException($"Invalid state got: {authorizationResponse.state}");

            var parameters = GetAccessTokenParameters(authorizationResponse.code);

            Debug.Assert(parameters != null);
#if UNITY_EDITOR
            Debug.Log($"ExchangeCodeForAccessTokenAsync :: " +
                $"\n paramenters{JsonConvert.SerializeObject(parameters, Formatting.Indented)}" +
                $"\n authorizationResponse.code: " +
                $"{authorizationResponse.code}");
#endif
            accessTokenResponse = await Utask_GetAccessTokenInternal((parameters));

            return accessTokenResponse;

        }

        /// <summary>
        /// Asynchronously exchanges code with a token.
        /// </summary>
        /// <param name="redirectUrl">
        /// <see cref="Cdm.Authentication.Browser.BrowserResult.redirectUrl">Redirect URL</see> which is retrieved
        /// from the browser result.
        /// </param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Access token response which contains the access token.</returns>
        /// <exception cref="AuthorizationCodeRequestException"></exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="SecurityException"></exception>
        /// <exception cref="AccessTokenRequestException"></exception>
        public virtual async Task<AccessTokenResponse> ExchangeCodeForAccessTokenAsync(string redirectUrl,
            CancellationToken cancellationToken = default)
        {
            Uri authorizationResponseUri = new Uri(redirectUrl);
            NameValueCollection query = HttpUtility.ParseQueryString(authorizationResponseUri.Query);

            // Is there any error?
            if (JsonHelper.TryGetFromNameValueCollection<AuthorizationCodeRequestError>(query, out var authorizationError))
                throw new AuthorizationCodeRequestException(authorizationError);

            if (!JsonHelper.TryGetFromNameValueCollection<AuthorizationCodeResponse>(query, out var authorizationResponse))
                throw new Exception("Authorization code could not get.");

            // Validate authorization response state.
            if (!string.IsNullOrEmpty(state) && state != authorizationResponse.state)
                throw new SecurityException($"Invalid state got: {authorizationResponse.state}");

            var parameters = GetAccessTokenParameters(authorizationResponse.code);

            Debug.Assert(parameters != null);
#if UNITY_EDITOR
            Debug.Log($"ExchangeCodeForAccessTokenAsync :: " +
                $"\n paramenters{JsonConvert.SerializeObject(parameters, Formatting.Indented)}" +
                $"\n authorizationResponse.code: " +
                $"{authorizationResponse.code}");
#endif
            accessTokenResponse =
                await GetAccessTokenInternalAsync(new FormUrlEncodedContent(parameters), cancellationToken);

            return accessTokenResponse;
        }


        protected virtual Dictionary<string, string> GetAccessTokenParameters(string code)
        {
            string _redirect_url = configuration.useVirtualRedirectUrl ? configuration.virtualRedirectUri : configuration.redirectUri;
            string _redirectScaped = Uri.EscapeDataString(configuration.redirectUri);
            var req = new AccessTokenRequest()
            {
                code = code,
                clientId = configuration.clientId,
                clientSecret = configuration.clientSecret,
                redirectUri = _redirect_url,
                codeVerifier = codeVerifier,
            };

            string reqStr = JsonConvert.SerializeObject(req);
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(reqStr);
            //if (dict.ContainsKey("client_secret") && string.IsNullOrEmpty(dict["client_secret"]))
            //    dict.Remove("client_secret");

            return dict;
        }

        /// <summary>
        /// Gets the access token immediately from cache if <see cref="ShouldRefreshToken"/> is <c>false</c>;
        /// or refreshes and returns it using the refresh token.
        /// if available. 
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <exception cref="AccessTokenRequestException">If access token cannot be granted.</exception>
        public async Task<AccessTokenResponse> GetOrRefreshTokenAsync(
            CancellationToken cancellationToken = default)
        {
            if (ShouldRefreshToken())
            {
                return await RefreshTokenAsync(cancellationToken);
            }

            // Return from the cache immediately.
            return accessTokenResponse;
        }

        /// <summary>
        /// Asynchronously refreshes an access token using the refresh token from the <see cref="accessTokenResponse"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Token response which contains the access token and the refresh token.</returns>
        public async Task<AccessTokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            if (accessTokenResponse == null)
                throw new AccessTokenRequestException(new AccessTokenRequestError()
                {
                    code = AccessTokenRequestErrorCode.InvalidGrant,
                    description = "Authentication required."
                }, null);

            return await RefreshTokenAsync(accessTokenResponse.refreshToken, cancellationToken);
        }

        /// <summary>
        /// Asynchronously refreshes an access token using a refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token which is used to get a new access token.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>Token response which contains the access token and the input refresh token.</returns>
        public async Task<AccessTokenResponse> RefreshTokenAsync(string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                var error = new AccessTokenRequestError()
                {
                    code = AccessTokenRequestErrorCode.InvalidGrant,
                    description = "Refresh token does not exist."
                };

                throw new AccessTokenRequestException(error, null);
            }

            var parameters = JsonHelper.ToDictionary(new RefreshTokenRequest()
            {
                refreshToken = refreshToken,
                scope = configuration.scope
            });

            Debug.Assert(parameters != null);

            var tokenResponse =
                await GetAccessTokenInternalAsync(new FormUrlEncodedContent(parameters), cancellationToken);
            if (!tokenResponse.HasRefreshToken())
            {
                tokenResponse.refreshToken = refreshToken;
            }

            accessTokenResponse = tokenResponse;
            return accessTokenResponse;
        }

        async UniTask<AccessTokenResponse> Utask_GetAccessTokenInternal(Dictionary<string, string> content)
        {
            Debug.Assert(content != null);

            var authString = $"{configuration.clientId}:{configuration.clientSecret}";
            var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            var form = new WWWForm();
            foreach (var field in content)
            {
                form.AddField(field.Key, field.Value);
            }

            using var request = UnityWebRequest.Post(accessTokenUrl, form);
            if (!string.IsNullOrEmpty(configuration.clientSecret))
                request.SetRequestHeader("Authorization", $"Basic {base64AuthString}");

            request.SetRequestHeader("Accept", "application/json");

#if UNITY_EDITOR
            Debug.Log($"content:{JsonConvert.SerializeObject(content, Formatting.Indented)}");
#endif

            var operation = await request.SendWebRequest();

            var responseJson = request.downloadHandler.text;
            Debug.Log($"GetAccessTokenInternalAsync :: {responseJson}");

            if (request.result == UnityWebRequest.Result.Success && request.responseCode >= 200 && request.responseCode < 300)
            {
                var tokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);
                tokenResponse.issuedAt = DateTime.UtcNow;
                return tokenResponse;
            }

            AccessTokenRequestError error = null;
            try
            {
                error = JsonConvert.DeserializeObject<AccessTokenRequestError>(responseJson);
            }
            catch (Exception _e)
            {
                throw _e;
            }

            throw new AccessTokenRequestException(error, (HttpStatusCode)request.responseCode);
        }

        private async Task<AccessTokenResponse> GetAccessTokenInternalAsync(FormUrlEncodedContent content,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(content != null);

            var authString = $"{configuration.clientId}:{configuration.clientSecret}";
            var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, accessTokenUrl);
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);
            tokenRequest.Content = content;
            Debug.Log(JsonConvert.SerializeObject(content, Formatting.Indented));
#if UNITY_EDITOR
            //Debug.Log($"url:{tokenRequest.RequestUri.ToString()}\n" +
            //    $"headers:\n{JsonConvert.SerializeObject(tokenRequest.Headers,Formatting.Indented)}\n" +
            //    $"body:{JsonConvert.SerializeObject(tokenRequest.Content, Formatting.Indented)}");
            Debug.Log($"content: {await tokenRequest.Content.ReadAsStringAsync()}");
#endif

            var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync();

            Debug.Log($"GetAccessTokenInternalAsync :: {responseJson}");
            if (response.IsSuccessStatusCode)
            {
                //var responseJson = await response.Content.ReadAsStringAsync();

#if UNITY_EDITOR
                Debug.Log(responseJson);
#endif
                var tokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseJson);
                tokenResponse.issuedAt = DateTime.UtcNow;
                return tokenResponse;
            }

            AccessTokenRequestError error = null;
            try
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                error = JsonConvert.DeserializeObject<AccessTokenRequestError>(errorJson);
            }
            catch (Exception)
            {
                // ignored
            }

            throw new AccessTokenRequestException(error, response.StatusCode);
        }



        public void Dispose()
        {
            httpClient?.Dispose();
        }

        internal async void SetAuthenticationToken(string value)
        {
            accessTokenResponse = string.IsNullOrEmpty(value)? null : new()
            {
                accessToken = value,
                tokenType = "Bearer"
            };
        }
    }

    /// <summary>
    /// The configuration of third-party authentication service client.
    /// </summary>
    [DataContract]
    public struct OauthAppConfiguration
    {
        /// <summary>
        /// User on certain types of OauthBrowser (platforms) to determine if a virtual redirect URL is used. to HttpListener
        /// </summary>
        [JsonIgnore]
        public bool useVirtualRedirectUrl
        {
            get; set;
        }

        /// <summary>
        /// The client identifier issued to the client during the registration process described by
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#section-2.2">Section 2.2</a>.
        /// </summary>
        [DataMember(Name = "client_id", IsRequired = true)]
        public string clientId
        {
            get; set;
        }

        /// <summary>
        /// The client secret. The client MAY omit the parameter if the client secret is an empty string.
        /// </summary>
        [DataMember(Name = "client_secret")]
        public string clientSecret
        {
            get; set;
        }

        /// <summary>
        /// The authorization and token endpoints allow the client to specify the scope of the access request using
        /// the "scope" request parameter.  In turn, the authorization server uses the "scope" response parameter to
        /// inform the client of the scope of the access token issued. The value of the scope parameter is expressed
        /// as a list of space- delimited, case-sensitive strings.  The strings are defined by the authorization server.
        /// If the value contains multiple space-delimited strings, their order does not matter, and each string adds an
        /// additional access range to the requested scope.
        /// </summary>
        [DataMember(Name = "scope")]
        public string scope
        {
            get; set;
        }

        /// <summary>
        /// After completing its interaction with the resource owner, the authorization server directs the resource
        /// owner's user-agent back to the client. The authorization server redirects the user-agent to the client's
        /// redirection endpoint previously established with the authorization server during the client registration
        /// process or when making the authorization request.
        /// </summary>
        /// <remarks>
        /// The redirection endpoint URI MUST be an absolute URI as defined by
        /// <a href="https://www.rfc-editor.org/rfc/rfc3986#section-4.3">[RFC3986] Section 4.3</a>.
        /// The endpoint URI MAY include an "application/x-www-form-urlencoded" formatted (per
        /// <a href="https://www.rfc-editor.org/rfc/rfc6749#appendix-B">Appendix B</a>) query
        /// component (<a href="https://www.rfc-editor.org/rfc/rfc3986#section-3.4">[RFC3986] Section 3.4</a>),
        /// which MUST be retained when adding additional query parameters. The endpoint URI MUST NOT include
        /// a fragment component.
        /// </remarks>
        [DataMember(Name = "redirect_uri")]
        public string redirectUri
        {
            get; set;
        }

        [JsonIgnore]

        public string virtualRedirectUri
        {
            get; set;
        }


    }
}