
using UnityEngine;

public class WebGlOauthListener : MonoBehaviour
{
    internal static WebGlOauthListener Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new GameObject(nameof(WebGlOauthListener)).AddComponent<WebGlOauthListener>();
            }
            return m_instance;
        }
    }
    static WebGlOauthListener m_instance;

    public delegate void OnSignedInDelegate(string redirectUrl);
    public delegate void OnSignInFailedDelegate();

    public OnSignedInDelegate OnSignedIn;
    public OnSignInFailedDelegate OnSignInFailed;


    internal bool finished
    {
        get; private set;
    }

    private void OnDestroy()
    {
        m_instance = null;

        OnSignedIn = null;
        OnSignInFailed = null;
    }
    public void SignedIn(string redirectUrl)
    {
        OnSignedIn?.Invoke(redirectUrl);
        Debug.Log($"WebGlOauthListener :: SignedIn \n token:{redirectUrl}");
        FinishListen();

    }

    internal void FinishListen() => finished = true;

    public void SignInFailed()
    {
        OnSignInFailed?.Invoke();
        OnSignedIn = null;
        OnSignInFailed = null;

        finished = true;
    }
}
