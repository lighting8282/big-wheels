using UnityEngine;

namespace BigWheels
{
    // Score, combo multiplier and the saved best score.
    public partial class Game
    {
        public int Score { get; private set; }
        public int Best => Settings.Best;
        public int Combo => combo;
        /// <summary>Current multiplier: 1 at the start of a streak, +1 for every two hits in a row, up to 5. Turbo doubles it again.</summary>
        public int Mult => Mathf.Min(5, 1 + combo / 2) * (turboT > 0 ? 2 : 1);
        public float ScorePulse { get; private set; }
        int combo; float comboClock;

        /// <summary>Adds points (times the multiplier) and floats the amount up from the spot in the world where it happened.</summary>
        void AddScore(int pts, float wx, float screenY)
        {
            if (!Started || pts <= 0) return;
            combo++; comboClock = 3f;
            int m = Mult, gain = pts * m;
            Score += gain; ScorePulse = 1f;
            if (Score > Settings.Best) Settings.Best = Score;
            hud.Popup("+" + gain + (m > 1 ? "  x" + m : ""), (wx - worldX) / Wl, 1f - screenY / Hl, m);
        }

        void StepScore(float dt)
        {
            if (ScorePulse > 0) ScorePulse = Mathf.Max(0, ScorePulse - dt * 3f);
            if (comboClock > 0)
            {
                comboClock -= dt;
                if (comboClock <= 0) { combo = 0; Settings.FlushBest(); }
            }
        }

        /// <summary>Parent corner: clears the score and the saved best.</summary>
        public void ResetScores() { Score = 0; combo = 0; comboClock = 0; Settings.Best = 0; Settings.FlushBest(); }

        void OnApplicationQuit() { Settings.FlushBest(); }
        void OnApplicationPause(bool paused) { if (paused) Settings.FlushBest(); }
    }
}
