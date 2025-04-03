using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class StdOauthConection : OauthConnection
{

    public StdOauthConection(OauthAppInfos oauthProfileInfos) : base(oauthProfileInfos)
    {
        
    }

    public override IEnumerator Authenticate()
    {
        accessTokenResponse = null;

        var task = session.AuthenticateAsync();
        yield return new WaitUntil(() => task.IsCompleted || task.IsCanceled);

        if (task.Status != TaskStatus.RanToCompletion)
        {
            SignedInFailed();
            yield break;
        }
        accessTokenResponse = task.Result;
        Debug.Log($"SignedInSuccess...");
        SignedInSuccess(accessTokenResponse); 
    }


    public override IEnumerator FetchUserInfo()
    {
        if (!session.SupportsUserInfo())
        {
            Debug.LogWarning("User info is not supported by this provider");
            yield break;
        }

        var task = session.GetUserInfoAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        UserInfoReceived(task.Result);
    }

    public override void SignWebRequest(UnityWebRequest webRequest)
    {
        if (accessTokenResponse == null)
            return;

        var token = Encoding.UTF8.GetBytes(accessTokenResponse.accessToken);
        webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
    }
    public override IEnumerator Refresh()
    {
        // Refreshing is part of the CDM package and does not need to be done manually
        yield break;
    }

    public override IEnumerator SignOut()
    {
        SignedOut();
        yield return null;
    }

}
