using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace nseutils.unityoauth.Browser
{
    public class CrossPlatformBrowser : IBrowser
    {
        public readonly Dictionary<RuntimePlatform, IBrowser> _platformBrowsers =
            new Dictionary<RuntimePlatform, IBrowser>();

        public bool useVitualRedirectUrl
        {
            get
            {
                var browser = platformBrowsers.FirstOrDefault(x => x.Key == Application.platform).Value;
                return browser?.useVitualRedirectUrl ?? false;
            }
        }

        public IDictionary<RuntimePlatform, IBrowser> platformBrowsers => _platformBrowsers;

        public async UniTask<BrowserResult> UTask_StartAsync(
            string loginUrl, string redirectUrl, string virtualRedirectUrl)
        {
            var browser = platformBrowsers.FirstOrDefault(x => x.Key == Application.platform).Value;
            if (browser == null)
            {
                //throw new NotSupportedException($"There is no browser found for '{Application.platform}' platform.");
                Debug.LogWarning($"There is no browser found for '{Application.platform}' platform. Using StandaloneBrowser by default");
                browser = new StandaloneBrowser();
            }

            return await browser.UTask_StartAsync(loginUrl, redirectUrl, virtualRedirectUrl);
        }

        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default)
        {
            var browser = platformBrowsers.FirstOrDefault(x => x.Key == Application.platform).Value;
            if (browser == null)
            {
                //throw new NotSupportedException($"There is no browser found for '{Application.platform}' platform.");
                Debug.LogWarning($"There is no browser found for '{Application.platform}' platform. Using StandaloneBrowser by default");
                browser = new StandaloneBrowser();
            }

            return await browser.StartAsync(loginUrl, redirectUrl, virtualRedirectUrl, cancellationToken);
        }
    }
}