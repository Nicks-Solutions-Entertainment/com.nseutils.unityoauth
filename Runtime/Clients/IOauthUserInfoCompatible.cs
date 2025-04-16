using Cysharp.Threading.Tasks;

namespace nseutils.unityoauth
{
    public interface IOauthUserInfoCompatible
    {

        public string userInfoUrl
        {
            get;
        }
        public UniTask<IOauthUserInfo> GetUserInfos();

    }
}