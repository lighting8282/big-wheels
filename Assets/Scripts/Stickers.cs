using UnityEngine;

namespace BigWheels
{
    /// <summary>
    /// The sticker book: 25 discoveries, saved between sessions in PlayerPrefs as a bit mask.
    /// 0-3 trucks, 4-11 animals (tap them), 12-14 worlds, 15-24 things to try.
    /// </summary>
    public static class Stickers
    {
        public const int Count = 31;   // keep this at 31 or fewer: the mask is an int and a negative mask means "not loaded"
        const string MaskKey = "bw_stickers", SeenKey = "bw_seen";
        static int mask = -1, seen = -1;

        public class Def { public Sprite Icon; public float Rot; public Color Bg; public string Sfx; }
        static Def[] defs;

        static void Load()
        {
            if (mask >= 0) return;
            try { mask = PlayerPrefs.GetInt(MaskKey, 0); seen = PlayerPrefs.GetInt(SeenKey, 0); } catch { mask = 0; seen = 0; }
        }
        static void Save()
        {
            try { PlayerPrefs.SetInt(MaskKey, mask); PlayerPrefs.SetInt(SeenKey, seen); PlayerPrefs.Save(); } catch { }
        }

        public static bool Have(int id) { Load(); return ((mask >> id) & 1) == 1; }
        public static bool Add(int id) { Load(); if (Have(id)) return false; mask |= 1 << id; Save(); return true; }
        public static int Unseen { get { Load(); return mask & ~seen; } }
        public static void MarkSeen() { Load(); seen = mask; Save(); }
        public static int Collected { get { Load(); int n = 0; for (int i = 0; i < Count; i++) if (((mask >> i) & 1) == 1) n++; return n; } }

        /// <summary>Test hooks.</summary>
        public static void ResetAll() { try { PlayerPrefs.DeleteKey(MaskKey); PlayerPrefs.DeleteKey(SeenKey); } catch { } mask = 0; seen = 0; }
        public static void ForgetCache() { mask = -1; seen = -1; }

        public static Def Get(int id)
        {
            if (defs == null) Build();
            return defs[id];
        }

        static void Build()
        {
            defs = new Def[Count];
            Color truckBg = Palette.Hex("#8a7a4a"), animalBg = Palette.Hex("#5a6a54"), featBg = Palette.Hex("#7a5a5a");
            var truckIdx = new[] { 0, 2, 0, 3 };
            var truckSfx = new[] { "honk", "beep", "siren", "lowhonk" };
            for (int i = 0; i < 4; i++) defs[i] = new Def { Icon = Trucks.All[i].Body[truckIdx[i]], Bg = truckBg, Sfx = truckSfx[i] };
            var animalSfx = Game.AnimalSfx;
            for (int k = 0; k < 8; k++) defs[4 + k] = new Def { Icon = Art2.Animal[k], Bg = animalBg, Sfx = animalSfx[k] };
            defs[12] = new Def { Icon = Art3.Barn, Bg = Palette.Hex("#6a6a4a"), Sfx = "swoosh" };
            defs[13] = new Def { Icon = Art3.Wave, Bg = Palette.Hex("#9a7a4a"), Sfx = "swoosh" };
            defs[14] = new Def { Icon = Art3.Flake, Bg = Palette.Hex("#5a6a82"), Sfx = "swoosh" };
            defs[15] = new Def { Icon = Trucks.All[0].Body[4], Rot = 180f, Bg = featBg, Sfx = "whee" };
            defs[16] = new Def { Icon = Art.Balloon[0], Bg = featBg, Sfx = "pop" };
            defs[17] = new Def { Icon = Art.Car[1], Bg = featBg, Sfx = "fanfare" };
            defs[18] = new Def { Icon = Art2.Jelly[0], Bg = featBg, Sfx = "splat" };
            defs[19] = new Def { Icon = Art2.StreamWater, Bg = featBg, Sfx = "splash" };
            defs[20] = new Def { Icon = Art2.Trampoline, Bg = featBg, Sfx = "tramp" };
            defs[21] = new Def { Icon = Art.WashBack, Bg = featBg, Sfx = "sparkle" };
            defs[22] = new Def { Icon = Art.Puddle, Bg = featBg, Sfx = "splash" };
            defs[23] = new Def { Icon = Art2.Hat, Bg = featBg, Sfx = "sparkle" };
            defs[24] = new Def { Icon = Art.SunNormal, Bg = featBg, Sfx = "sparkle" };
            defs[25] = new Def { Icon = Art3.MoonSky, Bg = Palette.Hex("#4a4a7a"), Sfx = "swoosh" };
            for (int k = 0; k < 3; k++) defs[26 + k] = new Def { Icon = Art2.Animal[8 + k], Bg = animalBg, Sfx = animalSfx[8 + k] };
            SmashArt.Build();
            defs[29] = new Def { Icon = SmashArt.Wall, Bg = featBg, Sfx = "crunch" };
            defs[30] = new Def { Icon = SmashArt.Flame, Bg = featBg, Sfx = "turbo" };
        }
    }

