using UnityEngine;

namespace BigWheels
{
    /// <summary>Second batch of procedural sprites: animals, new obstacles, the beach world, costumes and UI icons.</summary>
    public static class Art2
    {
        public const int Cow = 0, Sheep = 1, Duck = 2, Crab = 3, Gull = 4;
        public static Sprite[] Animal = new Sprite[5], Jelly = new Sprite[4];
        public static Sprite Trampoline, StreamWater, Hat, FlagPole, FlagCloth, IconEyes, IconFlag, UiRingSel;
        public static Sprite SkyBeach, HillSea, HillDunes;
        static bool built;

        // Jelly flavours reuse the truck palette: pink, green, purple, orange
        public static readonly int[] JellyPalette = { 6, 3, 5, 1 };

        static readonly Color T = new Color(0, 0, 0, 0);

        public static void Build()
        {
            if (built) return; built = true;
            var ink = Palette.Ink;

            Animal[Cow] = Art.Make(-62, -90, 70, 4, Vector2.zero, p =>
            {
                foreach (float x in new[] { -34f, -18f, 22f, 38f }) p.Fill(Shapes.Capsule(x, -26, x, -3, 5), Color.white, 3.5f);
                foreach (float x in new[] { -34f, -18f, 22f, 38f }) p.Fill(Shapes.Capsule(x, -4, x, -2, 5), Palette.Hex("#3a3a4a"));
                p.Fill(Shapes.Capsule(-48, -52, -58, -32, 2), ink);
                p.Fill(Shapes.Circle(-59, -29, 4.5f), ink);
                var body = Shapes.RRect(-48, -64, 92, 42, 16);
                p.Fill(body, Color.white);
                var spot = Palette.Hex("#3a3a4a");
                p.Fill(Shapes.Ellipse(-26, -46, 12, 9), spot, 0, null, body);
                p.Fill(Shapes.Ellipse(10, -36, 14, 10), spot, 0, null, body);
                p.Fill(Shapes.Ellipse(26, -56, 8, 7), spot, 0, null, body);
                p.Fill(body, T, 4);
                p.Fill(Shapes.Capsule(38, -78, 34, -87, 2.5f), Palette.Sun, 2.5f);
                p.Fill(Shapes.Ellipse(30, -70, 8, 5), Palette.Hex("#ffb3c1"), 3);
                p.Fill(Shapes.RRect(34, -78, 32, 36, 12), Color.white, 4);
                p.Fill(Shapes.Ellipse(56, -48, 13, 9), Palette.Hex("#ffb3c1"), 3);
                p.Fill(Shapes.Circle(52, -48, 1.6f), ink); p.Fill(Shapes.Circle(60, -48, 1.6f), ink);
                p.Fill(Shapes.Circle(48, -64, 3.4f), ink); p.Fill(Shapes.Circle(49, -65, 1.1f), Color.white);
            });

            Animal[Sheep] = Art.Make(-56, -82, 66, 4, Vector2.zero, p =>
            {
                var dark = Palette.Hex("#4a4a5c");
                foreach (float x in new[] { -24f, -8f, 14f, 30f }) p.Fill(Shapes.Capsule(x, -24, x, -2, 4.5f), dark, 3);
                var fluff = Shapes.Union(Shapes.Circle(-30, -44, 18), Shapes.Circle(-12, -54, 20), Shapes.Circle(10, -54, 20),
                    Shapes.Circle(28, -44, 18), Shapes.Circle(-4, -36, 22), Shapes.Circle(14, -34, 18));
                p.Fill(fluff, Palette.Hex("#fffaf0"), 4);
                p.Fill(Shapes.Ellipse(34, -56, 7, 4), dark, 3);
                p.Fill(Shapes.Circle(44, -48, 15), dark, 4);
                p.Fill(Shapes.Circle(48, -52, 4), Color.white); p.Fill(Shapes.Circle(49, -52, 2), ink);
                p.Fill(Shapes.Arc(48, -45, 5, .2f * Mathf.PI, .8f * Mathf.PI, 1.2f), Color.white);
                p.Fill(Shapes.Circle(36, -62, 8), Palette.Hex("#fffaf0"), 3);
            });

            Animal[Duck] = Art.Make(-36, -60, 48, 4, Vector2.zero, p =>
            {
                var orange = Palette.Hex("#ff9a1f");
                p.Fill(Shapes.Capsule(-6, -10, -6, -1, 2.2f), orange, 2); p.Fill(Shapes.Capsule(8, -10, 8, -1, 2.2f), orange, 2);
                p.Fill(Shapes.Poly(-22, -32, -34, -40, -26, -22), Palette.Sun, 3);
                p.Fill(Shapes.Ellipse(0, -26, 26, 18), Palette.Sun, 4);
                p.Fill(Shapes.Ellipse(-4, -24, 13, 8), Palette.Hex("#f5b82e"), 3);
                p.Fill(Shapes.Circle(20, -46, 14), Palette.Sun, 4);
                p.Fill(Shapes.Poly(31, -46, 45, -43, 31, -39), orange, 3);
                p.Fill(Shapes.Circle(24, -49, 2.8f), ink);
            });

            Animal[Crab] = Art.Make(-50, -54, 50, 4, Vector2.zero, p =>
            {
                var red = Palette.Hex("#ff5a4a");
                for (int i = 0; i < 3; i++)
                {
                    float x = 8 + i * 8;
                    p.Fill(Shapes.Capsule(-x, -14, -x - 12, -1, 2.2f), red, 2.5f);
                    p.Fill(Shapes.Capsule(x, -14, x + 12, -1, 2.2f), red, 2.5f);
                }
                p.Fill(Shapes.Capsule(-24, -26, -34, -36, 3), red, 3); p.Fill(Shapes.Capsule(24, -26, 34, -36, 3), red, 3);
                p.Fill(Shapes.Circle(-38, -42, 10), red, 3.5f); p.Fill(Shapes.Circle(38, -42, 10), red, 3.5f);
                p.Fill(Shapes.Poly(-38, -42, -44, -54, -34, -52), Palette.Hex("#ffd23f"), 0, null, Shapes.Circle(-38, -42, 10));
                p.Fill(Shapes.Capsule(-8, -34, -8, -46, 1.6f), ink); p.Fill(Shapes.Capsule(8, -34, 8, -46, 1.6f), ink);
                p.Fill(Shapes.Ellipse(0, -22, 28, 16), red, 4);
                p.Fill(Shapes.Circle(-8, -47, 5.5f), Color.white, 2.5f); p.Fill(Shapes.Circle(8, -47, 5.5f), Color.white, 2.5f);
                p.Fill(Shapes.Circle(-7, -47, 2.4f), ink); p.Fill(Shapes.Circle(9, -47, 2.4f), ink);
                p.Fill(Shapes.Arc(0, -22, 8, .2f * Mathf.PI, .8f * Mathf.PI, 1.6f), ink);
            });

            Animal[Gull] = Art.Make(-44, -74, 54, 4, Vector2.zero, p =>
            {
                var orange = Palette.Hex("#ffb52e");
                p.Fill(Shapes.Capsule(-4, -16, -4, -1, 1.8f), orange, 2); p.Fill(Shapes.Capsule(7, -16, 7, -1, 1.8f), orange, 2);
                p.Fill(Shapes.Poly(-24, -40, -42, -46, -26, -28), Palette.Hex("#c9d3e6"), 3);
                p.Fill(Shapes.Ellipse(0, -35, 26, 17), Color.white, 4);
                p.Fill(Shapes.Ellipse(-6, -35, 18, 9), Palette.Hex("#c9d3e6"), 3);
                p.Fill(Shapes.Circle(22, -55, 11), Color.white, 4);
                p.Fill(Shapes.Poly(31, -56, 48, -52, 31, -48), Palette.Sun, 3);
                p.Fill(Shapes.Circle(25, -58, 2.6f), ink);
            });

            Trampoline = Art.Make(-90, -66, 90, 4, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(-62, -38, -72, -2, 4), Palette.Steel, 3); p.Fill(Shapes.Capsule(62, -38, 72, -2, 4), Palette.Steel, 3);
                p.Fill(Shapes.Capsule(-26, -38, -28, -2, 3.5f), Palette.Steel, 3); p.Fill(Shapes.Capsule(26, -38, 28, -2, 3.5f), Palette.Steel, 3);
                p.Fill(Shapes.RRect(-82, -52, 164, 16, 8), Palette.Hex("#ff4b4b"), 4);
                p.Fill(Shapes.RRect(-76, -57, 152, 9, 4.5f), Palette.Hex("#3d8bff"), 3.5f);
                p.Fill(Shapes.Capsule(-40, -50, -40, -40, 1.5f), Color.white); p.Fill(Shapes.Capsule(0, -50, 0, -40, 1.5f), Color.white); p.Fill(Shapes.Capsule(40, -50, 40, -40, 1.5f), Color.white);
            });

