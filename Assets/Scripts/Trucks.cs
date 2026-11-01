using UnityEngine;

namespace BigWheels
{
    /// <summary>Everything that differs between truck types. Anchor points are in truck coords: origin = ground contact, y down.</summary>
    public class TruckDef
    {
        public string Name;
        public Sprite[] Body, Arm, Lid;
        public float PivotY, WheelR, Height, HalfW, EyeScale;
        public float[] WheelX;
        public Vector2[] Eyes;
        public Vector2 Puff, Hat, Flag, Joint, Nozzle, Hinge, Mouth, Beacon, MudMin, MudMax;
    }

    public static class Trucks
    {
        public const int Monster = 0, Digger = 1, Fire = 2, Garbage = 3;
        public static TruckDef[] All;
        static readonly Color T = new Color(0, 0, 0, 0);

        public static void Build()
        {
            if (All != null) return;
            int n = Palette.Truck.Length;
            var ink = Palette.Ink;

            var monster = new TruckDef
            {
                Name = "monster", Body = Art.Body, PivotY = 90, WheelR = 46, WheelX = new[] { -78f, 78f }, EyeScale = 1f,
                Eyes = new[] { new Vector2(-26, -160), new Vector2(12, -160) }, Height = 195, HalfW = 105,
                Puff = new Vector2(-98, -195), Hat = new Vector2(-9, -194), Flag = new Vector2(-100, -196),
                MudMin = new Vector2(-100, -190), MudMax = new Vector2(100, -78)
            };

            // ---- digger: yellow-ish boom arm that swings and dumps dirt ----
            var digBody = new Sprite[n]; var digArm = new Sprite[n];
            for (int i = 0; i < n; i++)
            {
                Color c = Palette.Truck[i], dk = Palette.Shade(c, -.28f), lt = Palette.Shade(c, .4f), arm = Palette.Shade(c, -.1f);
                digBody[i] = Art.Make(-112, -158, 112, -30, Vector2.zero, p =>
                {
                    p.Fill(Shapes.RRect(-104, -112, 50, 30, 8), dk, 4);
                    p.Fill(Shapes.Poly(-62, -96, -54, -152, 8, -152, 18, -96), c, 5);
                    p.Fill(Shapes.Poly(-50, -102, -44, -144, 2, -144, 8, -102), Palette.Glass, 4);
                    var body = Shapes.RRect(-100, -96, 196, 46, 12);
                    p.Fill(body, c);
                    p.Fill(Shapes.RRect(-104, -76, 204, 8, 0), lt, 0, null, body);
                    p.Fill(body, T, 5);
                    p.Fill(Shapes.Circle(62, -100, 9), Palette.Steel, 4);
                    p.Fill(Shapes.Circle(90, -78, 6), Palette.Hex("#fff6a8"), 3.5f);
                    p.Fill(Shapes.Arc(46, -80, 11, .15f * Mathf.PI, .85f * Mathf.PI, 2.2f), ink);
                });
                digArm[i] = Art.Make(-14, -96, 152, 62, Vector2.zero, p =>
                {
                    p.Fill(Shapes.Capsule(0, 0, 70, -45, 9), arm, 4);
                    p.Fill(Shapes.Capsule(70, -45, 112, 14, 8), arm, 4);
                    p.Fill(Shapes.Poly(98, 6, 138, 0, 144, 42, 102, 46), Palette.Steel, 4);
                    p.Fill(Shapes.Circle(0, 0, 7), Palette.Steel, 3.5f);
                    p.Fill(Shapes.Circle(70, -45, 6), Palette.Steel, 3);
                });
            }
            var digger = new TruckDef
            {
                Name = "digger", Body = digBody, Arm = digArm, PivotY = 70, WheelR = 30, WheelX = new[] { -62f, 62f }, EyeScale = 11f / 17f,
                Eyes = new[] { new Vector2(-34, -122), new Vector2(-10, -122) }, Height = 160, HalfW = 105,
                Joint = new Vector2(62, -100), Hat = new Vector2(-22, -152), Flag = new Vector2(-90, -112), Puff = new Vector2(-96, -100),
                MudMin = new Vector2(-90, -92), MudMax = new Vector2(90, -56)
            };

            // ---- fire truck: ladder, beacon, water cannon ----
            var fireBody = new Sprite[n];
            for (int i = 0; i < n; i++)
            {
                Color c = Palette.Truck[i], lt = Palette.Shade(c, .55f);
                fireBody[i] = Art.Make(-132, -178, 134, -34, Vector2.zero, p =>
                {
                    var body = Shapes.RRect(-122, -100, 244, 56, 10);
                    p.Fill(body, c);
                    p.Fill(Shapes.RRect(-130, -80, 270, 9, 0), lt, 0, null, body);
                    p.Fill(body, T, 5);
                    // ladder on the roof
                    foreach (float x in new[] { -100f, 30f }) p.Fill(Shapes.Capsule(x, -100, x, -112, 2.5f), Palette.Steel, 2.5f);
                    p.Fill(Shapes.Capsule(-112, -112, 40, -112, 3), Palette.Steel, 2.5f);
                    p.Fill(Shapes.Capsule(-112, -124, 40, -124, 3), Palette.Steel, 2.5f);
                    for (int k = 0; k < 8; k++) p.Fill(Shapes.Capsule(-102 + k * 20, -112, -102 + k * 20, -124, 2), Palette.Steel, 2f);
                    // cab
                    p.Fill(Shapes.RRect(52, -148, 72, 102, 12), c, 5);
                    p.Fill(Shapes.RRect(60, -140, 50, 30, 8), Palette.Glass, 4);
                    p.Fill(Shapes.Arc(92, -82, 12, .15f * Mathf.PI, .85f * Mathf.PI, 2.4f), ink);
                    p.Fill(Shapes.Circle(121, -64, 6), Palette.Hex("#fff6a8"), 3.5f);
                    p.Fill(Shapes.RRect(120, -54, 12, 16, 4), Palette.Steel, 3.5f);
                    p.Fill(Shapes.RRect(86, -157, 28, 9, 3), Palette.Steel, 3);
                    // water cannon on the roof
                    p.Fill(Shapes.Capsule(36, -102, 52, -124, 5), Palette.Steel, 3);
                });
            }
            var fire = new TruckDef
            {
                Name = "fire", Body = fireBody, PivotY = 75, WheelR = 34, WheelX = new[] { -82f, 78f }, EyeScale = 10f / 17f,
                Eyes = new[] { new Vector2(77, -125), new Vector2(97, -125) }, Height = 165, HalfW = 125,
                Nozzle = new Vector2(54, -127), Beacon = new Vector2(100, -164), Hat = new Vector2(70, -148), Flag = new Vector2(-118, -112),
                Puff = new Vector2(-124, -60), MudMin = new Vector2(-110, -96), MudMax = new Vector2(40, -52)
            };

            // ---- garbage truck: hopper lid opens and eats flattened cars ----
            var garBody = new Sprite[n]; var garLid = new Sprite[n];
            for (int i = 0; i < n; i++)
            {
                Color c = Palette.Truck[i], lt = Palette.Shade(c, .35f), dk = Palette.Shade(c, -.3f);
                garBody[i] = Art.Make(-132, -152, 134, -34, Vector2.zero, p =>
                {
                    var hop = Shapes.RRect(-122, -130, 152, 86, 12);
                    p.Fill(hop, c);
                    for (int k = 0; k < 4; k++) p.Fill(Shapes.Capsule(-96 + 34 * k, -122, -96 + 34 * k, -52, 2.5f), lt, 0, null, hop);
                    p.Fill(hop, T, 5);
                    p.Fill(Shapes.RRect(-126, -80, 18, 30, 5), Palette.Hex("#1d1d33"), 3);
                    p.Fill(Shapes.RRect(34, -118, 88, 74, 14), c, 5);
                    p.Fill(Shapes.Poly(48, -108, 90, -108, 108, -82, 48, -82), Palette.Glass, 4);
                    p.Fill(Shapes.Arc(84, -62, 10, .15f * Mathf.PI, .85f * Mathf.PI, 2.2f), ink);
                    p.Fill(Shapes.Circle(114, -58, 6), Palette.Hex("#fff6a8"), 3.5f);
                    p.Fill(Shapes.RRect(118, -54, 12, 18, 4), Palette.Steel, 3.5f);
                });
                garLid[i] = Art.Make(-6, -24, 118, 8, Vector2.zero, p => p.Fill(Shapes.RRect(0, -14, 112, 14, 6), dk, 4));
            }
            var garbage = new TruckDef
            {
                Name = "garbage", Body = garBody, Lid = garLid, PivotY = 70, WheelR = 32, WheelX = new[] { -78f, 78f }, EyeScale = 10f / 17f,
                Eyes = new[] { new Vector2(62, -95), new Vector2(84, -95) }, Height = 140, HalfW = 125,
                Hinge = new Vector2(-118, -130), Mouth = new Vector2(-100, -136), Hat = new Vector2(78, -118), Flag = new Vector2(-20, -130),
                Puff = new Vector2(-124, -60), MudMin = new Vector2(-110, -122), MudMax = new Vector2(20, -50)
            };

            All = new[] { monster, digger, fire, garbage };
        }
    }
}
