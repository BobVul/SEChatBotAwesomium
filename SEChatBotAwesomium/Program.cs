using Awesomium.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace SEChatBotAwesomium
{
    class Program
    {
        static WebSession aweSession = null;
        static WebView aweView = null;

        static void Main(string[] args)
        {
            try
            {
                WebPreferences prefs = new WebPreferences();
                prefs.LoadImagesAutomatically = false;
                prefs.RemoteFonts = false;
                prefs.WebAudio = false;
                prefs.Dart = false;
                prefs.CanScriptsCloseWindows = false;
                prefs.CanScriptsOpenWindows = false;
                prefs.WebSecurity = false;
                prefs.Javascript = true;
                prefs.LocalStorage = true;
                prefs.Databases = false;            // ?

                aweSession = WebCore.CreateWebSession("session.dat", prefs);
                aweSession.ClearCookies();
                aweView = WebCore.CreateWebView(1, 1, aweSession);
                aweView.ResponsiveChanged += aweView_ResponsiveChanged;
                aweView.Crashed += aweView_Crashed;
                aweView.ConsoleMessage += aweView_ConsoleMessage;

                
            }
            finally
            {
                if (aweView != null && aweView.IsResponsive)
                    aweView.Dispose();
                WebCore.Shutdown();
            }
        }

        static void aweView_ResponsiveChanged(object sender, ResponsiveChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        static void aweView_Crashed(object sender, CrashedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        static void aweView_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void go(String s_username, String s_password, String scriptPath, String chatURL)
        {
            if (!Uri.IsWellFormedUriString(chatURL, UriKind.Absolute))
            {
                throw new UriFormatException("The provided chat URL \"" + chatURL + "\" is invalid!");
            }
            aweView.Source = chatURL.ToUri();
            while (aweView.IsLoading)
            {
                WebCore.Update();
            }

            Console.WriteLine(aweView.Source + ": Chatroom loaded! Waiting for the chat JS.");
            Thread.Sleep(5000);

            Console.WriteLine(aweView.Source + ": In the chatroom! Determining if we need to login...");
            // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

            JSObject loginLink = null;
            // Awesomium doesn't seem to have any method to traverse/search the DOM, so let's do it in JS! (urgh)
            loginLink = aweView.ExecuteJavascriptWithResult("document.evaluate(\"//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;");
            //loginLink = dri.findElement(By.xpath("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]"));
            if (loginLink != null)
            {
                Console.WriteLine("Crap. We need to login.");
                loginLink.Invoke("click");
            }
            else
            {
                Console.WriteLine("We don't need to login!");
            }

            if(loginLink != null)
            {
                //Need to login
                while (aweView.IsLoading)
                {
                    WebCore.Update();
                }
                Console.WriteLine(aweView.Source + ": Performing stage 1 chat-login auth link");
                // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                JSObject midpointLink = null;
                midpointLink = aweView.ExecuteJavascriptWithResult("document.evaluate(\"//a[contains(@href, '/users/chat-login')]\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;");
                //midpointLink = dri.findElement(By.xpath("//a[contains(@href, '/users/chat-login')]"));
                if (midpointLink != null)
                {
                    Console.WriteLine("We don't have a network cookie at all.");
                    midpointLink.Invoke("click");
                }
                else
                {
                    Console.WriteLine("Sweet! We're logged in!");
                }
                if(midpointLink != null)
                {
                    while (aweView.IsLoading)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(aweView.Source + ": We should be at the page that asks you to pick a sign-in method now...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
    
                    aweView.ExecuteJavascriptWithResult("openid.signin('stack_exchange');");
                    // Perhaps use the .LoadingFrameComplete event instead?
                    while (aweView.IsLoading)
                    {
                        WebCore.Update();
                    }
                    Thread.Sleep(2000);
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                    Console.WriteLine(aweView.Source + ": We should be at the sign-in page now...");
    
                    aweView.ExecuteJavascriptWithResult("document.getElementById(\"email\").value = " + s_username, "//frame[name='affiliate-signin-iframe']");
                    aweView.ExecuteJavascriptWithResult("document.getElementById(\"password\").value = " + s_password, "//frame[name='affiliate-signin-iframe']");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                    JSObject submitButton = aweView.ExecuteJavascriptWithResult("document.getElementsByClassName(\"affiliate-button\")", "//frame[name='affiliate-signin-iframe']");
                    if(submitButton == null)
                        throw new Exception("Couldn't find the affiliate-button classed submit button!");
                    submitButton.Invoke("click");
                    while (aweView.IsLoading)
                    {
                        WebCore.Update();
                    }
                    Thread.Sleep(5000);
                    Console.WriteLine(aweView.Source + ": We should be logged in now...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    aweView.Source = chatURL.ToUri();
                    while (aweView.IsLoading)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(aweView.Source + ": Chatroom loaded! Waiting for the chat JS.");
                    Thread.Sleep(5000);

                    Console.WriteLine(aweView.Source + ": In the chatroom! Authenticating...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    loginLink = aweView.ExecuteJavascriptWithResult("document.evaluate(\"//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;");
                    loginLink.Invoke("click");
                    Thread.Sleep(8000);
                }
            }
            
            String content = File.ReadAllText(scriptPath);
            if (content == null || content.Length == 0)
                throw new IOException("Couldn't load " + scriptPath);
            Console.WriteLine(aweView.Source + ": Executing bot script...");
            aweView.ExecuteJavascript(content);

            Console.WriteLine("Running...");
            // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
            
            string line;
            while (true)
            {
                line = Console.ReadLine();
                aweView.ExecuteJavascript(line);
            }

        }
    }
}