            for (int i = 0; i < 4; i++)
            {
                Color c = Palette.Truck[JellyPalette[i]];
                Jelly[i] = Art.Make(-80, -70, 80, 6, Vector2.zero, p =>
                {
                    var half = new Shape(q => q.y, new Rect(-80, -70, 160, 70));
                    p.Fill(Shapes.Ellipse(0, 0, 70, 58), new Color(c.r, c.g, c.b, .92f), 4, null, half);
                    p.Fill(Shapes.Ellipse(0, -8, 56, 42), Palette.Shade(c, .22f), 0, null, half);
                    p.Fill(Shapes.Capsule(-70, 0, 70, 0, 2f), ink);
                    p.Fill(Shapes.Ellipse(-28, -40, 13, 7), new Color(1, 1, 1, .6f));
                    p.Fill(Shapes.Circle(-10, -50, 4), new Color(1, 1, 1, .6f));
                });
            }

            StreamWater = Art.Make(-156, -6, 156, 42, Vector2.zero, p =>
            {
                p.Fill(Shapes.Ellipse(0, 10, 150, 26), Palette.Hex("#46b8f0"), 4);
                p.Fill(Shapes.Ellipse(0, 8, 136, 18), Palette.Hex("#7ad2ff"));
                var w = new Color(1, 1, 1, .75f);
                p.Fill(Shapes.Ellipse(-60, 8, 22, 5), T, 2.2f, w); p.Fill(Shapes.Ellipse(40, 12, 28, 6), T, 2.2f, w); p.Fill(Shapes.Ellipse(92, 6, 16, 4), T, 2.2f, w);
            });

