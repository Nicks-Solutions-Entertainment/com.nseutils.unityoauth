mergeInto(LibraryManager.library, 
{
    _oAuth: null,

    

    OAuthInit: function()
    {
        // instantiate a new oAuth service; unless it already has.
        this._oAuth = this._oAuth || new function() {
            this.oAuthClientVersion = "2.2.0";
            this._client = null;
            this.redirectUri = null;
            this.scope = null;
            this._state = null;
            this._codeVerifier = null;

            this.windowObjectReference = null;
            this.previousUrl = null;

            this.init = () => {
                this._state = this._generateRandomString();

                // Trick of the day: dynamically load an external library so that the user of this package does not need
                // to explicitly declare it.
                const script = document.createElement("script");
                script.src = `https://cdn.jsdelivr.net/npm/@badgateway/oauth2-client@${this.oAuthClientVersion}/browser/oauth2-client.min.js`;
                document.head.appendChild(script);

                window.popupCompleted = () => {
                    try {
                        this._signInSucceeded(this.windowObjectReference.location);
                        console.log('js::_signInSucceeded', codeRedirect);
                    } catch (e) {
                        console.error("SignInFailed: ", e);
                        unityInstance.SendMessage("WebGlOauthBridge", "SignInFailed");
                    }
                }
            };

            this.setupClient = (authorizationEndpoint, clientId, clientSecret, tokenEndpoint, redirectUri, scope) => {
                const {host, protocol} = new URL(authorizationEndpoint);

                let clientConfig = {
                    server: `${protocol}//${host}`,
                    clientId,
                    tokenEndpoint,
                    authorizationEndpoint
                };
                
                // Some clients want the secret, others don't. So: let's give it when we have it
                if (clientSecret) {
                    clientConfig.clientSecret = clientSecret;
                }
                
                this._client = new OAuth2Client.OAuth2Client(clientConfig);
                this.redirectUri = redirectUri;
                this.scope = scope;

                // Don't ask why specifically, but apparently the binding of fetch causes an issue in this library;
                // I found https://stackoverflow.com/a/72800505 where someone had the same issue and I now explicitly 
                // bind it to window. See also: https://github.com/foxglove/just-fetch/pull/6 for more details.
                //
                // The error that occurs when you do not do this is: 
                //     TypeError: Failed to execute 'fetch' on 'Window': Illegal invocation 
                this._client.settings.fetch = window.fetch.bind(window);
            };

            this.signIn = async () => {
                this._codeVerifier = await OAuth2Client.generateCodeVerifier();
                const authorizeUri = await this._client.authorizationCode.getAuthorizeUri({
                    redirectUri: this.redirectUri,
                    state: this._state,
                    codeVerifier: this._codeVerifier,
                    scope: this.scope
                });
                
                var interval = setInterval(function() {
                    try {
                        console.log('js::popup uri:', this.redirectUri);
                        if (this.windowObjectReference.location.href.indexOf(this.redirectUri) !== -1) {
                            clearInterval(interval);
                            var params = new URLSearchParams(popup.location.search);
                            var authCode = params.get('code');
                            var error = params.get('error');
                            this.windowObjectReference.close();
                            console.log('js::popup redirectUri detected', authCode, error);
                            if (authCode) {
                                SendMessage('WebGlOauthBridge', 'SignedIn', authCode);
                            } else if (error) {
                                SendMessage('WebGlOauthBridge', 'SignInFailed', error);
                            }
                        }
                    } catch (e) {
                        // Ignore cross-origin errors
                    }
                }, 1000);

                this._openSignInWindow(authorizeUri, 'myOAuth');
            };

            this._generateRandomString = () => {
                return Math.floor(Math.random() * Date.now()).toString(36);
            };

            this._openSignInWindow = (url, name) => {
                const strWindowFeatures = 'toolbar=no, menubar=no, width=600, height=700, top=100, left=100';

                if (this.windowObjectReference === null || this.windowObjectReference.closed) {
                   this.windowObjectReference = window.open(url, name, strWindowFeatures);
                     //this.windowObjectReference = window.open(url, '_blank');
                } else if (this.previousUrl !== url) {
                   this.windowObjectReference = window.open(url, name, strWindowFeatures);
                     //this.windowObjectReference = window.open(url, '_blank');
                    this.windowObjectReference.focus();
                } else {
                    this.windowObjectReference.focus();
                }
                console.log('js::_openSignInWindow');
                this.previousUrl = url;
            };

            this._signInSucceeded = async (codeRedirect) =>
            {
                let token = await this._client.authorizationCode.getTokenFromCodeRedirect(
                    codeRedirect.toString(),
                    {
                        redirectUri: this.redirectUri,
                        state: this._state,
                        codeVerifier: this._codeVerifier,
                    }
                )

                unityInstance.SendMessage(
                    "WebGlOauthBridge", 
                    "SignedIn", 
                    JSON.stringify(
                        // Match format to what Cdm.Authentication.OAuth2.AccessTokenResponse expects
                        {
                            access_token: token.accessToken,
                            expires_in: null,
                            expires: token.expiresAt,
                            token_type: '',
                            scopes: this.scope.join(' '),
                            refresh_token: token.refreshToken
                        }
                    )
                );
            };
        };

        this._oAuth.init();
    },
    StartSignin: function(signinUrl){
        //open popup window with signinUrl
        
    },

    OAuthSignIn: async function (authorizationEndpoint, tokenEndpoint, clientId, clientSecret, redirectUri, scopes) {
        authorizationEndpoint = UTF8ToString(authorizationEndpoint);
        redirectUri = UTF8ToString(redirectUri);
        clientId = UTF8ToString(clientId);
        clientSecret = UTF8ToString(clientSecret);
        tokenEndpoint = UTF8ToString(tokenEndpoint);
        const scope = UTF8ToString(scopes).split(' ');

        this._oAuth.setupClient(authorizationEndpoint, clientId, clientSecret, tokenEndpoint, redirectUri, scope);
        this._oAuth.signIn();
        console.log($"OAuthSignIn :: redirectUri = ",this.redirectUri);
    },
    
    //OAuthSignOut: function() {
        // At the moment, we do not store tokens; so nothing needs to be done
    //},
});