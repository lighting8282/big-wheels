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
            public Wave W; public float F0, F1, Dur, Vol, Delay, Vib;
            public Voice(Wave w, float f0, float f1, float dur, float vol, float delay = 0, float vib = 0) { W = w; F0 = f0; F1 = f1 <= 0 ? f0 : f1; Dur = dur; Vol = vol; Delay = delay; Vib = vib; }
        }

        const int SR = 44100;
        static readonly float[] Notes = { 523, 587, 659, 784, 880, 1047, 1175, 1319 };
        static AudioSource src, voice, engine;
        static readonly Dictionary<string, AudioClip> clips = new();
        static AudioClip[] pops, blips;
        static bool muted;
        static float lastSay;

        public static bool Muted { get => muted; set { muted = value; ApplyVolume(); if (value && voice) voice.Stop(); } }
        public static void ApplyVolume() { AudioListener.volume = muted ? 0f : Settings.Volume; }

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
            clips["beep"] = Compose("beep", new Voice(Wave.Square, 880, 0, .07f, .08f), new Voice(Wave.Square, 880, 0, .07f, .08f, .12f));
            clips["lowhonk"] = Compose("lowhonk", new Voice(Wave.Square, 130, 0, .38f, .12f), new Voice(Wave.Square, 164, 0, .38f, .1f));
            clips["siren"] = Compose("siren", new Voice(Wave.Tri, 650, 0, .28f, .16f), new Voice(Wave.Tri, 880, 0, .28f, .16f, .28f), new Voice(Wave.Tri, 650, 0, .28f, .16f, .56f));
            clips["spray"] = Compose("spray", new Voice(Wave.Noise, 3500, 1200, .6f, .22f));
            clips["munch"] = Compose("munch", new Voice(Wave.Noise, 900, 200, .15f, .3f), new Voice(Wave.Saw, 200, 80, .18f, .22f, .12f), new Voice(Wave.Noise, 900, 200, .15f, .3f, .3f));
            clips["splat"] = Compose("splat", new Voice(Wave.Noise, 1000, 200, .25f, .3f), new Voice(Wave.Sine, 180, 70, .2f, .3f));
            clips["tramp"] = Compose("tramp", new Voice(Wave.Sine, 200, 700, .3f, .3f), new Voice(Wave.Tri, 400, 1200, .25f, .12f, .05f));
            clips["moo"] = Compose("moo", new Voice(Wave.Saw, 150, 105, .8f, .2f, 0, .03f), new Voice(Wave.Tri, 75, 60, .8f, .18f));
            clips["baa"] = Compose("baa", new Voice(Wave.Saw, 330, 270, .7f, .14f, 0, .08f), new Voice(Wave.Tri, 165, 135, .7f, .1f, 0, .08f));
            clips["quack"] = Compose("quack", new Voice(Wave.Square, 520, 380, .09f, .12f), new Voice(Wave.Square, 500, 360, .09f, .12f, .12f));
            clips["squawk"] = Compose("squawk", new Voice(Wave.Saw, 900, 500, .25f, .12f, 0, .04f), new Voice(Wave.Saw, 800, 480, .22f, .12f, .24f, .04f));
            clips["crab"] = Compose("crab", new Voice(Wave.Noise, 4000, 2000, .04f, .35f), new Voice(Wave.Noise, 4000, 2000, .04f, .35f, .09f), new Voice(Wave.Noise, 4000, 2000, .04f, .35f, .18f));
            clips["peng"] = Compose("peng", new Voice(Wave.Square, 600, 950, .08f, .1f), new Voice(Wave.Square, 950, 520, .1f, .1f, .1f), new Voice(Wave.Square, 600, 950, .08f, .1f, .26f));
            clips["roar"] = Compose("roar", new Voice(Wave.Saw, 125, 85, .7f, .2f, 0, .05f), new Voice(Wave.Noise, 700, 200, .6f, .12f));
            clips["hoho"] = Compose("hoho", new Voice(Wave.Tri, 270, 200, .2f, .24f), new Voice(Wave.Tri, 270, 200, .2f, .24f, .28f));
            clips["tada"] = Compose("tada", new Voice(Wave.Tri, 523, 0, .12f, .22f), new Voice(Wave.Tri, 659, 0, .12f, .22f, .09f), new Voice(Wave.Tri, 784, 0, .12f, .22f, .18f),
                new Voice(Wave.Tri, 1047, 0, .5f, .25f, .27f), new Voice(Wave.Square, 1047, 0, .35f, .06f, .27f));
            clips["hoot"] = Compose("hoot", new Voice(Wave.Sine, 420, 340, .32f, .26f), new Voice(Wave.Sine, 420, 330, .4f, .26f, .42f), new Voice(Wave.Tri, 210, 170, .32f, .1f));
            clips["yip"] = Compose("yip", new Voice(Wave.Square, 700, 1150, .1f, .1f), new Voice(Wave.Square, 1150, 800, .13f, .1f, .14f), new Voice(Wave.Square, 700, 1150, .1f, .1f, .38f));
            clips["huff"] = Compose("huff", new Voice(Wave.Noise, 1600, 400, .22f, .35f), new Voice(Wave.Noise, 1600, 400, .22f, .35f, .3f));
            clips["crash"] = Compose("crash", new Voice(Wave.Noise, 2200, 250, .35f, .5f), new Voice(Wave.Noise, 900, 150, .3f, .4f, .04f), new Voice(Wave.Sine, 110, 50, .28f, .5f),
                new Voice(Wave.Noise, 3500, 1500, .05f, .3f, .12f), new Voice(Wave.Noise, 3500, 1500, .05f, .3f, .19f), new Voice(Wave.Noise, 3500, 1500, .05f, .25f, .27f));
            clips["crunch"] = Compose("crunch", new Voice(Wave.Noise, 1200, 200, .22f, .45f), new Voice(Wave.Noise, 2500, 300, .18f, .35f, .06f), new Voice(Wave.Square, 140, 60, .2f, .18f),
                new Voice(Wave.Noise, 3000, 1200, .05f, .3f, .15f), new Voice(Wave.Noise, 3000, 1200, .05f, .3f, .22f));
            clips["strike"] = Compose("strike", new Voice(Wave.Sine, 90, 50, .2f, .4f), new Voice(Wave.Tri, 1500, 900, .08f, .15f, .03f),
                new Voice(Wave.Noise, 4000, 1500, .05f, .4f), new Voice(Wave.Noise, 4000, 1500, .05f, .4f, .05f), new Voice(Wave.Noise, 4000, 1500, .05f, .35f, .11f),
                new Voice(Wave.Noise, 4000, 1500, .05f, .35f, .17f), new Voice(Wave.Noise, 4000, 1500, .05f, .3f, .26f), new Voice(Wave.Noise, 4000, 1500, .05f, .3f, .34f));
            clips["turbo"] = Compose("turbo", new Voice(Wave.Saw, 120, 520, .6f, .22f), new Voice(Wave.Square, 240, 1040, .5f, .1f, .05f), new Voice(Wave.Noise, 800, 4000, .5f, .25f),
                new Voice(Wave.Tri, 1047, 0, .15f, .2f, .5f), new Voice(Wave.Tri, 1319, 0, .2f, .2f, .6f));
            pops = new AudioClip[Notes.Length];
            for (int i = 0; i < pops.Length; i++)
                pops[i] = Compose("pop" + i, new Voice(Wave.Sine, Notes[i], 0, .3f, .3f), new Voice(Wave.Noise, 6000, 2000, .06f, .25f));
            blips = new AudioClip[4];
            for (int i = 0; i < blips.Length; i++) blips[i] = Compose("blip" + i, new Voice(Wave.Sine, 700 + i * 230, 0, .09f, .1f));

            engine.clip = EngineLoop();
            ApplyVolume();
        }

        public static void Play(string name, float vol = 1f) { if (!muted && src && clips.TryGetValue(name, out var c)) src.PlayOneShot(c, vol); }
        public static void Pop(int i) { if (!muted && src) src.PlayOneShot(pops[i % pops.Length]); }
        public static void Blip() { if (!muted && src) src.PlayOneShot(blips[UnityEngine.Random.Range(0, blips.Length)]); }

        public static void StartEngine() { if (engine && Settings.EngineOn && !engine.isPlaying) engine.Play(); }
        public static void SetEngineEnabled(bool on) { if (!engine) return; if (on) { if (!engine.isPlaying) engine.Play(); } else engine.Stop(); }
        public static void PauseEngine(bool p) { if (engine) engine.volume = p ? 0f : .05f; }
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
                    float f = v.F0 * Mathf.Pow(v.F1 / v.F0, k) * (1f + v.Vib * Mathf.Sin(t * 6.5f * Mathf.PI * 2f));
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
