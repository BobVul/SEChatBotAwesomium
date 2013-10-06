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
        static bool pageLoaded = false;
        static bool frameLoaded = false;
        static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        static void Main(string[] args)
        {
            try
            {
                WebConfig config = new WebConfig();
                config.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_6_8) AppleWebKit/537.13+ (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2";
                WebCore.Initialize(config);

                WebPreferences prefs = new WebPreferences();
                prefs.LoadImagesAutomatically = true;
                prefs.RemoteFonts = false;
                prefs.WebAudio = false;
                prefs.Dart = false;
                prefs.CanScriptsCloseWindows = false;
                prefs.CanScriptsOpenWindows = false;
                prefs.WebSecurity = false;
                prefs.Javascript = true;
                prefs.LocalStorage = true;
                prefs.Databases = false;            // ?

                aweSession = WebCore.CreateWebSession("session", prefs);
                aweSession.ClearCookies();
                aweView = WebCore.CreateWebView(1024, 768, aweSession);
                aweView.ResponsiveChanged += aweView_ResponsiveChanged;
                aweView.Crashed += aweView_Crashed;
                aweView.ConsoleMessage += aweView_ConsoleMessage;
                aweView.LoadingFrameComplete += aweView_LoadingFrameComplete;

                string[] lines = File.ReadAllLines("account.txt");
                sw.Start();
                go(lines[0], lines[1], "master.min.js", "http://chat.stackexchange.com/rooms/118/root-access");
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
            Console.WriteLine("{0}: {1} {2} {3}: {4}", e.EventType, e.EventName, e.Source, e.LineNumber, e.Message);
        }

        static void aweView_LoadingFrameComplete(object sender, FrameEventArgs e)
        {
            if (e.IsMainFrame)
            {
                BitmapSurface surface = (BitmapSurface)aweView.Surface;
                surface.SaveToPNG("screenshot" + sw.ElapsedMilliseconds + ".png");
                pageLoaded = true;
            }
            else
            {
                BitmapSurface surface = (BitmapSurface)aweView.Surface;
                surface.SaveToPNG("screenshot" + sw.ElapsedMilliseconds + "f.png");
                frameLoaded = true;
            }
        }

        static void go(String s_username, String s_password, String scriptPath, String chatURL)
        {
            if (!Uri.IsWellFormedUriString(chatURL, UriKind.Absolute))
            {
                throw new UriFormatException("The provided chat URL \"" + chatURL + "\" is invalid!");
            }
            pageLoaded = false;
            aweView.Source = chatURL.ToUri();
            while (!pageLoaded)
            {
                WebCore.Update();
            }

            //Console.WriteLine(aweView.Source + ": Chatroom loaded! Waiting for the chat JS.");
            //Thread.Sleep(5000);

            Console.WriteLine(aweView.Source + ": In the chatroom! Determining if we need to login...");
            // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

            dynamic document = (JSObject)aweView.ExecuteJavascriptWithResult("document");

            // Awesomium doesn't seem to have any method to traverse/search the DOM, so let's do it in JS! (urgh)
            dynamic loginLink = document.evaluate("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue;
            //loginLink = dri.findElement(By.xpath("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]"));

            if (loginLink != null)
            {
                Console.WriteLine("Crap. We need to login.");
                string link = loginLink.ToString();
                pageLoaded = false;
                aweView.Source = link.ToUri();
            }
            else
            {
                Console.WriteLine("We don't need to login!");
            }

            if (loginLink != null)
            {
                //Need to login
                while (!pageLoaded)
                {
                    WebCore.Update();
                }
                Console.WriteLine(aweView.Source + ": Performing stage 1 chat-login auth link");
                // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                document = (JSObject)aweView.ExecuteJavascriptWithResult("document");
                dynamic midpointLink = document.evaluate("//a[contains(@href, '/users/chat-login')]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue;
                //midpointLink = dri.findElement(By.xpath("//a[contains(@href, '/users/chat-login')]"));
                if (midpointLink != null)
                {
                    Console.WriteLine("We don't have a network cookie at all.");
                    string link = midpointLink.ToString();
                    pageLoaded = false;
                    aweView.Source = link.ToUri();
                }
                else
                {
                    Console.WriteLine("Sweet! We're logged in!");
                }
                if(midpointLink != null)
                {
                    while (!pageLoaded)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(aweView.Source + ": We should be at the page that asks you to pick a sign-in method now...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                    
                    // TODO: only works up to around here


                    frameLoaded = false;
                    aweView.ExecuteJavascriptWithResult("openid.signin('stack_exchange');");
                    while (!frameLoaded)
                    {
                        WebCore.Update();
                    }
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));
                    Console.WriteLine(aweView.Source + ": We should be at the sign-in page now...");

                    frameLoaded = false;
                    while (!frameLoaded)
                    {
                        WebCore.Update();
                    }

                    document = (JSObject)aweView.ExecuteJavascriptWithResult("document", "//iframe[@id='affiliate-signin-iframe']");
                    document.getElementById("email").value = s_username;
                    document.getElementById("password").value = s_password;
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    //aweView.ExecuteJavascriptWithResult("var evt = document.createEvent('HTMLEvents'); evt.initEvent('click', true, true ); document.getElementsByClassName('affiliate-button')[0].dispatchEvent(evt); ");
                    dynamic submitButton = document.getElementsByClassName("affiliate-button")[0];
                    if(submitButton == null)
                        throw new Exception("Couldn't find the affiliate-button classed submit button!");
                    pageLoaded = false;
                    submitButton.click();
                    while (!pageLoaded)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(aweView.Source + ": We should be logged in now...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    pageLoaded = false;
                    aweView.Source = chatURL.ToUri();
                    while (!pageLoaded)
                    {
                        WebCore.Update();
                    }
                    //Console.WriteLine(aweView.Source + ": Chatroom loaded! Waiting for the chat JS.");
                    //Thread.Sleep(5000);

                    Console.WriteLine(aweView.Source + ": In the chatroom! Authenticating...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    Thread.Sleep(3000);


                    BitmapSurface surface = (BitmapSurface)aweView.Surface;
                    surface.SaveToPNG("screenshot" + sw.ElapsedMilliseconds + ".png");

                    document = (JSObject)aweView.ExecuteJavascriptWithResult("document");
                    loginLink = document.evaluate("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue;
                    if (loginLink != null)
                    {
                        string link = loginLink.ToString();
                        pageLoaded = false;
                        aweView.Source = link.ToUri();
                        while (!pageLoaded)
                        {
                            WebCore.Update();
                        }
                    }
                }
            }


            Thread.Sleep(5000);
            ((BitmapSurface)aweView.Surface).SaveToPNG("screenshot" + sw.ElapsedMilliseconds + ".png");
            
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
