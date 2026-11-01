using UnityEngine;

namespace BigWheels
{
    /// <summary>
    /// The rough-and-tough look: dusk skies, factory and mesa skylines, smoke, steel buttons, weathered vehicles, scrap collectibles and
    /// mean animals. Runs once after the base art is built and swaps the sprites in place, so nothing else has to know.
    /// </summary>
    public static class Gritty
    {
        static bool applied;
        static readonly Color Clear = new Color(0, 0, 0, 0);
        static Color X(string h) => Palette.Hex(h);
        static float Hh(int s, int k) { float v = Mathf.Sin((s * 91.7f + k * 37.3f + 11.1f) * 12.9898f) * 43758.5453f; return v - Mathf.Floor(v); }

        static Color Grad(float t, (float at, Color c)[] stops)
        {
            if (t <= stops[0].at) return stops[0].c;
            for (int i = 1; i < stops.Length; i++)
                if (t <= stops[i].at) return Color.Lerp(stops[i - 1].c, stops[i].c, (t - stops[i - 1].at) / (stops[i].at - stops[i - 1].at));
            return stops[stops.Length - 1].c;
        }
        static Sprite SkyGradient(params (float at, Color c)[] stops)
        {
            var s = Art.Make(0, 0, 4, 64, Vector2.zero, p => p.SetPixels((x, y) => Grad((y + .5f) / p.H, stops)));
            s.texture.wrapMode = TextureWrapMode.Clamp;
            return s;
        }

        public static void Apply()
        {
            if (applied) return; applied = true;
            Worlds(); Ui(); Things(); Animals(); Costumes(); Icons();
        }

        // ================= skies, skylines, sun, smoke =================
        static void Worlds()
        {
            Art.Sky = SkyGradient((0f, X("#ff8a3d")), (.4f, X("#d9505f")), (.78f, X("#5a3a7a")), (1f, X("#1d1c48")));
            Art2.SkyBeach = SkyGradient((0f, X("#ffe0a0")), (.45f, X("#ff9a58")), (1f, X("#7a4a8a")));
            Art2.SkySnow = SkyGradient((0f, X("#cfd8e2")), (.5f, X("#8798ad")), (1f, X("#38435c")));
            Art2.SkyNight = SkyGradient((0f, X("#3a2a5a")), (.5f, X("#141a3c")), (1f, X("#05071a")));

            float far = Art.HillFarPeriod, near = Art.HillNearPeriod;
            Art.HillFar = Skyline(far, 190, 1, X("#6b4768"), X("#ffb35c"), 0);
            Art.HillNear = Skyline(near, 100, 2, X("#2a2030"), X("#ff9a3c"), 0);
            Art2.HillSea = Skyline(far, 190, 3, X("#a4503e"), X("#ffd08a"), 1);
            Art2.HillDunes = Art.MakeHill(near, Art.HillNearWl, 55f, 12f, X("#8f4f2a"));
            Art2.HillMountains = Art.MakeRidge(far, Art.HillFarWl, 150f, 56f, X("#4c5875"));
            Art2.HillSnow = Art.MakeHill(near, Art.HillNearWl, 70f, 12f, X("#a9b8ca"));
            Art2.HillNightFar = Skyline(far, 190, 4, X("#1d1f4c"), X("#fff2a8"), 2);
            Art2.HillNightNear = Skyline(near, 100, 5, X("#10122e"), X("#ffd86a"), 2);

            // a low red sun, banded like a sunset behind haze, no face and no rays
            Art.SunNormal = Art.Make(-54, -54, 54, 54, Vector2.zero, p => DrawSun(p, false));
            Art.SunHappy = Art.Make(-54, -54, 54, 54, Vector2.zero, p => DrawSun(p, true));

            // smoke instead of fluffy clouds
            Art.Cloud = Art.Make(-60, -52, 150, 36, Vector2.zero, p =>
            {
                float s = 60;
                var u = Shapes.Union(Shapes.Circle(0, 0, s * .6f), Shapes.Circle(s * .6f, -s * .25f, s * .7f), Shapes.Circle(s * 1.3f, 0, s * .55f),
                    Shapes.RRect(-s * .5f, 0, s * 2.3f, s * .5f, s * .25f));
                p.Fill(u, new Color(.36f, .31f, .36f, .5f));
                p.Fill(Shapes.Circle(s * .55f, -s * .2f, s * .45f), new Color(.5f, .44f, .48f, .35f));
            });
        }

        static void DrawSun(Painter p, bool bright)
        {
            var disc = Shapes.Circle(0, 0, 48);
            p.Fill(disc, X("#ff6a2a"), 0);
            p.Fill(Shapes.Circle(0, 0, 38), X("#ff9a3a"), 0, null, disc);
            p.Fill(Shapes.Circle(0, 0, 26), bright ? X("#fff0a0") : X("#ffd069"), 0, null, disc);
            for (int i = 0; i < 4; i++) p.Fill(Shapes.RRect(-50, 8 + i * 10, 100, 3 + i * 1.5f, 0), new Color(.45f, .12f, .2f, .75f), 0, null, disc);
            p.Fill(disc, Clear, 3, X("#7a2418"));
        }

