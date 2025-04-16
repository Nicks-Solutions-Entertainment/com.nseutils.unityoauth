using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace nseutils.unityoauth.Browser
{
    public interface IBrowser
    {
        public bool useVitualRedirectUrl
        {
            get;
        }
        UniTask<BrowserResult> UTask_StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl);
        Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default);
    }
}