using Awesomium.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Mono.Options;

namespace SEChatBotAwesomium
{
    class Program
    {
        static WebSession aweSession = null;
        static WebView aweView = null;
        static ExtendedWebView eView = new ExtendedWebView();

        static void Main(string[] args)
        {
            bool help = false;
            bool screens = false;
            string user = null, pass = null;
            string chatUrl = null, scriptUrl = null;
            string sessionPath = null;
            // http://geekswithblogs.net/robz/archive/2009/11/22/command-line-parsing-with-mono.options.aspx
            OptionSet options = new OptionSet()
                .Add("?|h|help", "Print this message", option => help = option != null)
                .Add("u=|user=|username=", "Required: StackExchange username or email", option => user = option)
                .Add("p=|pass=|password=", "Required: StackExchange password", option => pass = option)
                .Add("c=|chat-url=", "Required: Chatroom URL", option => chatUrl = option)
                .Add("b=|bot-script=|script-url=", "Required: The URL of a bot script", option => scriptUrl = option)
                .Add("s|screens|screenshot", "Display screenshots at checkpoints", option => screens = option != null)
                .Add("session-path=", "Path to browser session (profile), where settings are saved", option => sessionPath = option);

#if DEBUG
            string[] lines = File.ReadAllLines("account.txt");
            user = lines[0];
            pass = lines[1];
            chatUrl = "http://chat.stackexchange.com/rooms/118/root-access";
            scriptUrl = "https://raw.github.com/allquixotic/SO-ChatBot/master/master.js";
            screens = true;
            sessionPath = "session";
#else
            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(options, "Error - usage is:");
            }

            if (help)
            {
                ShowHelp(options, "Usage:");
            }

            if (user == null)
            {
                ShowHelp(options, "Error: A username is required");
            }

            if (pass == null)
            {
                ShowHelp(options, "Error: A password is required");
            }

            if (chatUrl == null)
            {
                ShowHelp(options, "Error: A chat URL is required");
            }

            if (scriptUrl == null)
            {
                ShowHelp(options, "Error: A bot script is required");
            }

            if (sessionPath == null)
            {
                sessionPath = "session";
            }
#endif

            try
            {
                WebConfig config = new WebConfig();
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

                aweSession = WebCore.CreateWebSession(sessionPath, prefs);
                aweSession.ClearCookies();
                aweView = WebCore.CreateWebView(1024, 768, aweSession);
                aweView.ResponsiveChanged += aweView_ResponsiveChanged;
                aweView.Crashed += aweView_Crashed;
                aweView.ConsoleMessage += aweView_ConsoleMessage;
                eView.WebView = aweView;
                eView.AutoScreenshot = true;
                eView.ScreenshotsEnabled = screens;

                go(user, pass, scriptUrl, chatUrl);
            }
            finally
            {
                if (aweView != null && aweView.IsResponsive)
                    aweView.Dispose();
                WebCore.Shutdown();
            }
        }

