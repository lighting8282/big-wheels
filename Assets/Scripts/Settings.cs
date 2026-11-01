using UnityEngine;

namespace BigWheels
{
    /// <summary>Parent settings, saved in PlayerPrefs.</summary>
    public static class Settings
    {
        const string VolKey = "bw_volume", EngineKey = "bw_engine", WorldKey = "bw_world", BreakKey = "bw_break", FullKey = "bw_full", WinKey = "bw_win", BestKey = "bw_best", CockKey = "bw_cockpit";
        static bool loaded;
        static float volume = 1f; static bool engine = true; static int world = -1, brk;
        static bool full, bestDirty, cockpit; static int win = 1, best; static readonly int[] cos = new int[5];

        static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolKey, 1f)); engine = PlayerPrefs.GetInt(EngineKey, 1) == 1;
                world = Mathf.Clamp(PlayerPrefs.GetInt(WorldKey, -1), -1, 3); brk = Mathf.Max(0, PlayerPrefs.GetInt(BreakKey, 0));
                full = PlayerPrefs.GetInt(FullKey, 0) == 1; win = Mathf.Clamp(PlayerPrefs.GetInt(WinKey, 1), 0, 4); best = Mathf.Max(0, PlayerPrefs.GetInt(BestKey, 0)); cockpit = PlayerPrefs.GetInt(CockKey, 0) == 1;
                for (int i = 0; i < cos.Length; i++) cos[i] = Mathf.Max(0, PlayerPrefs.GetInt("bw_cos" + i, 0));
            }
            catch { }
        }
        static void Save()
        {
            try
            {
                PlayerPrefs.SetFloat(VolKey, volume); PlayerPrefs.SetInt(EngineKey, engine ? 1 : 0);
                PlayerPrefs.SetInt(WorldKey, world); PlayerPrefs.SetInt(BreakKey, brk);
                PlayerPrefs.SetInt(FullKey, full ? 1 : 0); PlayerPrefs.SetInt(WinKey, win); PlayerPrefs.SetInt(BestKey, best); PlayerPrefs.SetInt(CockKey, cockpit ? 1 : 0);
                for (int i = 0; i < cos.Length; i++) PlayerPrefs.SetInt("bw_cos" + i, cos[i]);
                PlayerPrefs.Save();
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
        /// <summary>True = the game opens in the first-person cockpit view.</summary>
        public static bool CockpitDefault { get { Load(); return cockpit; } set { Load(); cockpit = value; Save(); } }
        /// <summary>Saved cosmetic choices: 0 paint, 1 suit, 2 wheel, 3 hanging charm, 4 dash sticker.</summary>
        public static int GetCos(int i) { Load(); return cos[i]; }
        public static void SetCos(int i, int v) { Load(); cos[i] = Mathf.Max(0, v); Save(); }
        /// <summary>Index into DisplayOptions.Sizes for the windowed size.</summary>
        public static int WindowSize { get { Load(); return win; } set { Load(); win = Mathf.Clamp(value, 0, 4); Save(); } }
        /// <summary>Best score over all runs. Kept in memory while playing and written out by FlushBest.</summary>
        public static int Best { get { Load(); return best; } set { Load(); value = Mathf.Max(0, value); if (value != best) { best = value; bestDirty = true; } } }
        public static void FlushBest() { if (bestDirty) { bestDirty = false; Save(); } }

        /// <summary>Test hooks.</summary>
        public static void ResetAll()
        {
            try { PlayerPrefs.DeleteKey(VolKey); PlayerPrefs.DeleteKey(EngineKey); PlayerPrefs.DeleteKey(WorldKey); PlayerPrefs.DeleteKey(BreakKey);
                      PlayerPrefs.DeleteKey(FullKey); PlayerPrefs.DeleteKey(WinKey); PlayerPrefs.DeleteKey(BestKey); PlayerPrefs.DeleteKey(CockKey); for (int i = 0; i < 5; i++) PlayerPrefs.DeleteKey("bw_cos" + i); } catch { }
            loaded = false; bestDirty = false;
        }
        public static void ForgetCache() { loaded = false; }
    }
}
