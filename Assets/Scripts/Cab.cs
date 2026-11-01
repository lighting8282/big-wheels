using UnityEngine;

namespace BigWheels
{
    /// <summary>
    /// Sprites for the first-person cab. Design space: x runs from the centre line, y is "down is positive" and 0 is the bottom edge of the
    /// screen, so everything above the dash has a negative y. Big pieces are drawn in place (pivot at the design origin); moving parts
    /// (needles, levers, gloves, dice, wipers) are drawn around their own pivot so they can be rotated.
    /// </summary>
    public static class CabArt
    {
        public static Sprite Frame, WheelRing, Spoke, Glove, Sleeve, Needle, Lever, NitroBtn, NitroFlap, Dice, Mic, Cracks, Wiper;
        public static Sprite[] Hood = new Sprite[7], Scenery = new Sprite[5], Hang = new Sprite[3], DashSticker = new Sprite[3];
        static bool built;
        static readonly Color Clear = new Color(0, 0, 0, 0);
        static Color X(string h) => Palette.Hex(h);

        static void Gauge(Painter p, float cx, float cy, float r, float tickR)
        {
            p.Fill(Shapes.Circle(cx, cy, r), X("#0c0e12"), 3.2f, X("#8d97aa"));
            for (int k = 0; k < 20; k++)
            {
                float a = Mathf.PI * (.75f + 1.5f * k / 19f), c = Mathf.Cos(a), s = Mathf.Sin(a);
                p.Fill(Shapes.Capsule(cx + c * tickR, cy + s * tickR, cx + c * (tickR - 3.6f), cy + s * (tickR - 3.6f), .8f), k >= 16 ? X("#d33a2c") : X("#cfd3dc"));
            }
        }

