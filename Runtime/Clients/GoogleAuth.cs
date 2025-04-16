using System.Runtime.Serialization;
using nseutils.unityoauth;
using Cysharp.Threading.Tasks;

namespace nseutils.unityoauth.Clients
{
    public class GoogleAuth : AuthorizationCodeFlow, IOauthUserInfoCompatible
    {
        public GoogleAuth(OauthAppConfiguration configuration) : base(configuration)
        {
        }

        public override string authorizationUrl => "https://accounts.google.com/o/oauth2/auth";
        public override string accessTokenUrl => "https://accounts.google.com/o/oauth2/token";
        public string userInfoUrl => "https://www.googleapis.com/oauth2/v1/userinfo";


        public async UniTask<IOauthUserInfo> GetUserInfos()
        {
            return await FetchUserInfo<MSEntraIDInfo>(userInfoUrl);
        }
    }
    
    [DataContract]
    public class GoogleUserInfo : IOauthUserInfo
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string id { get; set; }

        [DataMember(Name = "name")] 
        public string name { get; set; }

        [DataMember(Name = "given_name")] 
        public string givenName { get; set; }

        [DataMember(Name = "family_name")] 
        public string familyName { get; set; }

        [DataMember(Name = "email")] 
        public string email { get; set; }

        [DataMember(Name = "picture")] 
        public string picture { get; set; }
    }
}