            Hat = Art.Make(-28, -54, 28, 6, Vector2.zero, p =>
            {
                var cone = Shapes.Poly(-18, 0, 18, 0, 0, -44);
                p.Fill(cone, Palette.Hex("#ff6eb4"), 0);
                var yel = Palette.Sun;
                p.Fill(Shapes.Poly(-25, -8, 25, -16, 25, -22, -25, -14), yel, 0, null, cone);
                p.Fill(Shapes.Poly(-25, -26, 25, -34, 25, -38, -25, -30), yel, 0, null, cone);
                p.Fill(cone, T, 4);
                p.Fill(Shapes.Circle(0, -46, 7), Palette.Hex("#3d8bff"), 3);
            });
            FlagPole = Art.Make(-6, -80, 6, 4, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(0, 0, 0, -70, 2.5f), Palette.Steel, 2.5f);
                p.Fill(Shapes.Circle(0, -73, 4.5f), Palette.Sun, 2);
            });
            FlagCloth = Art.Make(-3, -4, 56, 30, Vector2.zero, p =>
            {
                p.Fill(Shapes.Poly(0, 0, 50, 8, 0, 24), Palette.Hex("#ff4b4b"), 3);
                p.Fill(Shapes.Star(14, 11, 6), Color.white);
            });

            IconEyes = Art.Make(-32, -22, 32, 22, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(-15, 0, 14), Color.white, 3.5f); p.Fill(Shapes.Circle(15, 0, 14), Color.white, 3.5f);
                p.Fill(Shapes.Circle(-12, 2, 6.5f), ink); p.Fill(Shapes.Circle(18, 2, 6.5f), ink);
            });
            IconFlag = Art.Make(-24, -24, 24, 24, Vector2.zero, p =>
            {
                p.Fill(Shapes.Capsule(-14, 20, -14, -18, 2.5f), Palette.Steel, 2.5f);
                p.Fill(Shapes.Poly(-12, -19, 20, -10, -12, 1), Palette.Hex("#ff4b4b"), 3);
            });
            UiRingSel = Art.Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 45), Palette.Hex("#ffe27a"), 6));

            SkyBeach = Art.Make(0, 0, 4, 64, Vector2.zero, p =>
                p.SetPixels((x, y) => Color.Lerp(Palette.Hex("#fff1c9"), Palette.Hex("#5ec6ff"), (y + .5f) / p.H)));
            HillSea = Art.MakeHill(Art.HillFarPeriod, Art.HillFarWl, 14f, 56f, Palette.Hex("#42b9ea"));
            HillDunes = Art.MakeHill(Art.HillNearPeriod, Art.HillNearWl, 55f, 12f, Palette.Hex("#f7dfa0"));
        }
    }
}
