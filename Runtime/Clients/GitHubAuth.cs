using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using nseutils.unityoauth;
using nseutils.unityoauth.Utils;
using Cysharp.Threading.Tasks;

namespace nseutils.unityoauth.Clients
{
    public class GitHubAuth : AuthorizationCodeFlow,IOauthUserInfoCompatible
    {
        public GitHubAuth(OauthAppConfiguration configuration) : base(configuration)
        {
        }

        public override string authorizationUrl => "https://github.com/login/oauth/authorize";
        public override string accessTokenUrl => "https://github.com/login/oauth/access_token";
        public string userInfoUrl => "https://www.googleapis.com/oauth2/v1/userinfo";

        public async UniTask<IOauthUserInfo> GetUserInfos()
        {
            return await FetchUserInfo<MSEntraIDInfo>(userInfoUrl);
        }
    }
    
    [DataContract]
    public class GitHubUserInfo : IOauthUserInfo
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string id { get; set; }

        [DataMember(Name = "name")] 
        public string name { get; set; }

        [DataMember(Name = "email")] 
        public string email { get; set; }

        [DataMember(Name = "avatar_url")] 
        public string avatarUrl { get; set; }

        public string picture => avatarUrl;
    }
}