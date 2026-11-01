using UnityEngine;

namespace BigWheels
{
    /// <summary>
    /// The sticker book: 25 discoveries, saved between sessions in PlayerPrefs as a bit mask.
    /// 0-3 trucks, 4-11 animals (tap them), 12-14 worlds, 15-24 things to try.
    /// </summary>
    public static class Stickers
    {
        public const int Count = 25;
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
            Color truckBg = Palette.Hex("#fff0b3"), animalBg = Palette.Hex("#c9f2d4"), featBg = Palette.Hex("#ffd6e8");
            var truckIdx = new[] { 0, 2, 0, 3 };
            var truckSfx = new[] { "honk", "beep", "siren", "lowhonk" };
            for (int i = 0; i < 4; i++) defs[i] = new Def { Icon = Trucks.All[i].Body[truckIdx[i]], Bg = truckBg, Sfx = truckSfx[i] };
            var animalSfx = new[] { "moo", "baa", "quack", "crab", "squawk", "peng", "roar", "hoho" };
            for (int k = 0; k < 8; k++) defs[4 + k] = new Def { Icon = Art2.Animal[k], Bg = animalBg, Sfx = animalSfx[k] };
            defs[12] = new Def { Icon = Art3.Barn, Bg = Palette.Hex("#d3f5bd"), Sfx = "swoosh" };
            defs[13] = new Def { Icon = Art3.Wave, Bg = Palette.Hex("#ffeab0"), Sfx = "swoosh" };
            defs[14] = new Def { Icon = Art3.Flake, Bg = Palette.Hex("#d9edff"), Sfx = "swoosh" };
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
        }
    }

    /// <summary>Small icons used only by the sticker book.</summary>
    public static class Art3
    {
        public static Sprite IconBook, IconClose, Barn, Wave, Flake, Card, Moon, SleepEye;
        static bool built;
        static readonly Color T = new Color(0, 0, 0, 0);

        public static void Build()
        {
            if (built) return; built = true;
            var ink = Palette.Ink;

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