        public static void Build()
        {
            if (built) return; built = true;
            Color dark = X("#17171b"), panel = X("#1a1d24"), edge = X("#0f1116"), steel = X("#8d97aa"), yel = X("#f0b400");

            // ---- the cab: door panels, pillars, roof, overhead panel, mirror, CB radio, harness and the whole dashboard ----
            Frame = Art.Make(-380, -440, 380, 0, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(-380, -440, -278, -440, -196, -66, -380, -66), dark);
                p.Fill(Shapes.Poly(380, -440, 278, -440, 196, -66, 380, -66), dark);
                p.Fill(Shapes.Capsule(-278, -440, -196, -66, 2.6f), X("#2b303a"));
                p.Fill(Shapes.Capsule(278, -440, 196, -66, 2.6f), X("#2b303a"));
                p.Fill(Shapes.Capsule(-350, -380, -318, -80, 1.3f), X("#0c0e12")); p.Fill(Shapes.Capsule(350, -380, 318, -80, 1.3f), X("#0c0e12"));
                p.Fill(Shapes.RRect(-380, -440, 760, 34, 0), dark);
                p.Fill(Shapes.RRect(-90, -406, 180, 12, 3), X("#26262c"));
                for (int k = 0; k < 7; k++) p.Fill(Shapes.Circle(-72 + k * 24, -400, 3), Palette.Sun);
                p.Fill(Shapes.RRect(-186, -404, 104, 28, 5), X("#101218"), 2.4f, X("#3a404c"));          // overhead panel for the turbo flames
                p.Fill(Shapes.RRect(88, -406, 78, 28, 4), X("#26262c"), 2, edge);                           // CB radio
                p.Fill(Shapes.Circle(102, -392, 5), steel, 1.5f); p.Fill(Shapes.Circle(120, -392, 5), steel, 1.5f);
                p.Fill(Shapes.RRect(132, -400, 28, 13, 1.5f), X("#0b1a0b"), 1.5f, edge);
                p.Fill(Shapes.Capsule(136, -396, 141, -396, .8f), X("#7dff8a")); p.Fill(Shapes.Capsule(145, -396, 151, -396, .8f), X("#7dff8a")); p.Fill(Shapes.Capsule(136, -391, 151, -391, .8f), X("#7dff8a"));
                p.Fill(Shapes.Capsule(0, -406, 0, -394, 1.8f), X("#3a404c"));                               // rear-view mirror
                p.Fill(Shapes.RRect(-44, -394, 88, 26, 6), dark, 2, X("#3a404c"));
                p.Fill(Shapes.RRect(-39, -390, 78, 18, 3), X("#6b4a7a"));
                p.Fill(Shapes.Capsule(-30, -384, -8, -384, .8f), new Color(1, 1, 1, .5f)); p.Fill(Shapes.Capsule(10, -378, 32, -378, .8f), new Color(1, 1, 1, .5f));
                p.Fill(Shapes.Poly(-262, -320, -242, -320, -224, -240, -244, -240), yel);                   // padded pillars
                p.Fill(Shapes.Poly(262, -320, 242, -320, 224, -240, 244, -240), yel);
                for (int k = 0; k < 3; k++)
                {
                    p.Fill(Shapes.Capsule(-258 + k * 5, -312 + k * 28, -246 + k * 5, -312 + k * 28, 1.6f), dark);
                    p.Fill(Shapes.Capsule(258 - k * 5, -312 + k * 28, 246 - k * 5, -312 + k * 28, 1.6f), dark);
                }
                p.Fill(Shapes.RRect(-366, -330, 56, 36, 6), X("#0c0e12"), 2.5f, steel); p.Fill(Shapes.RRect(-362, -326, 48, 28, 3), X("#5a3a6a"));   // wing mirrors
                p.Fill(Shapes.RRect(310, -330, 56, 36, 6), X("#0c0e12"), 2.5f, steel); p.Fill(Shapes.RRect(314, -326, 48, 28, 3), X("#5a3a6a"));
                p.Fill(Shapes.Poly(-300, -140, -276, -140, -238, 0, -282, 0), X("#2a2d34"), 2, edge);       // harness straps
                p.Fill(Shapes.Capsule(-288, -136, -258, -6, .9f), X("#c0282d")); p.Fill(Shapes.RRect(-268, -52, 18, 22, 3), steel, 2, edge);
                p.Fill(Shapes.Poly(300, -140, 276, -140, 238, 0, 282, 0), X("#2a2d34"), 2, edge);
                p.Fill(Shapes.Capsule(288, -136, 258, -6, .9f), X("#c0282d")); p.Fill(Shapes.RRect(250, -52, 18, 22, 3), steel, 2, edge);

                // dashboard
                p.Fill(Shapes.Poly(-380, 0, -380, -52, -340, -58, -250, -66, 250, -66, 340, -58, 380, -52, 380, 0), panel, 3, edge);
                var strip = Shapes.RRect(-250, -66, 500, 8, 0);
                p.Fill(strip, yel);
                for (int k = 0; k < 22; k++) p.Fill(Shapes.Poly(-250 + k * 24, -58, -242 + k * 24, -66, -234 + k * 24, -66, -242 + k * 24, -58), dark, 0, null, strip);
                for (int k = 0; k < 20; k++) p.Fill(Shapes.Circle(-288 + k * 30, -50, 2.2f), steel);
                Gauge(p, -228, -26, 24, 19); Gauge(p, -160, -22, 19, 14); Gauge(p, 176, -22, 14, 10); Gauge(p, 212, -26, 18, 13);
                p.Fill(Shapes.RRect(128, -52, 104, 18, 4), X("#101218"), 2, X("#3a404c"));                   // toggle switches
                foreach (float x in new[] { 142f, 164f, 186f, 208f }) p.Fill(Shapes.Circle(x, -43, 4.4f), X("#2b303a"));
                p.Fill(Shapes.RRect(246, -46, 34, 32, 5), panel, 3, yel);                                    // guard for the nitro button
                p.Fill(Shapes.Poly(292, 0, 298, -20, 312, -20, 318, 0), X("#101218"), 2, X("#3a404c"));      // gear lever boot
                p.Fill(Shapes.RRect(-352, -52, 22, 52, 6), X("#c0282d"), 2.4f, edge);                        // fire extinguisher
                p.Fill(Shapes.RRect(-346, -60, 10, 9, 1.5f), dark); p.Fill(Shapes.Capsule(-344, -60, -326, -64, 1.8f), dark);
                p.Fill(Shapes.Circle(-341, -36, 4.4f), Color.white, 1.4f); p.Fill(Shapes.RRect(-352, -14, 22, 5, 0), dark);
                p.Fill(Shapes.Ellipse(-40, -30, 70, 8), new Color(0, 0, 0, .25f)); p.Fill(Shapes.Ellipse(160, -6, 50, 7), new Color(0, 0, 0, .25f));
                p.Grunge(77, .5f, .5f);
            });

