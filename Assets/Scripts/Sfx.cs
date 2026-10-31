using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWheels
{
    /// <summary>Procedurally synthesised sound effects (no audio files needed) plus an optional voice-clip hook.</summary>
    public static class Sfx
    {
        enum Wave { Sine, Square, Tri, Saw, Noise }
        struct Voice
        {
            public Wave W; public float F0, F1, Dur, Vol, Delay;
            public Voice(Wave w, float f0, float f1, float dur, float vol, float delay = 0) { W = w; F0 = f0; F1 = f1 <= 0 ? f0 : f1; Dur = dur; Vol = vol; Delay = delay; }
        }

        const int SR = 44100;
        static readonly float[] Notes = { 523, 587, 659, 784, 880, 1047, 1175, 1319 };
        static AudioSource src, voice, engine;
        static readonly Dictionary<string, AudioClip> clips = new();
        static AudioClip[] pops, blips;
        static bool muted;
        static float lastSay;

        public static bool Muted { get => muted; set { muted = value; AudioListener.volume = value ? 0f : 1f; if (value && voice) voice.Stop(); } }

        public static void Init(GameObject host)
        {
            if (src != null) return;
            src = host.AddComponent<AudioSource>(); src.playOnAwake = false;
            voice = host.AddComponent<AudioSource>(); voice.playOnAwake = false;
            engine = host.AddComponent<AudioSource>(); engine.playOnAwake = false; engine.loop = true; engine.volume = .05f;

            clips["honk"] = Compose("honk", new Voice(Wave.Square, 233, 0, .3f, .09f), new Voice(Wave.Square, 294, 0, .3f, .09f));
            clips["boing"] = Compose("boing", new Voice(Wave.Sine, 220, 560, .26f, .28f));
            clips["squash"] = Compose("squash", new Voice(Wave.Noise, 1800, 160, .24f, .45f), new Voice(Wave.Tri, 320, 70, .26f, .3f));
            clips["splash"] = Compose("splash", new Voice(Wave.Noise, 1400, 250, .4f, .3f));
            clips["thump"] = Compose("thump", new Voice(Wave.Sine, 95, 48, .22f, .45f));
            clips["whee"] = Compose("whee", new Voice(Wave.Sine, 300, 900, .45f, .22f), new Voice(Wave.Sine, 900, 340, .45f, .22f, .45f));
            clips["swoosh"] = Compose("swoosh", new Voice(Wave.Noise, 5000, 400, .3f, .22f));
            clips["sparkle"] = Compose("sparkle", new Voice(Wave.Tri, 880, 0, .12f, .14f), new Voice(Wave.Tri, 1100, 0, .12f, .14f, .05f), new Voice(Wave.Tri, 1320, 0, .12f, .14f, .1f));
            clips["fanfare"] = Compose("fanfare",
                new Voice(Wave.Tri, 523, 0, .32f, .25f), new Voice(Wave.Tri, 659, 0, .32f, .25f, .13f),
                new Voice(Wave.Tri, 784, 0, .32f, .25f, .26f), new Voice(Wave.Tri, 1047, 0, .32f, .25f, .39f),
                new Voice(Wave.Square, 1047, 0, .7f, .08f, .55f), new Voice(Wave.Square, 784, 0, .7f, .08f, .55f));
            pops = new AudioClip[Notes.Length];
            for (int i = 0; i < pops.Length; i++)
                pops[i] = Compose("pop" + i, new Voice(Wave.Sine, Notes[i], 0, .3f, .3f), new Voice(Wave.Noise, 6000, 2000, .06f, .25f));
            blips = new AudioClip[4];
            for (int i = 0; i < blips.Length; i++) blips[i] = Compose("blip" + i, new Voice(Wave.Sine, 700 + i * 230, 0, .09f, .1f));

            engine.clip = EngineLoop();
        }

        public static void Play(string name) { if (!muted && src && clips.TryGetValue(name, out var c)) src.PlayOneShot(c); }
        public static void Pop(int i) { if (!muted && src) src.PlayOneShot(pops[i % pops.Length]); }
        public static void Blip() { if (!muted && src) src.PlayOneShot(blips[UnityEngine.Random.Range(0, blips.Length)]); }

        public static void StartEngine() { if (engine && !engine.isPlaying) engine.Play(); }
        public static void SetEngine(float freq) { if (engine) engine.pitch = freq / 48f; }

        /// <summary>Plays Resources/Voice/&lt;word&gt;.* if you add recordings; silent otherwise.</summary>
        public static void Say(string word, bool priority)
        {
            if (muted || !voice) return;
            float now = Time.realtimeSinceStartup;
            if (!priority && now - lastSay < .9f) return;
            var c = Resources.Load<AudioClip>("Voice/" + word.ToLowerInvariant());
            if (!c) return;
            lastSay = now; voice.Stop(); voice.PlayOneShot(c);
        }

        static AudioClip Compose(string name, params Voice[] voices)
        {
            float total = 0f;
            foreach (var v in voices) total = Mathf.Max(total, v.Delay + v.Dur);
            int n = Mathf.CeilToInt((total + .05f) * SR);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            foreach (var v in voices)
            {
                int s0 = Mathf.RoundToInt(v.Delay * SR), len = Mathf.RoundToInt(v.Dur * SR);
                double phase = 0; float lp = 0;
                float decay = Mathf.Log(Mathf.Max(1.0001f, v.Vol / .0001f));
                for (int i = 0; i < len && s0 + i < n; i++)
                {
                    float t = i / (float)SR, k = t / v.Dur;
                    float f = v.F0 * Mathf.Pow(v.F1 / v.F0, k);
                    float env = t < .012f ? v.Vol * (t / .012f) : v.Vol * Mathf.Exp(-decay * (t - .012f) / (v.Dur - .012f));
                    float s;
                    if (v.W == Wave.Noise)
                    {
                        float a = 1f - Mathf.Exp(-2f * Mathf.PI * f / SR);
                        lp += a * ((float)(rng.NextDouble() * 2 - 1) - lp);
                        s = lp * 2.2f;
                    }
                    else
                    {
                        phase += f / SR; float ph = (float)(phase - Math.Floor(phase));
                        s = v.W switch
                        {
                            Wave.Sine => Mathf.Sin(ph * Mathf.PI * 2),
                            Wave.Square => ph < .5f ? 1f : -1f,
                            Wave.Tri => 4f * Mathf.Abs(ph - .5f) - 1f,
                            _ => 2f * ph - 1f
                        };
                    }
                    data[s0 + i] += s * env;
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip EngineLoop()
        {
            int n = SR;                         // exactly 48 cycles of 48 Hz -> seamless loop
            var data = new float[n]; float lp = 0, a = 1f - Mathf.Exp(-2f * Mathf.PI * 200f / SR);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    float saw = 2f * ((i * 48f / SR) % 1f) - 1f;
                    lp += a * (saw - lp);
                    data[i] = lp * 2f;
                }
            var clip = AudioClip.Create("engine", n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
