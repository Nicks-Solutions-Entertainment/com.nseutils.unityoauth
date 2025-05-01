
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace nseutils.unityoauth
{

    public class StdOauthConection : OauthConnection
    {

        public StdOauthConection(OauthAppInfos oauthProfileInfos) : base(oauthProfileInfos)
        {

        }

        public override IEnumerator Authenticate()
        {
            accessTokenResponse = null;

            var _tokenResponse = session.AuthenticateAsync();
            yield return new WaitUntil(() => _tokenResponse.IsCompleted || _tokenResponse.IsCanceled);

            if (_tokenResponse.Status != TaskStatus.RanToCompletion)
            {
                SignedInFailed();
                yield break;
            }
            accessTokenResponse = _tokenResponse.Result;
            Debug.Log($"SignedInSuccess...");
            SignedInSuccess();
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

}