using Cdm.Authentication;
using Cdm.Authentication.OAuth2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
[Serializable]
public enum OauthProvider
{
    MS_EntraID,
    Google,
    Apple,
    GitHub,
    Facebook
}

[Serializable]
struct OauthRedirectURIGroup
{
    [SerializeField] string m_StdredirectUri;
    [SerializeField] string m_deeplinkRedirectUri;
    public OauthRedirectURIGroup(string stdredirectUri, string deeplinkRedirectUri = "")
    {
        m_StdredirectUri = stdredirectUri;
        m_deeplinkRedirectUri = deeplinkRedirectUri;
    }
    public string stdredirectUri => m_StdredirectUri;
    public string deeplinkRedirectUri => !string.IsNullOrEmpty(m_deeplinkRedirectUri)? m_deeplinkRedirectUri : m_StdredirectUri;
}

[Serializable]
public class OauthAppInfos
{
    [SerializeField] OauthProvider m_identityProvider;
    [SerializeField]
    [Tooltip("The client id, as defined with the OAuth app for the associated provider")]
    string m_clientId;
    [SerializeField]
    [
        Tooltip(
            "The client secret, as defined with the OAuth app for the associated provider or left empty if your "
            + "provider doesn't want one"
        )
    ]
    string m_clientSecret;
    [SerializeField]
    [
        Tooltip(
            "The URL to the callback endpoint where to find the 'callback.html' file, as defined with the OAuth "
            + "app for the associated provider"
        )
    ]
    OauthRedirectURIGroup m_redirectUri;
    [SerializeField] string m_scope;

    [SerializeField] string m_entraIdTenant = "";

    //[SerializeField]
    public string entraIdTenant => m_entraIdTenant;
    public OauthProvider identityProvider => m_identityProvider;
    public string clientId => m_clientId;
    public string clientSecret => m_clientSecret;
    public string redirectUri => !Application.isMobilePlatform || Application.isEditor? m_redirectUri.stdredirectUri : m_redirectUri.deeplinkRedirectUri;
    public string scope => m_scope;


    //gerar construtor
    public OauthAppInfos(OauthProvider identityProvider, string clientId, string clientSecret, string redirectUri, string scope)
    {
        m_identityProvider = identityProvider;
        m_clientId = clientId;
        m_clientSecret = clientSecret;
        m_redirectUri = new(redirectUri);
        m_scope = scope;
    }
}

[Serializable]
public struct ProviderTenant
{
    public string tenantName;
    public string tenantId;

    public ProviderTenant(string tenantName, string tenantId = "")
    {
        this.tenantName = tenantName;
        this.tenantId = tenantId;
    }
}

[CreateAssetMenu(fileName = "New OAuth-App Profile", menuName = "ScriptableObjects/Authentication/OAuth-App Profile")]
public class OAuthAppProfile : ScriptableObject
{
    //private IConnection connection;
    [SerializeField] OauthAppInfos m_oauthAppInfos;

    public OauthAppInfos oauthAppInfos => m_oauthAppInfos;

    //[Header("Events")]
    //public UnityEvent OnSignedIn;
    //public UnityEvent OnSignInFailed;
    //public UnityEvent OnSignedOut;
    //public UnityEvent<IUserInfo> OnUserInfoReceived;
    //public AccessTokenResponse AccessToken
    //{
    //    get; private set;
    //}
    //public void Initialize()
    //{
    //    //var authorizationCodeFlow = CreateAuthorizationFlow();

    //    //connection = new CdmConnection(authorizationCodeFlow);
    //    //if (Application.platform == RuntimePlatform.WebGLPlayer)
    //    //{
    //    //    connection = new WebGLConnection(m_oauthAppInfos.identityProvider, authorizationCodeFlow);
    //    //}

    //    //connection.OnSignedIn += SignedIn;
    //    //connection.OnSignedOut += SignedOut;
    //    //connection.OnSignInFailed += SignInFailed;
    //    //connection.OnUserInfoReceived += UserInfoReceived;
    //}

    //public void Finialize()
    //{
    //    //connection.OnSignedIn -= SignedIn;
    //    //connection.OnSignedOut -= SignedOut;
    //    //connection.OnSignInFailed -= SignInFailed;
    //    //connection.OnUserInfoReceived -= UserInfoReceived;

    //    //connection = null;
    //}

    //private void OnValidate()
    //{
    //    //var neededSettings = Factory.GetRequiredProviderSpecificSettings(m_oauthAppInfos.identityProvider);

    //    //// Delete any key that is not needed for this identity provider
    //    //var unnecessarySettings = m_oauthAppInfos.providerTenants
    //    //    .Where(setting => neededSettings.Contains(setting.Key) == false)
    //    //    .ToList();
    //    //foreach (var setting in unnecessarySettings)
    //    //{
    //    //    m_oauthAppInfos.providerTenants.Remove(setting);
    //    //}

    //    //// Add any missing keys for this identity provider
    //    //var missingSettings = neededSettings
    //    //    .Where(setting => IdentityProviderSpecificSettings.Has(m_oauthAppInfos.providerTenants, setting) == false)
    //    //    .ToList();
    //    //foreach (var setting in missingSettings)
    //    //{
    //    //    m_oauthAppInfos.providerTenants.Add(new IdentityProviderSpecificSetting() { Key = setting, Value = "" });
    //    //}
    //}

    //private void SignedIn(AccessTokenResponse accessTokenResponse)
    //{
    //    AccessToken = accessTokenResponse;
    //    OnSignedIn?.Invoke();
    //}
    //private void SignedOut()
    //{
    //    OnSignedOut?.Invoke();
    //}
    //private void SignInFailed()
    //{
    //    OnSignInFailed?.Invoke();
    //}

    //private void UserInfoReceived(IUserInfo userinfo)
    //{
    //    OnUserInfoReceived?.Invoke(userinfo);
    //}


    //public IEnumerator SignIn()
    //{
    //    AccessToken = null;

    //    yield return connection.Authenticate();
    //}

    //public IEnumerator RefreshBeforeExpiry()
    //{
    //    if (AccessToken.HasRefreshToken() == false)
    //    {
    //        yield break;
    //    }

    //    if (AccessToken.expiresAt == null)
    //    {
    //        yield break;
    //    }

    //    int seconds = (int)Math.Min(0, Math.Floor((DateTime.Now - AccessToken.expiresAt).Value.TotalSeconds - 60));

    //    yield return new WaitForSecondsRealtime(seconds);

    //    yield return connection.Refresh();
    //}

    //public IEnumerator SignOut()
    //{
    //    AccessToken = null;

    //    yield return connection.SignOut();
    //}

    //public IEnumerator FetchUserInfo()
    //{
    //    yield return connection.FetchUserInfo();
    //}

    //private AuthorizationCodeFlow CreateAuthorizationFlow()
    //{
    //    return new Factory().Create(
    //        this.m_oauthAppInfos.identityProvider,
    //        new AuthorizationCodeFlow.Configuration()
    //        {
    //            clientId = this.m_oauthAppInfos.clientId,
    //            clientSecret = this.m_oauthAppInfos.clientSecret,
    //            redirectUri =this. m_oauthAppInfos.redirectUri,
    //            scope =this. m_oauthAppInfos.scope
    //        },
    //        this.m_oauthAppInfos.providerTenants
    //    );
    //}

    public OAuthAppProfile (OauthAppInfos oauthAppInfos)
    {
        m_oauthAppInfos = oauthAppInfos;
    }

    public static OAuthAppProfile New(OauthAppInfos oauthAppInfos)
    {
        return Instantiate(new OAuthAppProfile(oauthAppInfos));
    }
}
