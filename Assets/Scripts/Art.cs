using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWheels
{
    /// <summary>
    /// Everything is drawn in "logical px" with y pointing DOWN (the same numbers the web prototype used).
    /// 100 logical px = 1 Unity unit. Sprites are rasterised at 2 texels per logical px.
    /// </summary>
    public struct Shape
    {
        public Func<Vector2, float> D;   // signed distance, negative inside (logical px)
        public Rect B;                   // bounds (logical px, y down)
        public Shape(Func<Vector2, float> d, Rect b) { D = d; B = b; }
    }

    public static class Shapes
    {
        public static Shape Circle(float cx, float cy, float r) =>
            new Shape(p => Vector2.Distance(p, new Vector2(cx, cy)) - r, new Rect(cx - r, cy - r, 2 * r, 2 * r));

        public static Shape Ellipse(float cx, float cy, float rx, float ry)
        {
            float m = Mathf.Min(rx, ry);
            return new Shape(p => (new Vector2((p.x - cx) / rx, (p.y - cy) / ry).magnitude - 1f) * m,
                new Rect(cx - rx, cy - ry, 2 * rx, 2 * ry));
        }

        public static Shape RRect(float x, float y, float w, float h, float r, float angleDeg = 0f)
        {
            float cx = x + w / 2, cy = y + h / 2, hw = w / 2 - r, hh = h / 2 - r;
            float a = -angleDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Func<Vector2, float> d = p =>
            {
                Vector2 q = p;
                if (angleDeg != 0f) q = new Vector2(ca * p.x - sa * p.y, sa * p.x + ca * p.y);
                float dx = Mathf.Abs(q.x - cx) - hw, dy = Mathf.Abs(q.y - cy) - hh;
                return new Vector2(Mathf.Max(dx, 0), Mathf.Max(dy, 0)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0) - r;
            };
            float ext = angleDeg == 0f ? 0f : Mathf.Max(w, h);
            return new Shape(d, new Rect(x - ext, y - ext, w + 2 * ext, h + 2 * ext));
        }

        public static Shape Poly(params float[] xy)
        {
            int n = xy.Length / 2;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                minX = Mathf.Min(minX, xy[2 * i]); maxX = Mathf.Max(maxX, xy[2 * i]);
                minY = Mathf.Min(minY, xy[2 * i + 1]); maxY = Mathf.Max(maxY, xy[2 * i + 1]);
            }
            Func<Vector2, float> d = p =>
            {
                float best = float.MaxValue; bool inside = false;
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    Vector2 a = new Vector2(xy[2 * i], xy[2 * i + 1]), b = new Vector2(xy[2 * j], xy[2 * j + 1]);
                    Vector2 e = b - a, w = p - a;
                    float t = Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-6f, e.sqrMagnitude));
                    best = Mathf.Min(best, (w - e * t).magnitude);
                    if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)) inside = !inside;
                }
                return inside ? -best : best;
            };
            return new Shape(d, new Rect(minX, minY, maxX - minX, maxY - minY));
        }

        public static Shape Capsule(float ax, float ay, float bx, float by, float halfWidth)
        {
            Vector2 a = new Vector2(ax, ay), b = new Vector2(bx, by), e = b - a;
            return new Shape(p =>
            {
                float t = Mathf.Clamp01(Vector2.Dot(p - a, e) / Mathf.Max(1e-6f, e.sqrMagnitude));
                return (p - a - e * t).magnitude - halfWidth;
            }, new Rect(Mathf.Min(ax, bx) - halfWidth, Mathf.Min(ay, by) - halfWidth, Mathf.Abs(bx - ax) + 2 * halfWidth, Mathf.Abs(by - ay) + 2 * halfWidth));
        }

        /// <summary>Thick arc (canvas angles, y down), round caps.</summary>
        public static Shape Arc(float cx, float cy, float r, float a0, float a1, float halfWidth)
        {
            Vector2 c = new Vector2(cx, cy);
            return new Shape(p =>
            {
                Vector2 v = p - c;
                float a = Mathf.Atan2(v.y, v.x);
                float d;
                if (a >= a0 && a <= a1) d = Mathf.Abs(v.magnitude - r);
                else
                {
                    Vector2 e0 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, e1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
                    d = Mathf.Min(Vector2.Distance(p, e0), Vector2.Distance(p, e1));
                }
                return d - halfWidth;
            }, new Rect(cx - r - halfWidth, cy - r - halfWidth, 2 * (r + halfWidth), 2 * (r + halfWidth)));
        }

        public static Shape Union(params Shape[] s)
        {
            Rect b = s[0].B;
            foreach (var x in s) { b.xMin = Mathf.Min(b.xMin, x.B.xMin); b.yMin = Mathf.Min(b.yMin, x.B.yMin); b.xMax = Mathf.Max(b.xMax, x.B.xMax); b.yMax = Mathf.Max(b.yMax, x.B.yMax); }
            return new Shape(p => { float m = float.MaxValue; foreach (var x in s) m = Mathf.Min(m, x.D(p)); return m; }, b);
        }

        public static Shape Star(float cx, float cy, float r)
        {
            float[] pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                float rad = i % 2 == 1 ? r * .45f : r, a = -Mathf.PI / 2 + i * Mathf.PI / 5;
                pts[2 * i] = cx + Mathf.Cos(a) * rad; pts[2 * i + 1] = cy + Mathf.Sin(a) * rad;
            }
            return Poly(pts);
        }
    }

    /// <summary>Tiny software rasteriser: anti-aliased filled + outlined shapes onto a texture.</summary>
    public class Painter
    {
        public const float Scale = 2f;
        readonly float x0, y0, x1, y1;
        readonly int w, h;
        readonly Color[] pix;
        readonly Vector2 pivotLogical;

        /// <param name="pivot">logical point that becomes the sprite pivot</param>
        public Painter(float x0, float y0, float x1, float y1, Vector2 pivot)
        {
            this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; pivotLogical = pivot;
            w = Mathf.CeilToInt((x1 - x0) * Scale); h = Mathf.CeilToInt((y1 - y0) * Scale);
            pix = new Color[w * h];
        }

        static float Cov(float dTexels) => Mathf.Clamp01(.5f - dTexels);

        public void Fill(Shape s, Color fill, float stroke = 0f, Color? strokeColor = null, Shape? clip = null)
        {
            float hw = stroke / 2f, pad = hw + 1.5f;
            int ix0 = Mathf.Max(0, Mathf.FloorToInt((s.B.xMin - pad - x0) * Scale)), ix1 = Mathf.Min(w - 1, Mathf.CeilToInt((s.B.xMax + pad - x0) * Scale));
            int iy0 = Mathf.Max(0, Mathf.FloorToInt((y1 - (s.B.yMax + pad)) * Scale)), iy1 = Mathf.Min(h - 1, Mathf.CeilToInt((y1 - (s.B.yMin - pad)) * Scale));
            Color sc = strokeColor ?? Palette.Ink;
            for (int py = iy0; py <= iy1; py++)
                for (int px = ix0; px <= ix1; px++)
                {
                    Vector2 p = new Vector2(x0 + (px + .5f) / Scale, y1 - (py + .5f) / Scale);
                    float d = s.D(p);
                    float cv = clip.HasValue ? Cov(clip.Value.D(p) * Scale) : 1f;
                    if (cv <= 0f) continue;
                    int i = py * w + px;
                    float fa = Cov(d * Scale) * fill.a * cv;
                    if (fa > 0f) pix[i] = Over(pix[i], fill, fa);
                    if (stroke > 0f)
                    {
                        float sa = Cov((Mathf.Abs(d) - hw) * Scale) * sc.a * cv;
                        if (sa > 0f) pix[i] = Over(pix[i], sc, sa);
                    }
                }
        }

        static Color Over(Color dst, Color src, float a)
        {
            float oa = a + dst.a * (1 - a);
            if (oa <= 0f) return dst;
            return new Color((src.r * a + dst.r * dst.a * (1 - a)) / oa, (src.g * a + dst.g * dst.a * (1 - a)) / oa,
                (src.b * a + dst.b * dst.a * (1 - a)) / oa, oa);
        }

        public void SetPixels(Func<int, int, Color> f)  // direct per-texel write (used for gradients and hills)
        {
            for (int py = 0; py < h; py++) for (int px = 0; px < w; px++) pix[py * w + px] = f(px, py);
        }
        public int W => w;
        public int H => h;
        public float Y1 => y1;

        public Sprite ToSprite()
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pix); tex.Apply(false, true);
            var pv = new Vector2((pivotLogical.x - x0) / (x1 - x0), (y1 - pivotLogical.y) / (y1 - y0));
            return Sprite.Create(tex, new Rect(0, 0, w, h), pv, 100f * Scale, 0, SpriteMeshType.FullRect);
        }
    }

    public static class Palette
    {
        public static readonly Color Ink = Hex("#2b2a4c"), Sun = Hex("#ffd23f"), Sky1 = Hex("#74c8ff"), Sky2 = Hex("#e6f7ff"),
            Hill1 = Hex("#a6e594"), Hill2 = Hex("#7fd56f"), Grass = Hex("#5cc45e"), Dirt = Hex("#b9794a"),
            DirtLt = Hex("#cf9560"), DirtDk = Hex("#8f5a34"), Steel = Hex("#9aa0b4"), Glass = Hex("#d8f1ff"), White = Color.white,
            Mud = Hex("#6b4428");
        public static readonly string[] Names = { "red", "orange", "yellow", "green", "blue", "purple", "pink" };
        public static readonly Color[] Truck =
        {
            Hex("#ff4b4b"), Hex("#ff9a1f"), Hex("#ffd23f"), Hex("#38c172"), Hex("#3d8bff"), Hex("#9b5cff"), Hex("#ff6eb4")
        };
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static Color Shade(Color c, float amt)
        {
            float f = amt < 0 ? 0 : 1, p = Mathf.Abs(amt);
            return new Color((f - c.r) * p + c.r, (f - c.g) * p + c.g, (f - c.b) * p + c.b, c.a);
        }
    }

    /// <summary>All the procedurally drawn sprites. Replace any of these with real art later.</summary>
    public static class Art
    {
        public static Sprite White, Dot, Square, Star, Bubble, Shadow, Sky;
        public static Sprite[] Body, Car, Balloon, SwatchNormal, SwatchSelected;
        public static Sprite Wheel, EyeWhite, Pupil, Mud, Ramp, Puddle, WashBack, Strip, SunNormal, SunHappy, SunRays, Cloud;
        public static Sprite SpeakerOn, SpeakerOff, PlayTriangle, UiRing, UiRingFilled, SlotEmpty, Roundrect;
        public static Sprite HillFar, HillNear;
        public const float HillFarWl = 150f, HillNearWl = 110f;
        public static float HillFarPeriod => 4f * Mathf.PI * HillFarWl;
        public static float HillNearPeriod => 4f * Mathf.PI * HillNearWl;
        static bool built;

        public static void Build()
        {
            if (built) return; built = true;
            var ink = Palette.Ink;

            White = Make(-1, -1, 1, 1, new Vector2(0, 0), p => p.Fill(Shapes.RRect(-1, -1, 2, 2, 0), Color.white));
            Dot = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 49.5f), Color.white));
            Square = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.RRect(-50, -50, 100, 100, 0), Color.white));
            Star = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Star(0, 0, 49f), Color.white));
            Bubble = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 46), new Color(1, 1, 1, .35f), 5, new Color(.31f, .67f, .9f, .9f)));
            Shadow = Dot;
            Roundrect = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.RRect(-50, -50, 100, 100, 50), Color.white));

            Sky = Make(0, 0, 4, 64, Vector2.zero, p => p.SetPixels((x, y) => Color.Lerp(Palette.Sky2, Palette.Sky1, (y + .5f) / p.H)));
            Sky.texture.wrapMode = TextureWrapMode.Clamp;

            EyeWhite = Make(-21, -21, 21, 21, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 17), Color.white, 3.5f));
            Pupil = Make(-10, -10, 10, 10, Vector2.zero, p =>
            {
                p.Fill(Shapes.Circle(0, 0, 8), ink);
                p.Fill(Shapes.Circle(3, -3, 2.6f), Color.white);
            });
            Mud = Make(-20, -20, 20, 20, Vector2.zero, p =>
            {
                p.Fill(Shapes.Ellipse(0, 0, 14, 11), Palette.Mud);
                p.Fill(Shapes.Circle(15, -6, 4), Palette.Mud);
            });

            Wheel = Make(-50, -50, 50, 50, Vector2.zero, p =>
            {
                var knob = new Color(.12f, .12f, .18f);
                for (int i = 0; i < 12; i++) p.Fill(Shapes.RRect(-6, -47, 12, 11, 3, i * 30f), knob);
                p.Fill(Shapes.Circle(0, 0, 41), Palette.Hex("#34344a"), 5);
                p.Fill(Shapes.Circle(0, 0, 22), Palette.Hex("#e7e9f5"), 4);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2 / 5;
                    p.Fill(Shapes.Circle(Mathf.Cos(a) * 11, Mathf.Sin(a) * 11, 2.6f), ink);
                }
            });

            int n = Palette.Truck.Length;
            Body = new Sprite[n]; Car = new Sprite[n]; Balloon = new Sprite[n]; SwatchNormal = new Sprite[n]; SwatchSelected = new Sprite[n];
            for (int i = 0; i < n; i++)
            {
                Color c = Palette.Truck[i], lt = Palette.Shade(c, .4f);
                Body[i] = Make(-128, -204, 128, -66, Vector2.zero, p =>
                {
                    p.Fill(Shapes.RRect(-106, -196, 12, 74, 5), Palette.Steel, 4);
                    p.Fill(Shapes.RRect(-90, -190, 12, 68, 5), Palette.Steel, 4);
                    p.Fill(Shapes.Poly(-80, -128, -58, -194, 40, -194, 62, -128), c, 5);
                    p.Fill(Shapes.Poly(-66, -134, -48, -186, 30, -186, 46, -134), Palette.Glass, 4);
                    var body = Shapes.RRect(-112, -128, 224, 56, 16);
                    p.Fill(body, c);
                    p.Fill(Shapes.RRect(-120, -108, 240, 12, 0), lt, 0, null, body);
                    p.Fill(body, new Color(0, 0, 0, 0), 5);
                    p.Fill(Shapes.Arc(58, -116, 18, .15f * Mathf.PI, .85f * Mathf.PI, 2.5f), ink);
                    p.Fill(Shapes.Circle(100, -118, 8), Palette.Hex("#fff6a8"), 4);
                    p.Fill(Shapes.RRect(104, -98, 14, 24, 5), Palette.Steel, 4);
                });
                Car[i] = Make(-74, -68, 74, 2, Vector2.zero, p => DrawCar(p, c));
                Balloon[i] = Make(-34, -38, 34, 82, Vector2.zero, p =>
                {
                    float r = 27;
                    for (int k = 0; k < 5; k++)
                        p.Fill(Shapes.Capsule(Mathf.Sin(k * 1.2f) * 5, r * 1.3f + k * 11, Mathf.Sin((k + 1) * 1.2f) * 5, r * 1.3f + (k + 1) * 11, 1.3f), new Color(ink.r, ink.g, ink.b, .65f));
                    p.Fill(Shapes.Poly(0, r * 1.15f, -6, r * 1.38f, 6, r * 1.38f), c, 3);
                    p.Fill(Shapes.Ellipse(0, 0, r, r * 1.18f), c, 4);
                    p.Fill(Shapes.Ellipse(-r * .35f, -r * .45f, r * .18f, r * .3f), new Color(1, 1, 1, .55f));
                });
                SwatchNormal[i] = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 44), c, 10, Color.white));
                SwatchSelected[i] = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 44), c, 10, ink));
            }

            Ramp = Make(-4, -86, 184, 4, Vector2.zero, p =>
            {
                var tri = Shapes.Poly(0, 0, 180, 0, 180, -80);
                p.Fill(tri, Palette.Hex("#ffb347"), 5);
                for (int i = 0; i < 5; i++)
                    p.Fill(Shapes.Poly(20 + 38 * i, 0, 44 + 38 * i, 0, 64 + 38 * i, -80, 40 + 38 * i, -80), Color.white, 0, null, tri);
                p.Fill(tri, new Color(0, 0, 0, 0), 5);
            });
            Puddle = Make(-114, -6, 114, 34, Vector2.zero, p =>
            {
                p.Fill(Shapes.Ellipse(0, 8, 110, 20), Palette.Mud, 4);
                p.Fill(Shapes.Ellipse(0, 6, 98, 13), Palette.Hex("#7e5233"));
                p.Fill(Shapes.Ellipse(-40, 3, 30, 4), new Color(1, 1, 1, .35f));
            });
            WashBack = Make(-176, -306, 176, 6, Vector2.zero, p =>
            {
                var teal = Palette.Hex("#2bb3b3");
                p.Fill(Shapes.RRect(-164, -262, 28, 262, 6), teal, 5);
                p.Fill(Shapes.RRect(136, -262, 28, 262, 6), teal, 5);
                p.Fill(Shapes.RRect(-170, -300, 340, 60, 14), teal, 5);
                for (int i = 0; i < 4; i++) p.Fill(Shapes.Circle(-105 + i * 70, -270, 12 - (i % 2) * 4), Color.white, 3);
                var roller = Shapes.RRect(-140, -236, 280, 26, 13);
                p.Fill(roller, Palette.Hex("#ff6eb4"), 0);
                for (int i = 0; i < 9; i++)
                    p.Fill(Shapes.Poly(-140 + i * 36, -210, -126 + i * 36, -210, -110 + i * 36, -236, -124 + i * 36, -236), Color.white, 0, null, roller);
                p.Fill(roller, new Color(0, 0, 0, 0), 5);
            });
            Strip = Make(-8, 0, 8, 200, new Vector2(0, 0), p => p.Fill(Shapes.RRect(-7, 0, 14, 200, 7), Color.white));

            SunNormal = Make(-54, -54, 54, 54, Vector2.zero, p => DrawSun(p, false));
            SunHappy = Make(-54, -54, 54, 54, Vector2.zero, p => DrawSun(p, true));
            SunRays = Make(-84, -84, 84, 84, Vector2.zero, p =>
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    Vector2 R(float x, float y) => new Vector2(x * ca - y * sa, x * sa + y * ca);
                    Vector2 a1 = R(-9, -56), a2 = R(9, -56), a3 = R(0, -80);
                    p.Fill(Shapes.Poly(a1.x, a1.y, a2.x, a2.y, a3.x, a3.y), Palette.Sun);
                }
            });
            Cloud = Make(-60, -52, 150, 36, new Vector2(0, 0), p =>
            {
                float s = 60;
                var u = Shapes.Union(Shapes.Circle(0, 0, s * .6f), Shapes.Circle(s * .6f, -s * .25f, s * .7f), Shapes.Circle(s * 1.3f, 0, s * .55f),
                    Shapes.RRect(-s * .5f, 0, s * 2.3f, s * .5f, s * .25f));
                p.Fill(u, new Color(1, 1, 1, .92f));
            });

            HillFar = MakeHill(HillFarPeriod, HillFarWl, 130f, 56f, Palette.Hill1);
            HillNear = MakeHill(HillNearPeriod, HillNearWl, 80f, 12f, Palette.Hill2);

            SpeakerOn = Make(-12, -12, 12, 12, Vector2.zero, p => DrawSpeaker(p, true));
            SpeakerOff = Make(-12, -12, 12, 12, Vector2.zero, p => DrawSpeaker(p, false));
            PlayTriangle = Make(-12, -12, 12, 12, Vector2.zero, p => p.Fill(Shapes.Poly(-6, -8.5f, -6, 8.5f, 9, 0), Palette.Hex("#38c172"), 2.4f));
            UiRing = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 45), new Color(1, 1, 1, .55f), 4));
            UiRingFilled = Make(-50, -50, 50, 50, Vector2.zero, p => p.Fill(Shapes.Circle(0, 0, 45), Color.white, 4));
            SlotEmpty = UiRing;
        }

        static void DrawCar(Painter p, Color col)
        {
            var wl = Palette.Hex("#2c2c3e"); float lw = 3.5f;
            foreach (float x in new[] { -38f, 38f })
            {
                p.Fill(Shapes.Circle(x, -13, 13), wl, lw);
                p.Fill(Shapes.Circle(x, -13, 5), Palette.Hex("#d9dbe8"));
            }
            p.Fill(Shapes.Poly(-34, -40, -22, -62, 26, -62, 40, -40), col, lw);
            p.Fill(Shapes.Poly(-27, -42, -18, -57, 22, -57, 32, -42), Palette.Glass, lw * .8f);
            foreach (float x in new[] { -8f, 10f })
            {
                p.Fill(Shapes.Circle(x, -50, 5.5f), Color.white, lw * .6f);
                p.Fill(Shapes.Circle(x + 1, -50, 2.4f), Palette.Ink);
            }
            p.Fill(Shapes.RRect(-60, -40, 120, 28, 10), col, lw);
            p.Fill(Shapes.Circle(52, -30, 5), Palette.Hex("#fff6a8"), lw * .7f);
        }

        static void DrawSun(Painter p, bool happy)
        {
            p.Fill(Shapes.Circle(0, 0, 48), Palette.Sun, 5);
            p.Fill(Shapes.Ellipse(-16, -8, 5, 7), Palette.Ink);
            p.Fill(Shapes.Ellipse(16, -8, 5, 7), Palette.Ink);
            if (happy)
            {
                var half = Shapes.Circle(0, 8, 18);
                p.Fill(half, Palette.Hex("#ff6e6e"), 4, null, new Shape(q => 8 - q.y, new Rect(-30, 8, 60, 30)));
                p.Fill(Shapes.Capsule(-18, 8, 18, 8, 2f), Palette.Ink);
            }
            else p.Fill(Shapes.Arc(0, 6, 18, .2f * Mathf.PI, .8f * Mathf.PI, 2.5f), Palette.Ink);
            p.Fill(Shapes.Circle(-28, 8, 6), new Color(1, .43f, .43f, .45f));
            p.Fill(Shapes.Circle(28, 8, 6), new Color(1, .43f, .43f, .45f));
        }

        static void DrawSpeaker(Painter p, bool on)
        {
            var ink = Palette.Ink;
            p.Fill(Shapes.Poly(-8, -3, -4, -3, 1, -7, 1, 7, -4, 3, -8, 3), on ? Palette.Sun : Palette.Hex("#d9dbe8"), 1.6f);
            if (on)
            {
                p.Fill(Shapes.Arc(1, 0, 5, -.9f, .9f, .9f), ink);
                p.Fill(Shapes.Arc(1, 0, 9, -.9f, .9f, .9f), ink);
            }
            else
            {
                p.Fill(Shapes.Capsule(5, -3, 10, 3, 1f), ink);
                p.Fill(Shapes.Capsule(10, -3, 5, 3, 1f), ink);
            }
        }

        static Sprite MakeHill(float period, float wl, float amp, float extra, Color col)
        {
            int w = Mathf.CeilToInt(period), h = Mathf.CeilToInt(amp + extra);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float a = x / wl;
                float top = amp * (.6f + .4f * Mathf.Sin(a)) * (.7f + .3f * Mathf.Sin(a / 2.3f * 2.3f * .5f + 1f));
                // y measured up from the bottom edge: bottom edge is `extra` px below the curve base line
                float curve = extra + top;
                for (int y = 0; y < h; y++)
                {
                    float cov = Mathf.Clamp01(curve - y + .5f);
                    px[y * w + x] = new Color(col.r, col.g, col.b, cov);
                }
            }
            tex.SetPixels(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0), 100f, 0, SpriteMeshType.FullRect);
        }

        static Sprite Make(float x0, float y0, float x1, float y1, Vector2 pivot, Action<Painter> draw)
        {
            var p = new Painter(x0, y0, x1, y1, pivot);
            draw(p);
            return p.ToSprite();
        }
    }
}