            // ---- the hood, one per paint colour ----
            for (int i = 0; i < Hood.Length; i++)
            {
                int ci = i; Color c = Palette.Truck[i];
                Hood[i] = Art.Make(-215, -112, 215, -34, Vector2.zero, p =>
                {
                    p.Fill(Shapes.Poly(-205, -60, -100, -104, 100, -104, 205, -60, 205, -40, -205, -40), c, 4);
                    p.Fill(Shapes.Poly(-170, -62, -96, -98, -30, -98, -110, -62), new Color(1, 1, 1, .18f));
                    p.Fill(Shapes.Capsule(0, -104, 0, -40, 1.3f), Palette.Shade(c, -.3f));
                    p.Fill(Shapes.RRect(-34, -100, 68, 13, 3), dark, 2.2f, edge);
                    foreach (float x in new[] { -18f, 0f, 18f }) p.Fill(Shapes.Capsule(x, -98, x, -90, 1.1f), X("#5a5a62"));
                    p.Fill(Shapes.Circle(-74, -98, 8), X("#fff6a8"), 3, edge); p.Fill(Shapes.Circle(74, -98, 8), X("#fff6a8"), 3, edge);
                    p.Fill(Shapes.Circle(-146, -72, 5), steel, 2, edge); p.Fill(Shapes.Circle(146, -72, 5), steel, 2, edge);
                    p.Fill(Shapes.Capsule(-110, -66, -70, -80, .8f), new Color(1, 1, 1, .3f)); p.Fill(Shapes.Capsule(60, -86, 110, -70, .8f), new Color(1, 1, 1, .3f));
                    p.Fill(Shapes.Ellipse(96, -58, 20, 6), new Color(.35f, .24f, .15f, .55f));
                    p.Grunge(ci * 7 + 4, .35f, .5f);
                });
            }

