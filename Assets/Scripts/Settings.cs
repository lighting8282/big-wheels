using UnityEngine;

namespace BigWheels
{
    /// <summary>Parent settings, saved in PlayerPrefs.</summary>
    public static class Settings
    {
        const string VolKey = "bw_volume", EngineKey = "bw_engine", WorldKey = "bw_world", BreakKey = "bw_break", FullKey = "bw_full", WinKey = "bw_win", BestKey = "bw_best";
        static bool loaded;
        static float volume = 1f; static bool engine = true; static int world = -1, brk;
        static bool full, bestDirty; static int win = 1, best;

        static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolKey, 1f)); engine = PlayerPrefs.GetInt(EngineKey, 1) == 1;
                world = Mathf.Clamp(PlayerPrefs.GetInt(WorldKey, -1), -1, 3); brk = Mathf.Max(0, PlayerPrefs.GetInt(BreakKey, 0));
                full = PlayerPrefs.GetInt(FullKey, 0) == 1; win = Mathf.Clamp(PlayerPrefs.GetInt(WinKey, 1), 0, 4); best = Mathf.Max(0, PlayerPrefs.GetInt(BestKey, 0));
            }
            catch { }
        }
        static void Save()
        {
            try
            {
                PlayerPrefs.SetFloat(VolKey, volume); PlayerPrefs.SetInt(EngineKey, engine ? 1 : 0);
                PlayerPrefs.SetInt(WorldKey, world); PlayerPrefs.SetInt(BreakKey, brk);
                PlayerPrefs.SetInt(FullKey, full ? 1 : 0); PlayerPrefs.SetInt(WinKey, win); PlayerPrefs.SetInt(BestKey, best); PlayerPrefs.Save();
            }
            catch { }
        }

        /// <summary>Master volume, 0..1.</summary>
        public static float Volume { get { Load(); return volume; } set { Load(); volume = Mathf.Clamp01(value); Save(); } }
        public static bool EngineOn { get { Load(); return engine; } set { Load(); engine = value; Save(); } }
        /// <summary>-1 = the world changes by itself, otherwise 0 farm, 1 beach, 2 snow, 3 night stays put.</summary>
        public static int WorldLock { get { Load(); return world; } set { Load(); world = Mathf.Clamp(value, -1, 3); Save(); } }
        /// <summary>Minutes of play before the calm "time for a break" screen. 0 = off.</summary>
        public static int BreakMinutes { get { Load(); return brk; } set { Load(); brk = Mathf.Max(0, value); Save(); } }

        /// <summary>Desktop only: true = borderless fullscreen, false = a window.</summary>
        public static bool FullScreen { get { Load(); return full; } set { Load(); full = value; Save(); } }
        /// <summary>Index into DisplayOptions.Sizes for the windowed size.</summary>
        public static int WindowSize { get { Load(); return win; } set { Load(); win = Mathf.Clamp(value, 0, 4); Save(); } }
        /// <summary>Best score over all runs. Kept in memory while playing and written out by FlushBest.</summary>
        public static int Best { get { Load(); return best; } set { Load(); value = Mathf.Max(0, value); if (value != best) { best = value; bestDirty = true; } } }
        public static void FlushBest() { if (bestDirty) { bestDirty = false; Save(); } }

        /// <summary>Test hooks.</summary>
        public static void ResetAll()
        {
            try { PlayerPrefs.DeleteKey(VolKey); PlayerPrefs.DeleteKey(EngineKey); PlayerPrefs.DeleteKey(WorldKey); PlayerPrefs.DeleteKey(BreakKey);
                      PlayerPrefs.DeleteKey(FullKey); PlayerPrefs.DeleteKey(WinKey); PlayerPrefs.DeleteKey(BestKey); } catch { }
            loaded = false; bestDirty = false;
        }
        public static void ForgetCache() { loaded = false; }
    }
}
