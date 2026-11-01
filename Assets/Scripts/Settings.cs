using UnityEngine;

namespace BigWheels
{
    /// <summary>Parent settings, saved in PlayerPrefs.</summary>
    public static class Settings
    {
        const string VolKey = "bw_volume", EngineKey = "bw_engine", WorldKey = "bw_world", BreakKey = "bw_break";
        static bool loaded;
        static float volume = 1f; static bool engine = true; static int world = -1, brk;

        static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolKey, 1f)); engine = PlayerPrefs.GetInt(EngineKey, 1) == 1;
                world = Mathf.Clamp(PlayerPrefs.GetInt(WorldKey, -1), -1, 3); brk = Mathf.Max(0, PlayerPrefs.GetInt(BreakKey, 0));
            }
            catch { }
        }
        static void Save()
        {
            try
            {
                PlayerPrefs.SetFloat(VolKey, volume); PlayerPrefs.SetInt(EngineKey, engine ? 1 : 0);
                PlayerPrefs.SetInt(WorldKey, world); PlayerPrefs.SetInt(BreakKey, brk); PlayerPrefs.Save();
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

        /// <summary>Test hooks.</summary>
        public static void ResetAll()
        {
            try { PlayerPrefs.DeleteKey(VolKey); PlayerPrefs.DeleteKey(EngineKey); PlayerPrefs.DeleteKey(WorldKey); PlayerPrefs.DeleteKey(BreakKey); } catch { }
            loaded = false;
        }
        public static void ForgetCache() { loaded = false; }
    }
}
