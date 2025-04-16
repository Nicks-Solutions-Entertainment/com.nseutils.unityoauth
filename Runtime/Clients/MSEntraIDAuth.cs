using nseutils.unityoauth;
using Cysharp.Threading.Tasks;
using System.Runtime.Serialization;

namespace nseutils.unityoauth.Clients
{
    public class MSEntraIDAuth : AuthorizationCodeFlow, IOauthUserInfoCompatible
    {
        private readonly string tenant;

        public MSEntraIDAuth(OauthAppConfiguration configuration, string tenant) : base(configuration)
        {
            this.tenant = tenant;
        }

        public override string authorizationUrl => string.IsNullOrEmpty(tenant) ? "" :
            $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize";


        public override string accessTokenUrl => string.IsNullOrEmpty(tenant) ? "" :
            $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
        public string userInfoUrl => "https://graph.microsoft.com/v1.0/me";

        public async UniTask<IOauthUserInfo> GetUserInfos()
        {
            return await FetchUserInfo<MSEntraIDInfo>(userInfoUrl);
        }

        //internal override bool supportsUserInfo => true;

        //public async UniTask<MSEntraIDInfo> GetInfos()
        //{
        //    return await this.UTask_FetchUserInfo(accessTokenResponse);
        //}

    }

    [DataContract]
    public class MSEntraIDInfo : IOauthUserInfo
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string id
        {
            get; set;
        }

        [DataMember(Name = "displayName")]
        public string name
        {
            get; set;
        }

        [DataMember(Name = "mail")]
        public string email
        {
            get; set;
        }

        public string picture => null;
    }
}
