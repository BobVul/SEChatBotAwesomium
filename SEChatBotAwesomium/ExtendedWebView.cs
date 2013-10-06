using Awesomium.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SEChatBotAwesomium
{
    public class ExtendedWebView
    {
        public bool PageLoaded { get; set; }
        public bool FrameLoaded { get; set; }
        public Uri Source
        {
            get
            {
                return WebView.Source;
            }
            set
            {
                PageLoaded = false;
                WebView.Source = value;
            }
        }
        public bool AutoScreenshot { get; set; }
        public bool ScreenshotsEnabled { get; set; }

        private WebView webView = null;
        public WebView WebView
        {
            get
            {
                return webView;
            }
            set
            {
                if (webView != value)
                {
                    webView = value;
                    if (webView != null)
                    {
                        webView.LoadingFrameComplete += ExtendedWebView_LoadingFrameComplete;
                    }
                    RunningTime.Restart();
                }
            }
        }

        public Stopwatch RunningTime { get; private set; }

        public ExtendedWebView()
        {
            RunningTime = new Stopwatch();
        }

        public void SaveScreenshot(string path)
        {
            ((BitmapSurface)WebView.Surface).SaveToPNG(path);
        }

        void ExtendedWebView_LoadingFrameComplete(object sender, FrameEventArgs e)
        {
            if (e.IsMainFrame)
                PageLoaded = true;
            else
                FrameLoaded = true;
            if (AutoScreenshot)
                SaveScreenshot("screenshot" + RunningTime.ElapsedMilliseconds + ".png");
        }
    }
}