            // ---- steering wheel: the rim as an ellipse; the spokes, the red marker and the hands move around it ----
            WheelRing = Art.Make(-142, -96, 142, 0, Vector2.zero, p =>
            {
                var ring = Shapes.Ellipse(0, 14, 124, 92);
                p.Fill(ring, Clear, 26, X("#3a3d46")); p.Fill(ring, Clear, 18, X("#a0a6b2")); p.Fill(ring, Clear, 2.6f, X("#f0f4fa"));
            });
            Spoke = Art.Make(-9, 0, 9, 100, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-8, 0, 16, 100, 3), X("#a0a6b2"), 2.4f, X("#3a3d46")); p.Fill(Shapes.RRect(-2, 4, 4, 92, 2), X("#f0f4fa"));
            });
            Glove = Art.Make(-30, -24, 30, 24, Vector2.zero, p =>
            {
                p.Fill(Shapes.Ellipse(0, 0, 25, 16), X("#1d1d24"), 2.5f, edge);
                p.Fill(Shapes.RRect(-22, -9, 44, 6, 3), Color.white);
                for (int k = 0; k < 3; k++) p.Fill(Shapes.Capsule(-10 + k * 10, -15, -10 + k * 10, -7, .9f), edge);
                p.Fill(Shapes.Ellipse(-8, -7, 9, 4), new Color(1, 1, 1, .15f));
            });
            Sleeve = Art.Make(-24, 0, 24, 100, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-22, 0, 44, 100, 4), X("#e6e8ee"), 2.4f, edge); p.Fill(Shapes.RRect(-23, 0, 46, 10, 3), X("#2a2d34"), 2, edge);
            });

            // ---- small moving parts ----
            Needle = Art.Make(-4, -26, 4, 6, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(-1.7f, 0, 0, -24, 1.7f, 0), X("#ff9a1f")); p.Fill(Shapes.Circle(0, 0, 3.3f), steel);
            });
            Lever = Art.Make(-6, -16, 6, 4, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(0, 0, 0, -10, 1.5f), X("#cfd3dc")); p.Fill(Shapes.Circle(0, -11, 2.9f), Color.white);
            });
            NitroBtn = Art.Make(-12, -12, 12, 12, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 9.5f), X("#d33a2c"), 2.4f, edge); p.Fill(Shapes.Circle(-3.2f, -3.2f, 2.8f), new Color(1, 1, 1, .5f));
            });
            NitroFlap = Art.Make(-17, 0, 17, 16, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-16, 0, 32, 14, 3), new Color(.23f, .25f, .3f, .8f), 2, yel);
            });
            Dice = Art.Make(-14, 0, 14, 56, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(0, 0, 0, 32, .7f), X("#c9ced8"));
                p.Fill(Shapes.RRect(-10, 30, 20, 20, 3), Color.white, 2, edge);
                p.Fill(Shapes.Circle(-4.5f, 36, 1.8f), edge); p.Fill(Shapes.Circle(4.5f, 44, 1.8f), edge); p.Fill(Shapes.Circle(0, 40, 1.8f), edge);
            });
            Hang[0] = Dice;
            Hang[1] = Art.Make(-14, 0, 14, 56, Vector2.zero, p =>                         // a little skull on a string
            {
                p.Fill(Shapes.Capsule(0, 0, 0, 30, .7f), X("#c9ced8"));
                p.Fill(Shapes.Circle(0, 40, 10), Color.white, 2, edge); p.Fill(Shapes.RRect(-5, 46, 10, 6, 1.5f), Color.white, 1.6f, edge);
                p.Fill(Shapes.Circle(-3.6f, 39, 2.6f), edge); p.Fill(Shapes.Circle(3.6f, 39, 2.6f), edge);
            });
            Hang[2] = Art.Make(-14, 0, 14, 60, Vector2.zero, p =>                         // a pine-tree air freshener
            {
                p.Fill(Shapes.Capsule(0, 0, 0, 22, .7f), X("#c9ced8"));
                p.Fill(Shapes.Poly(0, 22, 11, 38, 5, 38, 13, 52, -13, 52, -5, 38, -11, 38), X("#3a8a4a"), 2, edge);
                p.Fill(Shapes.RRect(-2, 52, 4, 6, 1), X("#6b4a30"), 1.4f, edge);
            });
            DashSticker[0] = Art.Make(-14, -14, 14, 14, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 10), Color.white, 2, edge); p.Fill(Shapes.Circle(-4, -2, 2.6f), edge); p.Fill(Shapes.Circle(4, -2, 2.6f), edge); p.Fill(Shapes.RRect(-4, 4, 8, 4, 0), edge);
            });
            DashSticker[1] = Art.Make(-14, -14, 14, 14, Vector2.zero, p =>
            {
                p.Fill(Shapes.Union(Shapes.Circle(0, 3, 8), Shapes.Poly(-7, 0, 1, -13, 7, 0)), X("#ff8a1f"), 2, edge); p.Fill(Shapes.Union(Shapes.Circle(0, 5, 4), Shapes.Poly(-3, 3, 1, -5, 3, 3)), X("#ffe45e"));
            });
            DashSticker[2] = Art.Make(-14, -14, 14, 14, Vector2.zero, p => p.Fill(Shapes.Star(0, 0, 12), Palette.Sun, 2, edge));
            Mic = Art.Make(-10, 0, 10, 42, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(0, 0, 0, 8, 1.4f), dark);
                p.Fill(Shapes.RRect(-7, 8, 14, 28, 5), X("#26262c"), 2, edge); p.Fill(Shapes.Circle(0, 16, 2.4f), X("#d33a2c"));
            });
            Wiper = Art.Make(-6, -8, 196, 8, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(0, 0, 190, 0, 1.6f), X("#2b303a")); p.Fill(Shapes.Capsule(60, 0, 190, 0, 3.2f), edge); p.Fill(Shapes.Circle(0, 0, 4.5f), X("#3a404c"));
            });

            // ---- cracks across the glass (they appear as things hit the windshield) ----
            Cracks = Art.Make(-230, -400, 230, -80, Vector2.zero, p =>
            {
                Color w = new Color(1, 1, 1, .8f);
                foreach (var (cx, cy, n) in new[] { (-176f, -330f, 8), (168f, -250f, 7), (40f, -190f, 5) })
                    for (int k = 0; k < n; k++)
                    {
                        float a = k * Mathf.PI * 2f / n + cx * .013f, len = 34f + 38f * Mathf.Abs(Mathf.Sin(k * 2.3f + cy));
                        float ex = cx + Mathf.Cos(a) * len, ey = cy + Mathf.Sin(a) * len;
                        p.Fill(Shapes.Capsule(cx, cy, ex, ey, .8f), w);
                        float mx = (cx + ex) / 2f, my = (cy + ey) / 2f;
                        p.Fill(Shapes.Capsule(mx, my, mx + Mathf.Cos(a + 1.1f) * 16f, my + Mathf.Sin(a + 1.1f) * 16f, .6f), w);
                    }
            });

            // ---- roadside scenery (bottom-centre pivot): floodlight pole, cactus, dead pine, scrap heap, rock ----
            Scenery[0] = Art.Make(-40, -224, 40, 0, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-4, -216, 8, 216, 1), X("#4a4f5c"), 2.4f, edge);
                p.Fill(Shapes.RRect(-30, -226, 60, 16, 3), X("#2b303a"), 2.4f, edge);
                for (int k = 0; k < 5; k++) p.Fill(Shapes.Circle(-22 + k * 11, -218, 3), X("#ffe27a"));
            });
            Scenery[1] = Art.Make(-40, -100, 40, 0, Vector2.zero, p =>
            {
                Color g = X("#4f8a3a");
                p.Fill(Shapes.RRect(-9, -96, 18, 96, 9), g, 3, edge);
                p.Fill(Shapes.Union(Shapes.Capsule(-9, -40, -24, -40, 6), Shapes.Capsule(-24, -40, -24, -64, 6)), g, 3, edge);
                p.Fill(Shapes.Union(Shapes.Capsule(9, -54, 24, -54, 6), Shapes.Capsule(24, -54, 24, -78, 6)), g, 3, edge);
                p.Fill(Shapes.RRect(-9, -96, 18, 96, 9), g);
            });
            Scenery[2] = Art.Make(-40, -150, 40, 0, Vector2.zero, p =>
            {
                Color t = X("#24403f");
                p.Fill(Shapes.Poly(-5, 0, -3, -140, 3, -140, 5, 0), X("#3a2e26"), 2.4f, edge);
                foreach (var (y, w) in new[] { (-40f, 30f), (-70f, 24f), (-100f, 17f) }) { p.Fill(Shapes.Poly(-w, y + 12, 0, y - 20, w, y + 12), t, 2.4f, edge); p.Fill(Shapes.Poly(-w * .4f, y - 6, 0, y - 20, w * .4f, y - 6), X("#d9e4ee")); }
            });
            Scenery[3] = Art.Make(-80, -80, 80, 0, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(-76, 0, -56, -36, -34, -56, -10, -40, 14, -62, 44, -34, 76, 0), X("#5a4a44"), 3, edge);
                p.Fill(Shapes.Capsule(-14, -44, -8, -76, 2.4f), X("#7a6a62"), 2, edge); p.Fill(Shapes.Capsule(36, -36, 46, -66, 2f), X("#6a5a52"), 2, edge);
                p.Fill(Shapes.Circle(-40, -44, 8), X("#8a4a22"), 2, edge);
                p.Grunge(19, .3f, .6f);
            });
            Scenery[4] = Art.Make(-40, -38, 40, 0, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(-36, 0, -30, -22, -10, -34, 16, -30, 34, -12, 38, 0), X("#6a625c"), 3, edge);
                p.Fill(Shapes.Poly(-20, -24, -8, -30, 6, -26, -10, -18), new Color(1, 1, 1, .15f));
            });
        }
    }
}
