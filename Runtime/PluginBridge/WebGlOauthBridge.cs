
using UnityEngine;

public class WebGlOauthBridge : MonoBehaviour
{
    internal static WebGlOauthBridge Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new GameObject("WebGlOauthBridge").AddComponent<WebGlOauthBridge>();
            }
            return m_instance;
        }
    }
    static WebGlOauthBridge m_instance;

    public delegate void OnSignedInDelegate(string token);
    public delegate void OnSignInFailedDelegate();

    public OnSignedInDelegate OnSignedIn;
    public OnSignInFailedDelegate OnSignInFailed;

    private void OnDestroy()
    {
        m_instance = null;

        OnSignedIn = null;
        OnSignInFailed = null;
    }
    public void SignedIn(string token)
    {
        OnSignedIn?.Invoke(token);

        Debug.Log($"WebGlOauthBridge :: SignedIn \n token:{token}");

    }

    public void SignInFailed()
    {
        OnSignInFailed?.Invoke();
        OnSignedIn = null;
        OnSignInFailed = null;

    }
}