        /// <summary>A silhouette strip that repeats seamlessly every `period` px: factories, cranes and scrap (0), flat mesas (1), or floodlit stands (2).</summary>
        static Sprite Skyline(float period, float height, int seed, Color col, Color lit, int style)
        {
            return Art.Make(0, -height, period, 0, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-10, -height * .16f, period + 20, height * .16f + 10, 0), col);
                int n = 0;
                for (float x = Hh(seed, 0) * 60f; x < period - 40f; n++)
                {
                    float r = Hh(seed, n + 1), w;
                    foreach (float off in new[] { -period, 0f, period })
                    {
                        float ox = x + off;
                        if (style == 0) DrawJunkyard(p, ox, height, r, seed * 100 + n, col, lit);
                        else if (style == 1) DrawMesa(p, ox, height, r, seed * 100 + n, col, lit);
                        else DrawArena(p, ox, height, r, seed * 100 + n, col, lit);
                    }
                    w = style == 1 ? 160f + 120f * r : style == 2 ? 130f + 90f * r : 90f + 150f * r;
                    x += w;
                }
            });
        }

        static void DrawJunkyard(Painter p, float x, float H, float r, int s, Color col, Color lit)
        {
            if (r < .32f)                                   // factory with chimneys and lit windows
            {
                float w = 70 + 50 * Hh(s, 2), h = H * (.35f + .35f * Hh(s, 3));
                p.Fill(Shapes.RRect(x, -h, w, h, 2), col);
                p.Fill(Shapes.RRect(x + 8, -h - 38, 11, 40, 1), col); p.Fill(Shapes.RRect(x + w - 24, -h - 54, 13, 56, 1), col);
                for (int k = 0; k < 4; k++) p.Fill(Shapes.RRect(x + 8 + k * (w - 20) / 4f, -h * .55f, 8, 7, 1), lit);
            }
            else if (r < .52f)                              // crane
            {
                float h = H * .93f;
                p.Fill(Shapes.RRect(x, -h, 7, h, 1), col);
                p.Fill(Shapes.RRect(x - 24, -h, 110, 6, 1), col);
                p.Fill(Shapes.Capsule(x + 3, -h, x + 76, -h + 2, 1.6f), col);
                p.Fill(Shapes.Capsule(x + 80, -h + 4, x + 80, -h + 46, 1.2f), col);
                p.Fill(Shapes.Circle(x + 80, -h + 50, 4), col);
                p.Fill(Shapes.RRect(x - 14, -h - 2, 12, 14, 1), col);
                for (int k = 0; k < 6; k++) p.Fill(Shapes.Capsule(x, -h * (k / 6f), x + 7, -h * ((k + 1) / 6f), 1.2f), col);
            }
            else if (r < .84f)                              // scrap heap with sticking-out junk
            {
                float w = 100 + 90 * Hh(s, 4), h = H * (.2f + .2f * Hh(s, 5));
                p.Fill(Shapes.Poly(x, 0, x + w * .15f, -h * .6f, x + w * .3f, -h * .9f, x + w * .45f, -h * .7f, x + w * .6f, -h, x + w * .8f, -h * .55f, x + w, 0), col);
                p.Fill(Shapes.Capsule(x + w * .42f, -h * .8f, x + w * .46f, -h - 16, 1.8f), col);
                p.Fill(Shapes.Capsule(x + w * .7f, -h * .7f, x + w * .78f, -h - 10, 1.6f), col);
                p.Fill(Shapes.Circle(x + w * .3f, -h * .95f, 7), col);
            }
            else                                            // storage tank
            {
                float w = 56, h = H * .3f;
                p.Fill(Shapes.RRect(x, -h, w, h, 3), col); p.Fill(Shapes.Ellipse(x + w / 2, -h, w / 2, 12), col);
                p.Fill(Shapes.RRect(x + 6, -h * .6f, w - 12, 5, 1), lit);
            }
        }

        static void DrawMesa(Painter p, float x, float H, float r, int s, Color col, Color lit)
        {
            float w = 150 + 100 * r, h = H * (.45f + .5f * Hh(s, 2));
            p.Fill(Shapes.Poly(x, 0, x + w * .12f, -h * .85f, x + w * .2f, -h, x + w * .8f, -h, x + w * .9f, -h * .8f, x + w, 0), col);
            for (int k = 1; k < 4; k++)
                p.Fill(Shapes.Capsule(x + w * .1f, -h * k / 4.3f, x + w * .9f, -h * k / 4.3f, 1.4f), new Color(lit.r, lit.g, lit.b, .22f));
        }

        static void DrawArena(Painter p, float x, float H, float r, int s, Color col, Color lit)
        {
            if (r < .5f)                                    // floodlight tower
            {
                float h = H * .92f;
                p.Fill(Shapes.RRect(x, -h, 6, h, 1), col);
                p.Fill(Shapes.RRect(x - 22, -h - 4, 50, 16, 2), col);
                for (int k = 0; k < 5; k++) p.Fill(Shapes.Circle(x - 15 + k * 9, -h + 4, 3), lit);
            }
            else                                            // grandstand with a lit crowd
            {
                float w = 150 + 80 * Hh(s, 2), h = H * .34f;
                p.Fill(Shapes.Poly(x, 0, x + 14, -h, x + w - 14, -h, x + w, 0), col);
                for (int k = 0; k < 14; k++) p.Fill(Shapes.Circle(x + 22 + k * (w - 44) / 13f, -h * (.4f + .35f * Hh(s, 10 + k)), 2.2f), new Color(lit.r, lit.g, lit.b, .7f));
            }
        }

        // ================= steel buttons =================
        static void Ui()
        {
            Color steel = X("#2b303a"), edge = X("#0f1116"), rivet = X("#8d97aa");
            Sprite Disc(Color rim) => Art.Make(-50, -50, 50, 50, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 46), steel, 5, edge);
                p.Fill(Shapes.Circle(0, 0, 38), Clear, 3, rim);
                p.Fill(Shapes.Arc(0, 0, 42, -2.7f, -1.2f, 2.2f), new Color(1, 1, 1, .22f));
                foreach (var (dx, dy) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) p.Fill(Shapes.Circle(dx * 29, dy * 29, 2.4f), rivet);
            });
            Art.UiSteel = Disc(X("#6a7384"));
            Art.UiSteelSel = Disc(X("#ff9a1f"));
            Art.UiSteelDim = Art.Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 46), new Color(.1f, .12f, .16f, .5f), 4, new Color(.55f, .6f, .7f, .8f)));

            for (int i = 0; i < Palette.Truck.Length; i++)
            {
                Color c = Palette.Truck[i];
                Art.SwatchNormal[i] = Art.Make(-50, -50, 50, 50, Vector2.zero, p =>
                {
                    p.Fill(Shapes.Circle(0, 0, 44), c, 10, steel);
                    p.Fill(Shapes.Arc(0, 0, 34, -2.7f, -1.3f, 3f), new Color(1, 1, 1, .3f));
                });
                Art.SwatchSelected[i] = Art.Make(-50, -50, 50, 50, Vector2.zero, p =>
                {
                    p.Fill(Shapes.Circle(0, 0, 44), c, 10, X("#ffb02e"));
                    p.Fill(Shapes.Arc(0, 0, 34, -2.7f, -1.3f, 3f), new Color(1, 1, 1, .3f));
                });
            }
        }

        // ================= vehicles, collectibles and obstacles =================
        static void Things()
        {
            var ink = Palette.Ink;
            // spinning steel cogs in the air, in the truck colours (they replace the balloons)
            for (int i = 0; i < Art.Balloon.Length; i++)
            {
                Color c = Palette.Truck[i];
                Art.Balloon[i] = Art.Make(-36, -36, 36, 36, Vector2.zero, p =>
                {
                    var teeth = new Shape[9];
                    teeth[0] = Shapes.Circle(0, 0, 22);
                    for (int k = 0; k < 8; k++) teeth[k + 1] = Shapes.RRect(-5.5f, -32, 11, 13, 2.5f, k * 45f);
                    p.Fill(Shapes.Union(teeth), c, 3.6f);
                    p.Fill(Shapes.Circle(0, 0, 13), X("#1d2028"), 3);
                    p.Fill(Shapes.Circle(0, 0, 5), X("#8d97aa"), 2);
                    p.Fill(Shapes.Arc(0, 0, 17, -2.6f, -1.3f, 2.4f), new Color(1, 1, 1, .35f));
                });
            }

            // sludge piles instead of jelly: dark, glossy, bubbling
            for (int i = 0; i < Art2.Jelly.Length; i++)
            {
                Color c = Palette.Truck[Art2.JellyPalette[i]];
                Art2.Jelly[i] = Art.Make(-80, -70, 80, 6, Vector2.zero, p =>
                {
                    var half = new Shape(q => q.y, new Rect(-80, -70, 160, 70));
                    p.Fill(Shapes.Ellipse(0, 0, 70, 58), new Color(c.r * .75f, c.g * .75f, c.b * .75f, .96f), 4, null, half);
                    p.Fill(Shapes.Ellipse(8, -6, 52, 38), Palette.Shade(c, -.1f), 0, null, half);
                    p.Fill(Shapes.Capsule(-70, 0, 70, 0, 2f), ink);
                    p.Fill(Shapes.Capsule(-40, -38, -12, -50, 2.2f), new Color(1, 1, 1, .3f));
                    foreach (var (bx, by, br) in new[] { (-34f, -22f, 6f), (22f, -40f, 4.5f), (42f, -20f, 5.5f), (-8f, -14f, 3.5f) })
                        p.Fill(Shapes.Circle(bx, by, br), Clear, 2f, new Color(1, 1, 1, .7f));
                });
            }

            // a launch pad with hazard stripes and coil springs (instead of the trampoline)
            Art2.Trampoline = Art.Make(-90, -66, 90, 4, Vector2.zero, p =>
            {
                Color spr = X("#c9cdd6");
                foreach (float sx in new[] { -62f, -21f, 21f, 62f })
                    for (int k = 0; k < 4; k++) p.Fill(Shapes.Capsule(sx - 8, -36 + k * 8, sx + 8, -32 + k * 8, 1.8f), spr);
                p.Fill(Shapes.RRect(-86, -8, 172, 10, 3), X("#2b303a"), 3);
                var plat = Shapes.RRect(-84, -58, 168, 24, 6);
                p.Fill(plat, X("#f0b400"), 0);
                for (int k = 0; k < 9; k++) p.Fill(Shapes.Poly(-92 + k * 22, -34, -80 + k * 22, -34, -64 + k * 22, -58, -76 + k * 22, -58), X("#1a1a22"), 0, null, plat);
                p.Fill(plat, Clear, 3.6f);
            });

            // stacks of old tyres instead of bowling pins, and a single tyre for the pieces
            SmashArt.Pins = Art.Make(-82, -100, 82, 4, Vector2.zero, p =>
            {
                int[] stack = { 3, 4, 2 }; float[] xs = { -50f, 0f, 50f };
                for (int s = 0; s < 3; s++)
                    for (int k = 0; k < stack[s]; k++) DrawTyre(p, xs[s], -k * 21 - 1);
                p.Grunge(47);
            });
            SmashArt.Pin = Art.Make(-26, -26, 26, 26, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 23), X("#26262c"), 3.4f);
                p.Fill(Shapes.Circle(0, 0, 15), Clear, 2.4f, X("#3a3a44"));
                p.Fill(Shapes.Circle(0, 0, 9), X("#8d939d"), 2.6f);
                p.Fill(Shapes.Circle(0, 0, 3), X("#3a3a44"));
            });
        }

        static void DrawTyre(Painter p, float cx, float bottom)
        {
            var t = Shapes.RRect(cx - 27, bottom - 22, 54, 22, 10);
            p.Fill(t, X("#26262c"), 3.4f);
            for (int k = 0; k < 7; k++) p.Fill(Shapes.RRect(cx - 24 + k * 8, bottom - 19, 3, 16, 1), X("#3a3a44"), 0, null, t);
            p.Fill(Shapes.RRect(cx - 20, bottom - 20, 40, 3, 1.5f), new Color(1, 1, 1, .12f), 0, null, t);
        }

        // ================= costumes =================
        static void Costumes()
        {
            // a spiked steel helmet instead of the party hat
            Art2.Hat = Art.Make(-30, -58, 30, 6, Vector2.zero, p =>
            {
                var dome = Shapes.Union(Shapes.Ellipse(0, -10, 26, 20), Shapes.RRect(-27, -12, 54, 12, 3));
                p.Fill(dome, X("#6b7384"), 3.6f);
                p.Fill(Shapes.Ellipse(-8, -18, 11, 6), new Color(1, 1, 1, .25f), 0, null, dome);
                foreach (float sx in new[] { -16f, -5f, 6f, 17f })
                    p.Fill(Shapes.Poly(sx - 4.5f, -22, sx, -48 + Mathf.Abs(sx) * .5f, sx + 4.5f, -22), X("#c9ced8"), 2.6f);
                p.Fill(Shapes.RRect(-28, -6, 56, 7, 2), X("#2b303a"), 2.6f);
            });
            // a black flag with a skull
            Art2.FlagCloth = Art.Make(-3, -4, 56, 30, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(0, 0, 50, 8, 0, 24), X("#17171f"), 3);
                p.Fill(Shapes.Circle(14, 10, 6.2f), Color.white);
                p.Fill(Shapes.RRect(10, 13, 8, 6, 1.5f), Color.white);
                p.Fill(Shapes.Circle(12, 9, 1.7f), X("#17171f")); p.Fill(Shapes.Circle(16.4f, 9, 1.7f), X("#17171f"));
                p.Fill(Shapes.Capsule(8, 21, 22, 15, .9f), Color.white);
            });
            Art2.IconFlag = Art.Make(-24, -24, 24, 24, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(-14, 20, -14, -18, 2.5f), Palette.Steel, 2.5f);
                p.Fill(Shapes.Poly(-12, -19, 20, -10, -12, 1), X("#17171f"), 3, X("#c9ced8"));
                p.Fill(Shapes.Circle(-2, -12, 3.6f), Color.white);
                p.Fill(Shapes.Circle(-3.2f, -12.6f, 1), X("#17171f")); p.Fill(Shapes.Circle(-.8f, -12.6f, 1), X("#17171f"));
            });
        }

        // ================= sticker icons for the worlds =================
        static void Icons()
        {
            Art3.Barn = Art.Make(-40, -44, 40, 40, Vector2.zero, p =>     // junkyard: factory with smoke stacks
            {
                p.Fill(Shapes.RRect(-30, -10, 60, 40, 3), X("#4a4458"), 3.6f);
                p.Fill(Shapes.RRect(-24, -34, 10, 28, 2), X("#4a4458"), 3.4f); p.Fill(Shapes.RRect(8, -42, 12, 36, 2), X("#4a4458"), 3.4f);
                foreach (float wx in new[] { -18f, -4f, 10f, 22f }) p.Fill(Shapes.RRect(wx - 4, 2, 8, 9, 1), X("#ffb35c"));
                p.Fill(Shapes.Circle(14, -48, 6), new Color(.5f, .45f, .5f, .8f)); p.Fill(Shapes.Circle(-19, -40, 5), new Color(.5f, .45f, .5f, .8f));
            });
            Art3.Wave = Art.Make(-40, -44, 40, 40, Vector2.zero, p =>     // desert: cactus on a mesa
            {
                p.Fill(Shapes.Poly(-40, 34, -28, 6, -10, 6, 4, 34), X("#a4503e"), 3.4f);
                p.Fill(Shapes.RRect(14, -30, 12, 60, 6), X("#4f8a3a"), 3.4f);
                p.Fill(Shapes.Union(Shapes.Capsule(14, -2, 2, -2, 5), Shapes.Capsule(2, -2, 2, -16, 5)), X("#4f8a3a"), 3.2f);
                p.Fill(Shapes.Union(Shapes.Capsule(26, 6, 38, 6, 5), Shapes.Capsule(38, 6, 38, -10, 5)), X("#4f8a3a"), 3.2f);
            });
        }

        // ================= mean animals =================
        static void Eye(Painter p, float x, float y, float r)
        {
            p.Fill(Shapes.Circle(x, y, r), X("#ffcc33"), 2);
            p.Fill(Shapes.Capsule(x, y - r * .7f, x, y + r * .7f, r * .26f), Palette.Ink);
            p.Fill(Shapes.Capsule(x - r - 1, y - r - 2.5f, x + r + 2, y - r * .35f, 1.9f), Palette.Ink);
        }
        static void Fangs(Painter p, float x, float y, float s)
        {
            p.Fill(Shapes.Poly(x, y, x + 4 * s, y, x + 2 * s, y + 6 * s), Color.white, 1.6f);
            p.Fill(Shapes.Poly(x + 9 * s, y, x + 13 * s, y, x + 11 * s, y + 6 * s), Color.white, 1.6f);
        }

        static void Animals()
        {
            var ink = Palette.Ink;
            Art2.Animal[Art2.Cow] = Art.Make(-62, -106, 78, 4, Vector2.zero, p =>        // guard dog with a spiked collar
            {
                Color fur = X("#8a6a47"), dk = X("#4a3624"), lt = X("#b9946a");
                foreach (float x in new[] { -34f, -18f, 24f, 38f }) p.Fill(Shapes.Capsule(x, -26, x, -3, 6), fur, 3.2f);
                p.Fill(Shapes.Capsule(-42, -48, -56, -64, 3.4f), fur, 3);
                p.Fill(Shapes.RRect(-46, -64, 90, 42, 16), fur, 4);
                p.Fill(Shapes.Ellipse(30, -36, 14, 12), lt);
                p.Fill(Shapes.RRect(26, -88, 42, 40, 15), fur, 4);
                p.Fill(Shapes.Poly(32, -86, 38, -104, 50, -88), dk, 3);
                p.Fill(Shapes.RRect(48, -70, 28, 20, 8), lt, 3);
                p.Fill(Shapes.Circle(73, -68, 4.2f), ink);
                Eye(p, 49, -78, 4.6f); Fangs(p, 54, -52, .8f);
                p.Fill(Shapes.Capsule(24, -62, 34, -42, 5.5f), X("#2a2a30"), 2.6f);
                for (int k = 0; k < 4; k++) p.Fill(Shapes.Poly(21 + k * 3.5f, -60 + k * 6, 27 + k * 3.5f, -64 + k * 6, 29 + k * 3.5f, -56 + k * 6), X("#c9ced8"), 1.6f);
                p.Grunge(61);
            });
            Art2.Animal[Art2.Sheep] = Art.Make(-72, -44, 58, 4, Vector2.zero, p =>        // big rat
            {
                Color fur = X("#6a6a74"), pink = X("#c98a8a");
                p.Fill(Shapes.Capsule(-24, -14, -42, -8, 2.2f), pink, 2); p.Fill(Shapes.Capsule(-42, -8, -58, -13, 1.8f), pink, 2); p.Fill(Shapes.Capsule(-58, -13, -70, -4, 1.5f), pink, 2);
                foreach (float x in new[] { -8f, 12f }) p.Fill(Shapes.Capsule(x, -10, x + 3, -2, 3), pink, 2);
                p.Fill(Shapes.Ellipse(0, -20, 26, 14), fur, 3.6f);
                p.Fill(Shapes.Poly(20, -30, 50, -19, 22, -8), fur, 3.2f);
                p.Fill(Shapes.Circle(20, -32, 7), pink, 3);
                p.Fill(Shapes.Circle(30, -23, 3.2f), X("#ff4a3a")); p.Fill(Shapes.Circle(52, -19, 2.4f), ink);
                p.Fill(Shapes.Capsule(48, -17, 64, -23, .8f), ink); p.Fill(Shapes.Capsule(48, -15, 64, -12, .8f), ink);
                p.Fill(Shapes.Poly(44, -14, 47, -14, 45.5f, -9), Color.white, 1.2f);
            });
            Art2.Animal[Art2.Duck] = Art.Make(-42, -64, 52, 4, Vector2.zero, p =>         // crow
            {
                Color blk = X("#1e1e29"), wing = X("#2d2d3b");
                foreach (float x in new[] { -4f, 8f }) p.Fill(Shapes.Capsule(x, -14, x, -1, 1.9f), X("#3a3a46"), 2);
                p.Fill(Shapes.Poly(-18, -28, -42, -38, -34, -18), blk, 3);
                p.Fill(Shapes.Ellipse(0, -30, 23, 18), blk, 3.6f);
                p.Fill(Shapes.Ellipse(-6, -28, 15, 10), wing, 2.6f);
                p.Fill(Shapes.Circle(20, -48, 12), blk, 3.6f);
                p.Fill(Shapes.Poly(30, -50, 50, -44, 30, -41), X("#4a4a52"), 2.8f);
                p.Fill(Shapes.Circle(24, -51, 3.4f), X("#ff4a3a")); p.Fill(Shapes.Circle(24.6f, -51, 1.3f), ink);
                p.Fill(Shapes.Capsule(18, -57, 29, -53, 1.6f), ink);
            });
            Art2.Animal[Art2.Crab] = Art.Make(-56, -80, 66, 4, Vector2.zero, p =>         // scorpion
            {
                Color sh = X("#b0561e"), dk = X("#6a3412");
                for (int i = 0; i < 4; i++)
                {
                    float x = -14 + i * 9;
                    p.Fill(Shapes.Capsule(x, -12, x - 6, -1, 1.8f), dk, 2); p.Fill(Shapes.Capsule(x + 4, -12, x + 12, -1, 1.8f), dk, 2);
                }
                foreach (var (cx, cy, cr) in new[] { (-26f, -22f, 7.5f), (-34f, -34f, 7f), (-34f, -48f, 6.5f), (-25f, -60f, 6f), (-10f, -66f, 5.5f) })
                    p.Fill(Shapes.Circle(cx, cy, cr), sh, 3);
                p.Fill(Shapes.Poly(-4, -70, 8, -60, -6, -60), X("#ffd23f"), 2.6f);
                p.Fill(Shapes.Ellipse(0, -14, 28, 12), sh, 4);
                p.Fill(Shapes.Capsule(24, -14, 42, -22, 4.2f), sh, 3);
                p.Fill(Shapes.Circle(48, -26, 8.5f), sh, 3.2f);
                p.Fill(Shapes.Poly(48, -26, 64, -34, 62, -22), Clear, 2.6f);
                p.Fill(Shapes.Circle(18, -22, 2.6f), X("#ff4a3a"));
            });
            Art2.Animal[Art2.Gull] = Art.Make(-50, -90, 56, 4, Vector2.zero, p =>         // vulture
            {
                Color body = X("#3a2e28"), wing = X("#2a201c"), skin = X("#c47a6a");
                foreach (float x in new[] { -6f, 8f }) p.Fill(Shapes.Capsule(x, -16, x, -1, 2.4f), X("#5a4a44"), 2);
                p.Fill(Shapes.Ellipse(-6, -34, 27, 25), body, 4);
                p.Fill(Shapes.Ellipse(-14, -30, 19, 16), wing, 3);
                p.Fill(Shapes.Capsule(14, -48, 22, -68, 5), skin, 3);
                for (int k = 0; k < 4; k++) p.Fill(Shapes.Poly(6 + k * 4, -46, 12 + k * 4, -58, 15 + k * 4, -44), X("#d9d2c0"), 1.6f);
                p.Fill(Shapes.Circle(24, -73, 9.5f), skin, 3.2f);
                p.Fill(Shapes.Poly(31, -76, 47, -71, 38, -63), X("#d9c89a"), 2.8f);
                p.Fill(Shapes.Circle(27, -75, 2.8f), X("#ffcc33")); p.Fill(Shapes.Circle(27.6f, -75, 1.1f), ink);
                p.Fill(Shapes.Capsule(20, -80, 32, -76, 1.5f), ink);
            });
            Art2.Animal[Art2.Penguin] = Art.Make(-80, -98, 88, 4, Vector2.zero, p =>      // wolf
            {
                Color gray = X("#8a8f99"), dk = X("#4a4f59"), lt = X("#d0d4dc");
                p.Fill(Shapes.Poly(-50, -48, -78, -40, -52, -30), gray, 3.4f);
                foreach (float x in new[] { -30f, -16f, 24f, 38f }) p.Fill(Shapes.Capsule(x, -26, x, -3, 5), dk, 3);
                p.Fill(Shapes.RRect(-46, -58, 92, 36, 16), gray, 4);
                p.Fill(Shapes.Ellipse(10, -28, 26, 7), lt);
                p.Fill(Shapes.Poly(44, -66, 48, -92, 62, -70), gray, 3); p.Fill(Shapes.Poly(62, -68, 72, -90, 74, -62), dk, 3);
                p.Fill(Shapes.Circle(58, -52, 17), gray, 4);
                p.Fill(Shapes.Poly(62, -56, 88, -44, 64, -34), lt, 3.2f);
                p.Fill(Shapes.Circle(86, -45, 3.6f), ink);
                Eye(p, 60, -57, 4f); Fangs(p, 72, -37, .7f);
                p.Grunge(67);
            });
            Art2.Animal[Art2.Bear] = Art.Make(-66, -90, 84, 4, Vector2.zero, p =>         // grizzly
            {
                Color fur = X("#6a4a35"), dk = X("#3e2b1e"), snout = X("#a98363");
                foreach (float x in new[] { -34f, -18f, 22f, 38f }) { p.Fill(Shapes.Capsule(x, -24, x, -3, 8), fur, 3.5f); p.Fill(Shapes.Poly(x - 4, -4, x - 1, 1, x + 2, -4), X("#e8e0d0"), 1.2f); }
                p.Fill(Shapes.Circle(-54, -42, 7), fur, 3.5f);
                p.Fill(Shapes.RRect(-52, -62, 102, 44, 20), fur, 4);
                p.Fill(Shapes.Ellipse(10, -72, 24, 14), fur, 3.6f);
                p.Fill(Shapes.Circle(44, -68, 7), dk, 3); p.Fill(Shapes.Circle(62, -68, 7), dk, 3);
                p.Fill(Shapes.Circle(53, -47, 21), fur, 4);
                p.Fill(Shapes.Ellipse(70, -41, 12, 10), snout, 3);
                p.Fill(Shapes.Circle(77, -43, 3.8f), ink);
                Eye(p, 56, -53, 4.4f); Fangs(p, 64, -34, .8f);
                p.Grunge(71);
            });
            Art2.Animal[Art2.Snowman] = Art.Make(-52, -108, 52, 4, Vector2.zero, p =>     // yeti
            {
                Color fur = X("#dfe8f2"), mask = X("#5a6a80");
                p.Fill(Shapes.Ellipse(-38, -38, 13, 26), fur, 3.8f); p.Fill(Shapes.Ellipse(38, -38, 13, 26), fur, 3.8f);
                p.Fill(Shapes.Ellipse(-15, -5, 16, 8), fur, 3.8f); p.Fill(Shapes.Ellipse(15, -5, 16, 8), fur, 3.8f);
                p.Fill(Shapes.Ellipse(0, -40, 33, 36), fur, 4.2f);
                p.Fill(Shapes.Circle(0, -82, 21), fur, 4.2f);
                p.Fill(Shapes.Ellipse(0, -78, 14, 13), mask, 3);
                p.Fill(Shapes.Poly(-18, -96, -26, -112, -9, -100), X("#a9b4c4"), 2.6f); p.Fill(Shapes.Poly(18, -96, 26, -112, 9, -100), X("#a9b4c4"), 2.6f);
                p.Fill(Shapes.Circle(-6, -82, 3.4f), X("#ff4a3a")); p.Fill(Shapes.Circle(6, -82, 3.4f), X("#ff4a3a"));
                p.Fill(Shapes.Capsule(-10, -87, -2, -84, 1.7f), ink); p.Fill(Shapes.Capsule(10, -87, 2, -84, 1.7f), ink);
                Fangs(p, -7, -73, .9f);
            });
            Art2.Animal[Art2.Owl] = Art.Make(-40, -82, 40, 4, Vector2.zero, p =>          // night owl with burning eyes
            {
                Color brown = X("#5a4a3a"), cream = X("#c9b89a"), orange = X("#e0841a");
                foreach (float x in new[] { -8f, 8f }) p.Fill(Shapes.Capsule(x, -10, x, -1, 2.5f), orange, 2);
                p.Fill(Shapes.Ellipse(-25, -34, 7, 16), Palette.Shade(brown, -.2f), 3); p.Fill(Shapes.Ellipse(25, -34, 7, 16), Palette.Shade(brown, -.2f), 3);
                p.Fill(Shapes.Poly(-20, -56, -14, -76, -4, -58), brown, 3); p.Fill(Shapes.Poly(4, -58, 14, -76, 20, -56), brown, 3);
                p.Fill(Shapes.Ellipse(0, -36, 26, 32), brown, 4);
                p.Fill(Shapes.Ellipse(0, -26, 16, 21), cream);
                p.Fill(Shapes.Circle(-11, -48, 10), X("#ffcc33"), 3); p.Fill(Shapes.Circle(11, -48, 10), X("#ffcc33"), 3);
                p.Fill(Shapes.Capsule(-11, -55, -11, -41, 2.4f), ink); p.Fill(Shapes.Capsule(11, -55, 11, -41, 2.4f), ink);
                p.Fill(Shapes.Poly(-24, -62, 0, -50, -22, -52), ink); p.Fill(Shapes.Poly(24, -62, 0, -50, 22, -52), ink);
                p.Fill(Shapes.Poly(-4, -42, 4, -42, 0, -33), X("#3a3a46"), 2.5f);
            });
            Art2.Animal[Art2.Fox] = Art.Make(-80, -92, 86, 4, Vector2.zero, p =>          // coyote
            {
                Color tan = X("#a8855a"), cream = X("#e6d6b8"), dark = X("#4a3a2c");
                p.Fill(Shapes.Poly(-46, -50, -76, -34, -52, -28), tan, 3.4f); p.Fill(Shapes.Poly(-66, -38, -78, -33, -68, -30), dark, 2);
                foreach (float x in new[] { -30f, -16f, 22f, 36f }) p.Fill(Shapes.Capsule(x, -26, x, -3, 4), dark, 3);
                p.Fill(Shapes.RRect(-44, -56, 90, 32, 15), tan, 4);
                p.Fill(Shapes.Ellipse(8, -28, 26, 6), cream);
                p.Fill(Shapes.Poly(46, -64, 49, -90, 62, -68), tan, 3); p.Fill(Shapes.Poly(62, -66, 72, -88, 74, -60), tan, 3);
                p.Fill(Shapes.Circle(58, -50, 16), tan, 4);
                p.Fill(Shapes.Poly(62, -54, 84, -45, 64, -36), cream, 3);
                p.Fill(Shapes.Circle(82, -46, 3.4f), ink);
                Eye(p, 60, -55, 3.8f); Fangs(p, 70, -38, .65f);
                p.Grunge(73);
            });
            Art2.Animal[Art2.Hedgehog] = Art.Make(-54, -78, 54, 4, Vector2.zero, p =>     // bat
            {
                Color body = X("#3a2a4a"), wing = X("#4a3a5c");
                foreach (float x in new[] { -6f, 6f }) p.Fill(Shapes.Capsule(x, -14, x, -1, 2), X("#2a1f36"), 2);
                p.Fill(Shapes.Poly(-12, -46, -52, -70, -46, -44, -38, -36, -28, -22, -12, -22), wing, 3.2f);
                p.Fill(Shapes.Poly(12, -46, 52, -70, 46, -44, 38, -36, 28, -22, 12, -22), wing, 3.2f);
                p.Fill(Shapes.Capsule(-12, -44, -46, -62, 1.2f), body); p.Fill(Shapes.Capsule(12, -44, 46, -62, 1.2f), body);
                p.Fill(Shapes.Ellipse(0, -30, 13, 20), body, 3.6f);
                p.Fill(Shapes.Poly(-10, -48, -14, -68, -3, -52), body, 3); p.Fill(Shapes.Poly(10, -48, 14, -68, 3, -52), body, 3);
                p.Fill(Shapes.Circle(0, -46, 10), body, 3.4f);
                p.Fill(Shapes.Circle(-4, -47, 2.6f), X("#ff4a3a")); p.Fill(Shapes.Circle(4, -47, 2.6f), X("#ff4a3a"));
                Fangs(p, -6, -41, .8f);
            });
        }
    }
}
