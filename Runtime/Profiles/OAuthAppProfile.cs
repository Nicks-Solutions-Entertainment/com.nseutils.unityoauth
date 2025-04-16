using nseutils.unityoauth;
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
    [Header("Used just on Serverless or full back-end \n DO NOT SAVE THIS ON PROJECT BY SCRIPTABLE, insert it from outside")]
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
    [SerializeField] OauthAppInfos m_oauthAppInfos;

    public OauthAppInfos oauthAppInfos => m_oauthAppInfos;

    public OAuthAppProfile (OauthAppInfos oauthAppInfos)
    {
        m_oauthAppInfos = oauthAppInfos;
    }

    public static OAuthAppProfile New(OauthAppInfos oauthAppInfos)
    {
        return Instantiate(new OAuthAppProfile(oauthAppInfos));
    }
}