    /// <summary>Small icons used only by the sticker book.</summary>
    public static class Art3
    {
        public static Sprite IconJump, IconPause, IconGas, IconBrake, IconPaint, IconWheel, IconGear, IconBook, IconClose, Barn, Wave, Flake, Card, Moon, SleepEye, MoonSky, MoonSkyHappy, Beam;
        static bool built;
        static readonly Color T = new Color(0, 0, 0, 0);

        static void DrawMoon(Painter p, bool happy)
        {
            var ink = Palette.Ink;
            p.Fill(Shapes.Circle(0, 0, 48), Palette.Hex("#fff2b0"), 5);
            var crater = Palette.Hex("#efdc8c");
            p.Fill(Shapes.Circle(-26, -20, 8), crater); p.Fill(Shapes.Circle(24, -26, 6), crater); p.Fill(Shapes.Circle(28, 22, 9), crater);
            if (happy) p.Fill(Shapes.Circle(0, 0, 36), Palette.Hex("#fffbe0"));   // brighter when tapped; no face
            p.Fill(Shapes.Ellipse(-14, -17, 13, 8), new Color(1, 1, 1, .5f));
        }

        public static void Build()
        {
            if (built) return; built = true;
            var ink = Palette.Ink;

            // settings gear: a toothed disc with a hole
            IconGear = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>
            {
                var shapes = new Shape[9];
                shapes[0] = Shapes.Circle(0, 0, 19);
                for (int i = 0; i < 8; i++) shapes[i + 1] = Shapes.RRect(-6, -29, 12, 14, 2.5f, i * 45f);
                p.Fill(Shapes.Union(shapes), Palette.Hex("#aab3c8"), 3.4f);
                p.Fill(Shapes.Circle(0, 0, 8), Palette.Hex("#2b303a"), 3.4f);
            });
            IconJump = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>                       // jump: an up arrow over a spring
            {
                p.Fill(Shapes.Poly(0, -26, 20, -4, 8, -4, 8, 8, -8, 8, -8, -4, -20, -4), Palette.Hex("#ffb02e"), 3);
                for (int k = 0; k < 3; k++) p.Fill(Shapes.Capsule(-14, 14 + k * 6, 14, 14 + k * 6, 1.8f), Palette.Hex("#e9edf5"));
            });
            IconPause = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-17, -20, 12, 40, 3), Palette.Hex("#e9edf5"), 3); p.Fill(Shapes.RRect(5, -20, 12, 40, 3), Palette.Hex("#e9edf5"), 3);
            });
            IconGas = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>                       // gas: two chevrons pointing forward
            {
                p.Fill(Shapes.Poly(-22, 4, 0, -18, 22, 4, 22, 14, 0, -8, -22, 14), Palette.Hex("#38c172"), 3);
                p.Fill(Shapes.Poly(-22, 18, 0, -4, 22, 18, 22, 28, 0, 6, -22, 28), Palette.Hex("#38c172"), 3);
            });
            IconBrake = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>                     // brake: a stop sign
            {
                p.Fill(Shapes.Poly(-10, -22, 10, -22, 22, -10, 22, 10, 10, 22, -10, 22, -22, 10, -22, -10), Palette.Hex("#d33a2c"), 3);
                p.Fill(Shapes.RRect(-13, -4, 26, 8, 2), Color.white);
            });
            IconPaint = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>                     // customise: a paint palette
            {
                p.Fill(Shapes.Ellipse(0, 2, 26, 21), Palette.Hex("#e9edf5"), 3);
                p.Fill(Shapes.Circle(11, 12, 6), Palette.Hex("#2b303a"), 2.4f);
                p.Fill(Shapes.Circle(-12, -5, 5), Palette.Hex("#d33a2c")); p.Fill(Shapes.Circle(-2, -13, 5), Palette.Hex("#ffc933")); p.Fill(Shapes.Circle(10, -9, 5), Palette.Hex("#38c172")); p.Fill(Shapes.Circle(-14, 8, 5), Palette.Hex("#3d8bff"));
            });
            // steering wheel: a ring with three spokes
            IconWheel = Art.Make(-32, -32, 32, 32, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(-24, 4, 24, 4, 4.4f), Palette.Hex("#aab3c8"), 3);
                p.Fill(Shapes.Capsule(0, 4, 0, 26, 4.4f), Palette.Hex("#aab3c8"), 3);
                p.Fill(Shapes.Circle(0, 0, 26), new Color(0, 0, 0, 0), 6.5f, Palette.Hex("#aab3c8"));
                p.Fill(Shapes.Circle(0, 4, 7), Palette.Hex("#2b303a"), 3, Palette.Hex("#aab3c8"));
                p.Fill(Shapes.RRect(-4, -30, 8, 8, 2), Palette.Hex("#d33a2c"));
            });
            IconBook = Art.Make(-32, -28, 32, 28, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-27, -23, 54, 46, 7), Palette.Truck[0], 4);
                p.Fill(Shapes.Capsule(-22, -19, -22, 19, 3), Palette.Shade(Palette.Truck[0], -.3f));
                p.Fill(Shapes.Star(3, 0, 13), Palette.Sun, 2.5f);
            });
            var pc = new Painter(-50, -50, 50, 50, Vector2.zero);
            pc.Fill(Shapes.RRect(-47, -47, 94, 94, 14), Color.white, 3);
            Card = pc.ToSprite(new Vector4(34, 34, 34, 34));
            Moon = Art.Make(-60, -60, 60, 60, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 52), Palette.Hex("#fff2b0"), 5);
                p.Fill(Shapes.Arc(-18, -4, 9, .15f * Mathf.PI, .85f * Mathf.PI, 2.6f), ink);
                p.Fill(Shapes.Arc(18, -4, 9, .15f * Mathf.PI, .85f * Mathf.PI, 2.6f), ink);
                p.Fill(Shapes.Arc(0, 16, 7, .15f * Mathf.PI, .85f * Mathf.PI, 2.2f), ink);
                p.Fill(Shapes.Circle(-30, 12, 7), new Color(1, .5f, .55f, .45f)); p.Fill(Shapes.Circle(30, 12, 7), new Color(1, .5f, .55f, .45f));
            });
            MoonSky = Art.Make(-54, -54, 54, 54, Vector2.zero, p => DrawMoon(p, false));
            MoonSkyHappy = Art.Make(-54, -54, 54, 54, Vector2.zero, p => DrawMoon(p, true));
            Beam = Art.Make(0, -70, 300, 70, new Vector2(0, 0), p =>
                p.SetPixels((x, y) =>
                {
                    float lx = (x + .5f) / Painter.Scale, ly = p.Y1 - (y + .5f) / Painter.Scale;
                    float halfH = 8f + lx * .22f, d = Mathf.Abs(ly) - halfH;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - lx / 300f), 1.3f) * .42f * Mathf.Clamp01(-d / 12f);
                    return new Color(1f, .95f, .6f, a);
                }));
            SleepEye = Art.Make(-22, -14, 22, 14, Vector2.zero, p => p.Fill(Shapes.Arc(0, -5, 13, .15f * Mathf.PI, .85f * Mathf.PI, 2.8f), ink));
            IconClose = Art.Make(-22, -22, 22, 22, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(-12, -12, 12, 12, 4.5f), ink);
                p.Fill(Shapes.Capsule(12, -12, -12, 12, 4.5f), ink);
            });
            Barn = Art.Make(-36, -36, 36, 34, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-26, -8, 52, 38, 2), Palette.Hex("#ff4b4b"), 4);
                p.Fill(Shapes.Poly(-31, -6, 0, -31, 31, -6), Palette.Hex("#b03a3a"), 4);
                p.Fill(Shapes.RRect(-10, 6, 20, 24, 1), Color.white, 3);
                p.Fill(Shapes.Capsule(-10, 6, 10, 30, 1.6f), ink); p.Fill(Shapes.Capsule(10, 6, -10, 30, 1.6f), ink);
                p.Fill(Shapes.Circle(0, -14, 4.5f), Color.white, 2.5f);
            });
            Wave = Art.Make(-36, -34, 36, 34, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(14, -14, 12), Palette.Sun, 3);
                p.Fill(Shapes.RRect(-31, 2, 62, 28, 12), Palette.Hex("#42b9ea"), 4);
                p.Fill(Shapes.Arc(-12, 14, 9, -Mathf.PI, 0, 2f), Color.white);
                p.Fill(Shapes.Arc(10, 14, 9, -Mathf.PI, 0, 2f), Color.white);
            });
            Flake = Art.Make(-34, -34, 34, 34, Vector2.zero, p =>
            {
                var blue = Palette.Hex("#8ecbff");
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.PI / 3f, dx = Mathf.Cos(a) * 26, dy = Mathf.Sin(a) * 26;
                    p.Fill(Shapes.Capsule(-dx, -dy, dx, dy, 3f), blue, 2f);
                }
                p.Fill(Shapes.Circle(0, 0, 6), Color.white, 2.5f);
            });
        }
    }
}
