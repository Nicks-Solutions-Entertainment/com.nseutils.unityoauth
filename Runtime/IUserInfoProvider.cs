using System.Threading;
using System.Threading.Tasks;

namespace nseutils.unityoauth
{
    public interface IUserInfoProvider
    {
        /// <summary>
        /// Obtains user information using third-party authentication service using data provided via callback request.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task<IOauthUserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default);
    }
}