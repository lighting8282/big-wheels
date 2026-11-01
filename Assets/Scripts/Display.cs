using UnityEngine;

namespace BigWheels
{
    /// <summary>Window and fullscreen options for desktop builds. Does nothing on phones or inside the Unity editor.</summary>
    public static class DisplayOptions
    {
        public static readonly Vector2Int[] Sizes = { new(960, 540), new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440) };

        public static bool Supported => !Application.isEditor &&
            (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer);
        /// <summary>The parent corner shows the screen rows on desktop builds (and in the editor, so they can be checked).</summary>
        public static bool Show => Supported || Application.isEditor;

        static Vector2Int Desktop
        {
            get
            {
                var d = Screen.mainWindowDisplayInfo;
                return d.width > 0 ? new Vector2Int(d.width, d.height) : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            }
        }

        /// <summary>A window has to leave room for the taskbar and title bar.</summary>
        static bool Fits(int i) { var d = Desktop; return Sizes[i].x <= d.x - 40 && Sizes[i].y <= d.y - 120; }

        /// <summary>The wanted preset, or the largest smaller one that fits on this desktop.</summary>
        public static int FitIndex(int wanted)
        {
            for (int i = Mathf.Clamp(wanted, 0, Sizes.Length - 1); i > 0; i--) if (Fits(i)) return i;
            return 0;
        }

        public static string SizeLabel { get { var s = Sizes[FitIndex(Settings.WindowSize)]; return s.x + " x " + s.y; } }

        public static void Apply()
        {
            if (!Supported) return;
            if (Settings.FullScreen) { var d = Desktop; Screen.SetResolution(d.x, d.y, FullScreenMode.FullScreenWindow); }
            else { var s = Sizes[FitIndex(Settings.WindowSize)]; Screen.SetResolution(s.x, s.y, FullScreenMode.Windowed); }
        }

        public static void ToggleFullscreen() { Settings.FullScreen = !Settings.FullScreen; Apply(); }

        /// <summary>Leaves fullscreen if it is on, otherwise steps to the next bigger window and wraps back to the smallest.</summary>
        public static void NextWindowSize()
        {
            if (Settings.FullScreen) { Settings.FullScreen = false; Apply(); return; }
            int next = FitIndex(Settings.WindowSize) + 1;
            if (next >= Sizes.Length || !Fits(next)) next = 0;
            Settings.WindowSize = next; Apply();
        }
    }
}