        static void ShowHelp(OptionSet options, string message = "")
        {
            Console.Error.WriteLine(message);
            options.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
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

        static void go(String s_username, String s_password, String scriptUrl, String chatUrl)
        {
            if (!Uri.IsWellFormedUriString(chatUrl, UriKind.Absolute))
            {
                throw new UriFormatException("The provided chat URL \"" + chatUrl + "\" is invalid!");
            }
            eView.Source = chatUrl.ToUri();
            while (!eView.PageLoaded)
            {
                WebCore.Update();
            }

            Console.WriteLine(eView.Source + ": In the chatroom! Determining if we need to login...");

            // Awesomium doesn't seem to have any method to traverse/search the DOM, so let's do it in JS! (urgh)

            bool isLoggedIn = true;
            dynamic loginLink;

            dynamic document;
            using (document = (JSObject)aweView.ExecuteJavascriptWithResult("document"))
            {
                using (loginLink = document.evaluate("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue)
                {

                    if (loginLink != null)
                    {
                        Console.WriteLine("Crap. We need to login.");
                        isLoggedIn = false;
                        string link = loginLink.ToString();
                        eView.Source = link.ToUri();
                    }
                    else
                    {
                        Console.WriteLine("We don't need to login!");
                        isLoggedIn = true;
                    }
                }
            }


            if (!isLoggedIn)
            {
                //Need to login
                while (!eView.PageLoaded)
                {
                    WebCore.Update();
                }
                Console.WriteLine(eView.Source + ": Performing stage 1 chat-login auth link");

                using (document = (JSObject)aweView.ExecuteJavascriptWithResult("document"))
                {
                    dynamic midpointLink;
                    using (midpointLink = document.evaluate("//a[contains(@href, '/users/chat-login')]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue)
                    {
                        if (midpointLink != null)
                        {
                            Console.WriteLine("We don't have a network cookie at all.");
                            isLoggedIn = false;
                            string link = midpointLink.ToString();
                            eView.Source = link.ToUri();
                        }
                        else
                        {
                            Console.WriteLine("Sweet! We're logged in!");
                            isLoggedIn = true;
                        }
                    }
                }


                if (!isLoggedIn)
                {
                    while (!eView.PageLoaded)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(eView.Source + ": We should be at the page that asks you to pick a sign-in method now...");

                    eView.FrameLoaded = false;
                    aweView.ExecuteJavascriptWithResult("openid.signin('stack_exchange');");
                    while (!eView.FrameLoaded)
                    {
                        WebCore.Update();
                    }

                    Console.WriteLine(eView.Source + ": We should be at the sign-in page now...");

                    eView.FrameLoaded = false;
                    while (!eView.FrameLoaded)
                    {
                        WebCore.Update();
                    }

                    using (document = (JSObject)aweView.ExecuteJavascriptWithResult("document", "//iframe[@id='affiliate-signin-iframe']"))
                    {
                        dynamic element;
                        using (element = document.getElementById("email"))
                        {
                            element.value = s_username;
                        }
                        using (element = document.getElementById("password"))
                        {
                            element.value = s_password;
                        }

                        using (element = document.getElementsByClassName("affiliate-button")[0])
                        {
                            if (element == null)
                                throw new Exception("Couldn't find the affiliate-button classed submit button!");
                            eView.PageLoaded = false;
                            element.click();
                        }
                    }

                    while (!eView.PageLoaded)
                    {
                        WebCore.Update();
                    }
                    Console.WriteLine(eView.Source + ": We should be logged in now...");
                    // ImagePanel.displayImage(dri.getScreenshotAs(OutputType.BASE64));

                    eView.Source = chatUrl.ToUri();
                    while (!eView.PageLoaded)
                    {
                        WebCore.Update();
                    }
                    //Console.WriteLine(eView.Source + ": Chatroom loaded! Waiting for the chat JS.");
                    //Thread.Sleep(5000);

                    Console.WriteLine(eView.Source + ": In the chatroom! Authenticating...");

                    eView.SaveScreenshot("screenshot" + eView.RunningTime.ElapsedMilliseconds + ".png");

                    using (document = (JSObject)aweView.ExecuteJavascriptWithResult("document"))
                    {
                        using (loginLink = document.evaluate("//a[starts-with(@href, '/login/global') and text() = 'logged in' and not(ancestor::div[contains(@style,'display:none')]) and not(ancestor::div[contains(@style,'display: none')])]", document, null, 9 /*XPathResult.FIRST_ORDERED_NODE_TYPE*/, null).singleNodeValue)
                        {
                            if (loginLink != null)
                            {
                                string link = loginLink.ToString();
                                eView.Source = link.ToUri();
                                while (!eView.PageLoaded)
                                {
                                    WebCore.Update();
                                }

                                Console.WriteLine(eView.Source + ": Returning to the chatroom...");

                                // We **really** want to be sure we're back in the chatroom after that redirect.
                                while (eView.Source.ToString() != chatUrl.ToString())
                                {
                                    while (!eView.PageLoaded)
                                    {
                                        //eView.SaveScreenshot("screenshot" + eView.RunningTime.ElapsedMilliseconds + ".png");

                                        WebCore.Update();
                                    }
                                    eView.PageLoaded = false;
                                }
                            }
                        }
                    }
                }
            }
            eView.SaveScreenshot("screenshot" + eView.RunningTime.ElapsedMilliseconds + ".png");
            
            Console.WriteLine(eView.Source + ": Executing bot script...");

            aweView.ExecuteJavascriptWithResult("var a=document.createElement(\"script\");a.src=\"" + scriptUrl + "\",document.head.appendChild(a)");

            Console.WriteLine("Running...");

#if DEBUG
            File.WriteAllText("dump.html", aweView.ExecuteJavascriptWithResult("document.getElementsByTagName('html')[0].innerHTML"));
#endif

            eView.SaveScreenshot("screenshot" + eView.RunningTime.ElapsedMilliseconds + ".png");
            
            string line;
            while (true)
            {
                line = Console.ReadLine();
                aweView.ExecuteJavascriptWithResult(line);

                eView.SaveScreenshot("screenshot" + eView.RunningTime.ElapsedMilliseconds + ".png");
            }

        }
    }
}
