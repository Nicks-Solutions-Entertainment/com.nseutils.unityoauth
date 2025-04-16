using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using nseutils.unityoauth;
using nseutils.unityoauth.Utils;
using Cysharp.Threading.Tasks;

namespace nseutils.unityoauth.Clients
{
    public class FacebookAuth : AuthorizationCodeFlow,IOauthUserInfoCompatible
    {
        public override string authorizationUrl => "https://www.facebook.com/dialog/oauth";
        public override string accessTokenUrl => "https://graph.facebook.com/oauth/access_token";
        public string userInfoUrl => "https://graph.facebook.com/me";

        public FacebookAuth(OauthAppConfiguration configuration) : base(configuration)
        {
        }

        public async UniTask<IOauthUserInfo> GetUserInfos()
        {
            return await FetchUserInfo<MSEntraIDInfo>(userInfoUrl);
        }
    }
    
    [DataContract]
    public class FacebookUserInfo : IOauthUserInfo
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string id { get; set; }
        
        [DataMember(Name = "first_name")] 
        public string firstName { get; set; }

        [DataMember(Name = "last_name")] 
        public string lastName { get; set; }

        [DataMember(Name = "email")] 
        public string email { get; set; }

        [DataMember(Name = "picture")]
        public PictureData pictureData { get; set; }
        
        public string name => $"{firstName} {lastName}";
        public string picture => pictureData?.url;
        
        [DataContract]
        public class PictureData
        {
            [DataMember(Name = "url", IsRequired = true)] 
            public string url { get; set; }
        }
    }
}