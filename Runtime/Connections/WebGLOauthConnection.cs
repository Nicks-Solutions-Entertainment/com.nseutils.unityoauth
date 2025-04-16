
using nseutils.unityoauth;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class WebGLOauthConnection : OauthConnection
{

#if !UNITY_EDITOR && UNITY_WEBGL
    //    [DllImport("__Internal")]
    //private static extern void oAuthInit();

    //[DllImport("__Internal")]
    //private static extern void oAuthSignIn(
    //    string authorizationEndpoint,
    //    string tokenEndpoint,
    //    string clientId,
    //    string clientSecret,
    //    string redirectUri,
    //    string scopes
    //);
#else




#endif

    private void oAuthInit()
    {

    }
    private void oAuthSignIn(
        string authorizationEndpoint,
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string redirectUri,
        string scopes
    )
    {

    }

    //private static extern void OAuthSignOut();
    public WebGLOauthConnection(OauthAppInfos oauthProfileInfos) : base(oauthProfileInfos)
    {
        oAuthInit();
        Debug.Log("WebGLOauthConnection created");
    }
    
    public override IEnumerator Authenticate()
    {
        accessTokenResponse = null;

        var interceptor = WebGlOauthListener.Instance;
        interceptor.OnSignedIn += (token) =>
        {
            WebGLAccessTokenResponse response = JsonUtility.FromJson<WebGLAccessTokenResponse>(token);
            accessTokenResponse = new AccessTokenResponse()
            {
                accessToken = response.access_token,
                expiresIn = response.expires_in,
                tokenType = response.token_type,
                scope = String.Join(' ', response.scopes),
            };
            SignedInSuccess(accessTokenResponse);
        };

        interceptor.OnSignInFailed += () =>
        {
            SignedInFailed();
        };

        oAuthSignIn(
            session.codeflow.authorizationUrl,
            session.codeflow.accessTokenUrl,
            session.codeflow.configuration.clientId,
            session.codeflow.configuration.clientSecret,
            session.codeflow.configuration.redirectUri,
            session.codeflow.configuration.scope
        );

        yield return null;
    }

    public override void SignWebRequest(UnityWebRequest webRequest)
    {
        if (accessTokenResponse == null)
        {
            return;
        }

        var token = accessTokenResponse.accessToken;
        webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
    }
    //public override IEnumerator FetchUserInfo()
    //{

    //    if (session.SupportsUserInfo())
    //    {
    //        yield return null;
    //    }

    //    var task = session.GetUserInfoAsync();
    //    yield return new WaitUntil(() => task.IsCompleted);

    //    UserInfoReceived(task.Result);


    //    //using var webRequest = UnityWebRequest.Get(session.client.configuration.us);
    //    //webRequest.SetRequestHeader("Accept", "application/json");
    //    //webRequest.downloadHandler = new DownloadHandlerBuffer();

    //    //SignWebRequest(webRequest);

    //    //yield return webRequest.SendWebRequest();

    //    //if (webRequest.result != UnityWebRequest.Result.Success)
    //    //{
    //    //    Debug.LogWarning("Unable to retrieve user information: " + webRequest.error);
    //    //    yield break;
    //    //}

    //    //OnUserInfoReceived?.Invoke(DeserializeUserInfo(webRequest.downloadHandler.text));

    //    yield return null;
    //}

    public override IEnumerator Refresh()
    {
        // The JSO javascript library self-refreshes if you call authenticate again; so we do.
        yield return Authenticate();
    }

    public override IEnumerator SignOut()
    {
        //OAuthSignOut();
        SignedOut();

        yield return null;
    }

}


[Serializable]
public class WebGLAccessTokenResponse
{
    public string access_token;
    public long? expires;
    public long? expires_in;
    public long? received;
    public string id_token;
    public string[] scopes;
    public string token_type;
}
