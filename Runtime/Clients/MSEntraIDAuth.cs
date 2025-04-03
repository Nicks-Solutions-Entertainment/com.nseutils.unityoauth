using Cdm.Authentication.OAuth2;
using Cdm.Authentication.Utils;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Cdm.Authentication.Clients
{
    public class MSEntraIDAuth : AuthorizationCodeFlow, IUserInfoProvider
    {
        private readonly string tenant;

        public MSEntraIDAuth(Configuration configuration, string tenant) : base(configuration)
        {
            this.tenant = tenant;
        }

        public override string authorizationUrl => string.IsNullOrEmpty(tenant) ? "" :
            $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize";


        public override string accessTokenUrl => string.IsNullOrEmpty(tenant) ? "" :
            $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
        public override string userInfoUrl => "https://graph.microsoft.com/v1.0/me";

        public async Task<IUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            if (accessTokenResponse == null)
                throw new AccessTokenRequestException(new AccessTokenRequestError()
                {
                    code = AccessTokenRequestErrorCode.InvalidGrant,
                    description = "Authentication required."
                }, null);

            var authenticationHeader = accessTokenResponse.GetAuthenticationHeader();
            return await UserInfoParser.GetUserInfoAsync<MSEntraIDInfo>(
                httpClient, userInfoUrl, authenticationHeader, cancellationToken);
        }
    }

    [DataContract]
    public class MSEntraIDInfo : IUserInfo
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
