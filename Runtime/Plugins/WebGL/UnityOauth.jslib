mergeInto(LibraryManager.library, 
{
    _oAuth: null,
    windowObjectReference: null,
    previousUrl: null,

    
    PrepareOauth: async function(){
        
        this.windowObjectReference = null;

        var script = document.createElement('script');
        script.type = 'text/javascript';
        script.text = `
        
            window.addEventListener('message', (event) => {
                console.log('Popup URL:', event.data);

                unityInstance.SendMessage(
                "WebGlOauthListener", 
                "SignedIn", 
                    event.data
                );

            });
        `;
        document.head.appendChild(script);
    },

    StartSignin : function(url){
        const strWindowFeatures = 'toolbar=no, menubar=no, width=600, height=700, top=100, left=100';
        
        url = UTF8ToString(url);
        console.log('js:StartSignin', url);
        if (this.windowObjectReference == null || 
        this.windowObjectReference != null && this.windowObjectReference.closed) {
            this.windowObjectReference = window.open(url, name, strWindowFeatures);
            //this.RegisterOpenWindowCallback();
        } else if (this.previousUrl !== url) {
            this.windowObjectReference = window.open(url, name, strWindowFeatures);
            //this.RegisterOpenWindowCallback();
            this.windowObjectReference.focus();
        } else {
            this.windowObjectReference.focus();
        }

        this.previousUrl = url;
    },

    oAuthSignIn: async function (authorizationEndpoint, tokenEndpoint, clientId, clientSecret, redirectUri, scopes) {
        authorizationEndpoint = UTF8ToString(authorizationEndpoint);
        redirectUri = UTF8ToString(redirectUri);
        clientId = UTF8ToString(clientId);
        clientSecret = UTF8ToString(clientSecret);
        tokenEndpoint = UTF8ToString(tokenEndpoint);
        const scope = UTF8ToString(scopes).split(' ');

        this._oAuth.setupClient(authorizationEndpoint, clientId, clientSecret, tokenEndpoint, redirectUri, scope);
        this._oAuth.signIn();
    },

    RegisterOpenWindowCallback: function(){
        window.addEventListener('message', (event) => {
            console.log('Popup URL:', event.data);

            unityInstance.SendMessage(
            "WebGlOauthListener", 
            "SignedIn", 
                event.data
            );

        });

        console.log('BANANA');
    },
    
    oAuthSignOut: function() {
        // At the moment, we do not store tokens; so nothing needs to be done
    },
});
