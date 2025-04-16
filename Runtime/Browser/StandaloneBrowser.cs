using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace nseutils.unityoauth.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser that runs a local server and waits for a call with
    /// the authorization verification code.
    /// </summary>
    public class StandaloneBrowser : IBrowser
    {
        string m_finalReedirect = "";
        public bool useVitualRedirectUrl => m_useVitualRedirectUrl;
        bool m_useVitualRedirectUrl = false;

        public StandaloneBrowser()
        {
        }
        public StandaloneBrowser(bool _usevRedirect = false)
        {
            m_useVitualRedirectUrl = _usevRedirect;

        }

        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

#if !UNITY_EDITOR && UNITY_WEBGL
        //[DllImport("__Internal")]
        //private static extern void StartSignin( string signinUrl);
        private void StartSignin(string signinUrl)
        {
            
        }
#else
        private void StartSignin(string signinUrl)
        {
            Debug.Log($"StartSignin ::{signinUrl}");
            Application.OpenURL(signinUrl);
        }
#endif

        /// <summary>
        /// Gets or sets the close page response. This HTML response is shown to the user after redirection is done.
        /// </summary>
        public string closePageResponse
        {
            get; set;
        } =
        //"<html><body><b>Redirecting to your URL...</b><br>(You can close this tab/window now)</body></html>";
        @"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>
                @prjectName | Redirecting...
            </title>
        </head>

        <body>
    
        <script>
            // document.onload = Start;
            document.addEventListener('DOMContentLoaded', Start);

            function Start(){
                const redirectUri = ('@redirectUri');
                const redirectUrl = MountRedirectUri(redirectUri);
        
        
                const redirectText = document.getElementById('redirect_lbl');
                const _redirectIsValid = redirectText!== null && 
                redirectUri!== null && redirectUri.length !== 0 && redirectUri !== '@redirectUri';
                //noa é innertext o texto dentro da tag!
                if(_redirectIsValid)
                    redirectText.innerText = `Reedirecting to\n${redirectUri ?? '...'}`;
                else
                    redirectText.innerText = `RedirectUrl not Defined on backend`;

                //adding to body
                document.body.appendChild(redirectText);

                setTimeout(() => {
                    if(_redirectIsValid)
                        window.location.href = redirectUrl;
                }, 800);
            }
    
            function MountRedirectUri(redirectUri){
                const currentUrl = new URL(window.location.href);
                const redirectUrl = new URL(redirectUri);

                const params = currentUrl.searchParams;

                params.forEach((value, key) => {
                    redirectUrl.searchParams.append(key, value);
                });

                return redirectUrl.toString();
            }
    
        </script>
            <p id=""redirect_lbl"" style=""font-family: system-ui, sans-serif; color: rgb(161, 161, 161); font-size: 1rem; font-weight: 500; align-self: center; position: absolute; display: flex; top: 50%; left: 50%; transform: translate(-50%, -50%); text-align: center;"">
                Redirecting to<br>
                ...
            </p>
        </body>
        </html> 
        ";


        //"<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";

        // @"
        //     <!DOCTYPE html>
        //     <html lang=""en"">
        //     <head>
        //         <meta charset=""UTF-8"">
        //         <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        //         <title>Authenticate Page</title>
        //         <script>
        //             const appUrl = ""@webAppUrl"";
        //             function SendMessageToApp(){
        //                 if(window.opener!==null)
        //                     window.opener.postMessage(window.location.href, appUrl);
        //             }
        //             window.addEventListener('beforeunload', (event) => {
        //                 SendMessageToApp();
        //             });
        //         </script>
        //     </head>
        //     <body>
        //         <script>
        //             function BackToApp() {
        //                 SendMessageToApp()
        //                 window.close();
        //             }
        //         </script>
        //         <button onclick=""BackToApp()"" style=""height: 48x; width: 256px;"">Fechar e Voltar para o App</button>
        //     </body>
        //     </html>
        // ";

        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, string virtualRedirectUrl, CancellationToken cancellationToken = default)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            HttpListener httpListener = new HttpListener()
            {

            };
            try
            {

                redirectUrl = AddForwardSlashIfNecessary(redirectUrl);
                virtualRedirectUrl = AddForwardSlashIfNecessary(virtualRedirectUrl);

                if (!useVitualRedirectUrl)
                    httpListener.Prefixes.Add(redirectUrl);
                else
                    httpListener.Prefixes.Add(virtualRedirectUrl);


                    httpListener.Start();
                Debug.Log($"StartAsync :: {loginUrl}");

                m_finalReedirect = redirectUrl;
                httpListener.BeginGetContext(IncomingHttpRequest, httpListener);
                StartSignin(loginUrl);

                //if (!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
                //    Application.ExternalEval($"window.open('{loginUrl}', '_blank')");
                //else
                //    Application.OpenURL(loginUrl);

                return await _taskCompletionSource.Task;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                httpListener.Stop();
            }
        }

        private void IncomingHttpRequest(IAsyncResult result)
        {
            var httpListener = (HttpListener)result.AsyncState;
            var httpContext = httpListener.EndGetContext(result);
            var httpRequest = httpContext.Request;

            Debug.Log($"IncomingHttpRequest...");

            bool isUnauthorized = httpRequest.Url.Query.Contains("error=access_denied");
            // Define a resposta HTML com base no status de autorização

            // Build a response to send an "ok" back to the browser for the user to see.
            var httpResponse = httpContext.Response;
            // Verifica se a URL contém um erro de autorização
            var output = httpResponse.OutputStream;
            var buffer = System.Text.Encoding.UTF8.GetBytes(closePageResponse);
            try
            {
                string webAppUrl = OAuthManager.Instance.applicationAbsoluteURL;
                if (string.IsNullOrEmpty(webAppUrl))
                    webAppUrl = "http://localhost";

                string _closePageResponse = closePageResponse;
                if(_closePageResponse.IndexOf("('@redirectUri')") !=1)
                    _closePageResponse = closePageResponse.Replace("('@redirectUri')", $"('{m_finalReedirect}')");

                //string _closePageResponse = closePageResponse.IndexOf("@webAppUrl") != -1 ? this.closePageResponse.Replace("@webAppUrl", webAppUrl) : this.closePageResponse;
                //Debug.Log($"closePageResponse: {_closePageResponse}");

                buffer = System.Text.Encoding.UTF8.GetBytes(_closePageResponse);
                // Send the output to the client browser.
                httpResponse.ContentLength64 = buffer.Length;
                output.Write(buffer, 0, buffer.Length);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw e;
            }


            output.Close();
            //_taskCompletionSource.SetResult(
            //    new BrowserResult(BrowserStatus.Success, httpRequest.Url.ToString()));

            // Define o resultado da tarefa com base no status de autorização
            var browserResult = isUnauthorized
                ? new BrowserResult(BrowserStatus.UnknownError, httpRequest.Url.ToString(), "Usuário não autorizado")
                : new BrowserResult(BrowserStatus.Success, httpRequest.Url.ToString());


            _taskCompletionSource.SetResult(browserResult);
        }

        public async UniTask<BrowserResult> UTask_StartAsync(string loginUrl, string redirectUrl, string virtualRedirectUrl)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();


            HttpListener httpListener = new HttpListener();

            try
            {

                redirectUrl = AddForwardSlashIfNecessary(redirectUrl);
                virtualRedirectUrl = AddForwardSlashIfNecessary(virtualRedirectUrl);

                if (!useVitualRedirectUrl)
                    httpListener.Prefixes.Add(redirectUrl);
                else
                    httpListener.Prefixes.Add(virtualRedirectUrl);


                httpListener.Start();
                Debug.Log($"StartAsync :: {loginUrl}");

                m_finalReedirect = redirectUrl;
                httpListener.BeginGetContext(IncomingHttpRequest, httpListener);
                StartSignin(loginUrl);


                return await _taskCompletionSource.Task;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                httpListener.Stop();
            }
        }

        /// <summary>
        /// Prefixes must end in a forward slash ("/")
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=net-7.0#remarks" />
        private string AddForwardSlashIfNecessary(string url)
        {
            string forwardSlash = "/";
            if (!url.EndsWith(forwardSlash))
            {
                url += forwardSlash;
            }

            return url;
        }

    }
}