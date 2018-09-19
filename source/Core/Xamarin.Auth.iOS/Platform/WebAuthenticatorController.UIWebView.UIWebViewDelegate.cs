using System;
using System.Threading.Tasks;
using System.Text;
using Foundation;
using UIKit;
using WebKit;

namespace Xamarin.Auth
{
    internal partial class WebAuthenticatorController
    {
        //==============================================================================================================

        internal class UIWebViewDelegate : UIKit.UIWebViewDelegate
        {
            protected WebAuthenticatorController controller;
            Uri lastUrl;


            public UIWebViewDelegate(WebAuthenticatorController controller)
            {
                this.controller = controller;
            }

            public override bool ShouldStartLoad(UIWebView webView, NSUrlRequest request, UIWebViewNavigationType navigationType)
            {
                NSUrl nsUrl = request.Url;
    
                string msg = null;

#if DEBUG
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"UIWebViewDelegate.ShouldStartLoad ");
                sb.AppendLine($"        nsUrl.AbsoluteString = {nsUrl.AbsoluteString}");
                sb.AppendLine($"        WebViewConfiguration.UserAgent = {WebViewConfiguration.UserAgent}");
                System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif
                
                WebAuthenticator wa = null;
                WebRedirectAuthenticator wra = null;

                wa = this.controller.authenticator as WebAuthenticator;
                wra = this.controller.authenticator as WebRedirectAuthenticator;

#if DEBUG
                if (wa != null)
                {
                    msg = String.Format("WebAuthenticatorController.authenticator as WebAuthenticator");
                    System.Diagnostics.Debug.WriteLine(msg);
                }
                if (wra != null)
                {
                    msg = String.Format("WebAuthenticatorController.authenticator as WebRedirectAuthenticator");
                    System.Diagnostics.Debug.WriteLine(msg);
                }

                msg = String.Format("WebAuthenticatorController.ShouldStartLoad {0}", nsUrl.AbsoluteString);
                System.Diagnostics.Debug.WriteLine(msg);
#endif

                bool is_loadable_url = false;
                if (nsUrl != null && !controller.authenticator.HasCompleted)
                {
                    Uri url;
                    if (Uri.TryCreate(nsUrl.AbsoluteString, UriKind.Absolute, out url))
                    {
                        string host = url.Host.ToLower();
                        string scheme = url.Scheme;

#if DEBUG
                        msg = String.Format("WebAuthenticatorController.ShouldStartLoad {0}", url.AbsoluteUri);
                        System.Diagnostics.Debug.WriteLine(msg);
                        msg = string.Format("                          Host   = {0}", host);
                        System.Diagnostics.Debug.WriteLine(msg);
                        msg = string.Format("                          Scheme = {0}", scheme);
                        System.Diagnostics.Debug.WriteLine(msg);
#endif

                        if (host == "localhost" || host == "127.0.0.1" || host == "::1")
                        {
                            is_loadable_url = false;
                            this.controller.DismissViewControllerAsync(true);
                        }
                        else
                        {
                            is_loadable_url = true;
                        }

                        controller.authenticator.OnPageLoading(url);
                    }
                }

                if (wra != null)
                {
                    // TODO: class refactoring
                    // OAuth2Authenticator is WebRedirectAuthenticator wra
                    wra.IsLoadableRedirectUri = is_loadable_url;
                    return wra.IsLoadableRedirectUri;
                }
                else if (wa != null)
                {
                    // TODO: class refactoring
                    // OAuth1Authenticator is WebRedirectAuthenticator wra
                    return is_loadable_url;
                }

                return false;
            }

            public override void LoadStarted(UIWebView webView)
            {
#if DEBUG
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"UIWebViewDelegate.LoadStarted ");
                System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif

                controller.activity.StartAnimating();

                webView.UserInteractionEnabled = false;
            }

            public override void LoadFailed(UIWebView webView, NSError error)
            {
#if DEBUG
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"UIWebViewDelegate.LoadFailed ");
                sb.AppendLine($"        error.Code   = {error.Code}");
                sb.AppendLine($"        error.Domain = {error.Domain}");
                sb.AppendLine($"        error.Domain = {error.LocalizedDescription}");
                sb.AppendLine($"        error.Domain = {error.LocalizedFailureReason}");
                sb.AppendLine($"        error.Domain = {error.LocalizedRecoveryOptions}");
                sb.AppendLine($"        error.Domain = {error.LocalizedRecoverySuggestion}");
                System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif

                if (error.Domain == "WebKitErrorDomain")
                {
                    if (error.Code == 102)
                    {
                        // 
                        // WebViewDelegate.ShouldStartLoad returned false
                        // localhost, 127.0.0.1, ::1
                        // TODO: custom uris
                        // No need to show error - return immediately
                        return;
                    }
                }
                else if (error.Domain == "NSURLErrorDomain")
                {
                    // {The operation couldn’t be completed. (NSURLErrorDomain error -999.)}
                    if (error.Code == -999)
                    {
                        // delegate is getting a "cancelled" (-999) failure, 
                        //      that might be originated in javascript or 
                        //      fast clicks!!
                        //      perhaps even in a UIWebView bug.
                        return;
                    }
                }
                else
                {
                    controller.activity.StopAnimating();
                }

                webView.UserInteractionEnabled = true;

                controller.authenticator.OnError(error.LocalizedDescription);

                return;
            }

            public override void LoadingFinished(UIWebView webView)
            {
                controller.activity.StopAnimating();

                webView.UserInteractionEnabled = true;

                var url = new Uri(webView.Request.Url.AbsoluteString);
                if (url != lastUrl && !controller.authenticator.HasCompleted)
                {
                    lastUrl = url;
                    controller.authenticator.OnPageLoaded(url);
                }

                return;
            }
        }

        //==============================================================================================================
    }
}
