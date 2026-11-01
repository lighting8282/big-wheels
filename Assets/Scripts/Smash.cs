using UnityEngine;

namespace BigWheels
{
    /// <summary>Sprites for the smashable props (crates, brick wall, bowling pins, explosive barrels, glass, fence), their flying pieces and the turbo flame.</summary>
    public static class SmashArt
    {
        public static Sprite Crates, Wall, Pins, Barrels, Glass, Fence, CrateBit, Plank, Brick, Pin, Shard, Flame, FlameBlob;
        static bool built;
        static readonly Color Clear = new Color(0, 0, 0, 0);

        static void DrawCrate(Painter p, float cx, float top, float s)
        {
            Color wood = Palette.Hex("#d9a05b"), dk = Palette.Hex("#a96f32"), nail = Palette.Hex("#6b4220");
            var box = Shapes.RRect(cx - s / 2, top, s, s, 5);
            p.Fill(box, wood, 3.5f);
            p.Fill(Shapes.RRect(cx - s / 2 + 7, top + 7, s - 14, s - 14, 2), Clear, 3, dk);
            p.Fill(Shapes.Capsule(cx - s / 2 + 10, top + s - 10, cx + s / 2 - 10, top + 10, 2.6f), dk);
            foreach (var (dx, dy) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                p.Fill(Shapes.Circle(cx + dx * (s / 2 - 5), top + s / 2 + dy * (s / 2 - 5), 1.8f), nail);
        }

        static void DrawPin(Painter p, float cx, float by, float k)
        {
            var pin = Shapes.Union(Shapes.Circle(cx, by - 80 * k, 10 * k), Shapes.Capsule(cx, by - 72 * k, cx, by - 58 * k, 6.5f * k), Shapes.Ellipse(cx, by - 34 * k, 16 * k, 31 * k));
            p.Fill(pin, Color.white, 3.2f);
            Color red = Palette.Truck[0];
            p.Fill(Shapes.RRect(cx - 14 * k, by - 70 * k, 28 * k, 4.5f * k, 0), red, 0, null, pin);
            p.Fill(Shapes.RRect(cx - 14 * k, by - 62 * k, 28 * k, 4.5f * k, 0), red, 0, null, pin);
            p.Fill(pin, Clear, 3.2f);
        }

        static void DrawBarrel(Painter p, float cx)
        {
            Color red = Palette.Hex("#d33a2c"), dk = Palette.Shade(red, -.3f);
            var b = Shapes.RRect(cx - 27, -82, 54, 80, 12);
            p.Fill(b, red, 3.6f);
            p.Fill(Shapes.RRect(cx - 30, -66, 60, 9, 0), dk, 0, null, b);
            p.Fill(Shapes.RRect(cx - 30, -26, 60, 9, 0), dk, 0, null, b);
            p.Fill(Shapes.RRect(cx - 21, -76, 8, 66, 3), new Color(1, 1, 1, .3f), 0, null, b);
            p.Fill(Shapes.Circle(cx, -45, 14), Color.white, 2.6f);
            p.Fill(Shapes.Union(Shapes.Circle(cx, -42, 8), Shapes.Poly(cx - 7, -46, cx, -62, cx + 7, -46)), Palette.Hex("#ff8a1f"));
            p.Fill(b, Clear, 3.6f);
        }

        public static void Build()
        {
            if (built) return; built = true;
            Color wood = Palette.Hex("#d9a05b"), dk = Palette.Hex("#a96f32");

            // two crates with one on top, standing on the ground (origin = bottom centre)
            Crates = Art.Make(-68, -132, 68, 4, Vector2.zero, p =>
            {
                DrawCrate(p, -32, -62, 62); DrawCrate(p, 32, -62, 62); DrawCrate(p, 0, -124, 62);
                p.Grunge(31, .45f, .3f);
            });

            // six rows of bricks, every other row shifted half a brick
            Wall = Art.Make(-66, -124, 66, 4, Vector2.zero, p =>
            {
                Color[] tones = { Palette.Hex("#d9644a"), Palette.Hex("#c4513a"), Palette.Hex("#e07a5b") };
                for (int r = 0; r < 6; r++)
                {
                    float y = -20 * (r + 1), x = -60;
                    bool odd = r % 2 == 1;
                    int i = 0;
                    while (x < 60 - .5f)
                    {
                        float w = (odd && (i == 0)) ? 20 : 40;
                        if (x + w > 60) w = 60 - x;
                        p.Fill(Shapes.RRect(x + 1, y + 1, w - 2, 18, 3), tones[(r * 5 + i * 3) % 3], 2.6f);
                        x += w; i++;
                    }
                }
                p.Grunge(37, .45f, .2f);
            });

            // bowling pins: three standing a little behind, four in front
            Pins = Art.Make(-82, -100, 82, 4, Vector2.zero, p =>
            {
                foreach (float x in new[] { -40f, 0f, 40f }) DrawPin(p, x, -6, .9f);
                foreach (float x in new[] { -60f, -20f, 20f, 60f }) DrawPin(p, x, 0, 1f);
            });

            // two red explosive barrels with a flame badge
            Barrels = Art.Make(-70, -86, 70, 4, Vector2.zero, p => { DrawBarrel(p, -34); DrawBarrel(p, 34); p.Grunge(43, .5f, .5f); });

            // a glass wall: three panes in a frame, with shine
            Glass = Art.Make(-82, -118, 82, 4, Vector2.zero, p =>
            {
                Color frame = Palette.Hex("#5a6070"), pane = new Color(.75f, .91f, 1f, .6f);
                foreach (float x in new[] { -52f, 0f, 52f })
                {
                    var s = Shapes.RRect(x - 23, -108, 46, 106, 5);
                    p.Fill(s, pane, 3.2f);
                    p.Fill(Shapes.Capsule(x - 12, -90, x + 8, -50, 2.6f), new Color(1, 1, 1, .8f), 0, null, s);
                    p.Fill(Shapes.Capsule(x - 4, -96, x + 14, -66, 1.6f), new Color(1, 1, 1, .7f), 0, null, s);
                }
                p.Fill(Shapes.RRect(-78, -112, 156, 8, 3), frame, 2.6f);
                p.Fill(Shapes.RRect(-78, -8, 156, 10, 3), frame, 2.6f);
                foreach (float x in new[] { -78f, 78f }) p.Fill(Shapes.RRect(x - 4, -112, 8, 114, 3), frame, 2.6f);
            });

            // a white picket fence
            Fence = Art.Make(-82, -76, 82, 4, Vector2.zero, p =>
            {
                Color rail = Palette.Hex("#d9cfb8"), picket = Palette.Hex("#f2ead8");
                p.Fill(Shapes.RRect(-78, -50, 156, 8, 2), rail, 2.6f);
                p.Fill(Shapes.RRect(-78, -22, 156, 8, 2), rail, 2.6f);
                for (int i = 0; i < 7; i++)
                {
                    float x = -66 + i * 22;
                    p.Fill(Shapes.Poly(x - 9, 0, x - 9, -58, x, -70, x + 9, -58, x + 9, 0), picket, 3f);
                }
            });

            // pieces that fly off (origin = centre). White ones are tinted when they are spawned.
            CrateBit = Art.Make(-14, -14, 14, 14, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-12, -12, 24, 24, 3), wood, 3);
                p.Fill(Shapes.Capsule(-8, 8, 8, -8, 2f), dk);
            });
            Plank = Art.Make(-26, -7, 26, 7, Vector2.zero, p =>
            {
                p.Fill(Shapes.RRect(-23, -5, 46, 10, 2), Color.white, 2.8f);
                p.Fill(Shapes.Circle(-17, 0, 1.8f), Palette.Hex("#6b4220"));
            });
            Brick = Art.Make(-23, -13, 23, 13, Vector2.zero, p => p.Fill(Shapes.RRect(-20, -10, 40, 20, 3), Color.white, 2.8f));
            Pin = Art.Make(-20, -52, 20, 52, Vector2.zero, p => DrawPin(p, 0, 48, 1f));
            Shard = Art.Make(-14, -14, 14, 14, Vector2.zero, p => p.Fill(Shapes.Poly(-11, 9, 12, 7, -1, -12), Color.white, 1.8f, new Color(1, 1, 1, .9f)));

            // turbo flame: icon with an inner flame, and a plain white teardrop to tint for the trail
            Flame = Art.Make(-22, -40, 22, 22, Vector2.zero, p =>
            {
                p.Fill(Shapes.Union(Shapes.Circle(0, 0, 17), Shapes.Poly(-15, -8, 1, -38, 15, -8)), Palette.Hex("#ff8a1f"), 3.2f);
                p.Fill(Shapes.Union(Shapes.Circle(0, 5, 9), Shapes.Poly(-8, 0, 1, -20, 8, 0)), Palette.Hex("#ffe45e"));
            });
            FlameBlob = Art.Make(-14, -30, 14, 20, Vector2.zero, p =>
                p.Fill(Shapes.Union(Shapes.Circle(0, 6, 12), Shapes.Poly(-11, 2, 0, -27, 11, 2)), Color.white));
        }
    }

    // Smash props, debris, chain explosions, the turbo meter and the screen shake. The rest of the game stays untouched.
    public partial class Game
    {
        const int PCrate = 5, PPlank = 6, PBrick = 7, PPin = 8, PFlame = 9, PStreak = 10, PShard = 11;
        const float TurboSeconds = 6f;
        Pool pCrate, pPlank, pBrick, pPin, pFlame, pShard;
        readonly SpriteRenderer[] pips = new SpriteRenderer[4];
        int charge; float turboT, spdMul = 1f, shakeMag, hitStop, flameT, streakT;

        /// <summary>True while the flame boost is on.</summary>
        public bool Turbo => turboT > 0;
        public int Charge => charge;
        public float SpeedMul => spdMul;
        public void DebugTurbo() { if (!Started) StartGame(); StartTurbo(); }

        void SmashInit()
        {
            pFlame = new Pool(this, SmashArt.FlameBlob, 19, worldRoot);
            pCrate = new Pool(this, SmashArt.CrateBit, 24, worldRoot); pPlank = new Pool(this, SmashArt.Plank, 25, worldRoot);
            pBrick = new Pool(this, SmashArt.Brick, 26, worldRoot); pPin = new Pool(this, SmashArt.Pin, 27, worldRoot);
            pShard = new Pool(this, SmashArt.Shard, 28, worldRoot);
            for (int i = 0; i < pips.Length; i++) pips[i] = MakeSR("Pip", SmashArt.Flame, 30, worldRoot);
        }

        // ---------- the smashable props ----------
        static bool IsSmashable(char t) => "kmnxgf".IndexOf(t) >= 0;

        Sprite SmashSprite(char type) =>
            type == 'k' ? SmashArt.Crates : type == 'm' ? SmashArt.Wall : type == 'n' ? SmashArt.Pins :
            type == 'x' ? SmashArt.Barrels : type == 'g' ? SmashArt.Glass : SmashArt.Fence;

        static float SmashTop(char t) => t == 'k' ? 128f : t == 'm' ? 124f : t == 'n' ? 100f : t == 'x' ? 86f : t == 'g' ? 116f : 74f;

        /// <summary>Called every step for a smashable prop: it bursts as soon as the truck reaches it.</summary>
        void StepSmash(Obj o, float cx)
        {
            if (o.dead) return;
            bool tb = turboT > 0;
            float reach = o.w / 2 + Def.HalfW * .8f + (tb ? 130f : 0f);
            if (Mathf.Abs(cx - o.x) < reach && truck.h < SmashTop(o.t) + (tb ? 140f : 0f)) Smash(o, cx);
        }

        void Smash(Obj o, float cx)
        {
            if (o.dead) return;
            o.dead = true;
            bool tb = turboT > 0;
            float spd = Speed * spdMul, k = tb ? 1.35f : 1f;
            Color wood = Palette.Hex("#d9a05b");
            int pts = 100;
            switch (o.t)
            {
                case 'k':
                    Sfx.Play("crash");
                    foreach (var (dx, dy) in new[] { (-32f, -31f), (32f, -31f), (0f, -93f) })
                        for (int i = 0; i < 2; i++) Debris(PCrate, o.x + dx, BASE + dy, spd, k, Random.Range(.75f, 1.15f), Color.white);
                    for (int i = 0; i < 16; i++) Debris(PPlank, o.x + Random.Range(-60f, 60f), BASE - Random.Range(10f, 110f), spd, k, Random.Range(.7f, 1.2f), wood);
                    break;
                case 'm':
                    Sfx.Play("crunch"); Award(29); pts = 150;
                    {
                        Color[] tones = { Palette.Hex("#ff8a6a"), Palette.Hex("#ee6a4c"), Palette.Hex("#ffb08a") };
                        for (int i = 0; i < 26; i++)
                            Debris(PBrick, o.x + Random.Range(-56f, 56f), BASE - Random.Range(10f, 114f), spd, k, Random.Range(.7f, 1.05f), tones[Random.Range(0, 3)]);
                    }
                    break;
                case 'n':
                    Sfx.Play("tires"); pts = 150;
                    for (int i = 0; i < 7; i++)
                        Debris(PPin, o.x + Random.Range(-64f, 64f), BASE - Random.Range(20f, 70f), spd, k * 1.15f, Random.Range(.8f, 1f), Color.white, 1.25f);
                    break;
                case 'g':
                    Sfx.Play("shatter"); pts = 120;
                    {
                        Color[] glass = { Palette.Hex("#bfe8ff"), Palette.Hex("#e8f7ff"), Palette.Hex("#9fd8ff") };
                        for (int i = 0; i < 30; i++)
                            Debris(PShard, o.x + Random.Range(-70f, 70f), BASE - Random.Range(10f, 108f), spd, k * 1.1f, Random.Range(.7f, 1.3f), glass[Random.Range(0, 3)]);
                        Stars(o.x, BASE - 70, 12, new[] { Color.white, glass[0] }, 340);
                    }
                    break;
                case 'f':
                    Sfx.Play("snap"); pts = 100;
                    for (int i = 0; i < 16; i++)
                        Debris(PPlank, o.x + Random.Range(-70f, 70f), BASE - Random.Range(8f, 70f), spd, k, Random.Range(.6f, 1f), new Color(1f, .97f, .88f));
                    break;
                default: // 'x': explosive barrels
                    Sfx.Play("boom"); pts = 250;
                    for (int i = 0; i < 26; i++)
                        AddPart(PFlame, o.x + Random.Range(-50f, 50f), BASE - Random.Range(10f, 90f), spd * .5f + Random.Range(-260f, 260f), Random.Range(-520f, -120f), -160, Random.Range(20f, 40f), 0, 0, Random.Range(.5f, .95f), Color.white);
                    for (int i = 0; i < 10; i++)
                        AddPart(4, o.x + Random.Range(-60f, 60f), BASE - Random.Range(30f, 110f), spd * .4f + Random.Range(-60f, 120f), Random.Range(-200f, -40f), -40, Random.Range(26f, 46f), 0, 0, Random.Range(.9f, 1.5f), new Color(.28f, .28f, .32f));
                    for (int i = 0; i < 12; i++)
                        Debris(PPlank, o.x + Random.Range(-60f, 60f), BASE - Random.Range(10f, 90f), spd, k * 1.3f, Random.Range(.7f, 1.1f), new Color(.85f, .3f, .24f), 1.3f);
                    Pow(o.x - 30, BASE - 80); Pow(o.x + 40, BASE - 50);
                    break;
            }
            float px = cx + Def.HalfW * .7f;
            Pow(px, BASE - 70);
            for (int i = 0; i < 6; i++) AddPart(4, o.x + Random.Range(-50f, 50f), BASE - Random.Range(10f, 60f), spd * .5f + Random.Range(-40f, 120f), Random.Range(-140f, -20f), -30, Random.Range(14f, 26f), 0, 0, Random.Range(.5f, .9f), new Color(.87f, .82f, .74f));
            bool boom = o.t == 'x';
            AddShake(boom ? 15f : tb ? 9f : 6f);
            hitStop = boom ? .12f : tb ? .05f : .09f;
            truck.bobV += boom ? 520 : 380;
            AddScore(pts, o.x, BASE - 110);
            FeedCharge();
            if (boom) Chain(o, cx);
        }

        /// <summary>An explosion sets off everything standing close by.</summary>
        void Chain(Obj from, float cx)
        {
            foreach (var other in objs)
            {
                if (other.dead || other == from || Mathf.Abs(other.x - from.x) > 330f) continue;
                if (IsSmashable(other.t)) Smash(other, cx);
                else if (other.t == 'c' && other.s == 0) CrushCar(other);
            }
        }

        void Debris(int kind, float x, float y, float spd, float k, float scale, Color col, float up = 1f)
        {
            float vx = spd * .9f + Random.Range(-120f, 520f) * k, vy = Random.Range(-780f, -200f) * k * up;
            AddPart(kind, x, y, vx, vy, 1900, scale, Random.Range(0, Tau), Random.Range(-15f, 15f), Random.Range(3f, 4.4f), col);
        }

        /// <summary>A cartoon impact star.</summary>
        void Pow(float x, float y)
        {
            AddPart(1, x, y, 0, 0, 0, 64, Random.Range(0, Tau), 3f, .28f, Palette.Hex("#ff7a1a"));
            AddPart(1, x, y, 0, 0, 0, 46, Random.Range(0, Tau), -2f, .26f, Palette.Sun);
            AddPart(1, x, y, 0, 0, 0, 26, Random.Range(0, Tau), 0f, .24f, Color.white);
        }

        void AddShake(float m) { shakeMag = Mathf.Max(shakeMag, m); }

        // ---------- turbo ----------
        /// <summary>Every smash or crushed car lights one of four flames; the fourth starts the boost.</summary>
        void FeedCharge()
        {
            if (turboT > 0 || !Started) return;
            if (++charge >= pips.Length) { charge = pips.Length; StartTurbo(); }
            else Sfx.Blip();
        }

        void StartTurbo()
        {
            turboT = TurboSeconds; charge = pips.Length;
            Sfx.Play("turbo"); Award(30); AddShake(8f);
            AddScore(200, worldX + TX, BASE - 220);
            var pp = TruckPt(Def.Puff);
            for (int i = 0; i < 16; i++)
                AddPart(PFlame, worldX + pp.x, pp.y, Random.Range(-60f, 60f), Random.Range(-260f, -60f), -60, Random.Range(11f, 19f), 0, 0, Random.Range(.4f, .7f), Color.white);
        }

        void SmashStep(float dt)
        {
            shakeMag = Mathf.Max(0, shakeMag - dt * 40f);
            spdMul += ((turboT > 0 ? 1.5f : 1f) - spdMul) * Mathf.Min(1, dt * 3f);
            if (turboT <= 0) return;
            turboT -= dt; flameT -= dt; streakT -= dt;
            if (flameT <= 0)
            {
                flameT = .028f; var pp = TruckPt(Def.Puff);
                for (int i = 0; i < 2; i++)
                    AddPart(PFlame, worldX + pp.x, pp.y + Random.Range(-6f, 6f), Random.Range(-40f, 20f), Random.Range(-90f, -10f), -60, Random.Range(9f, 16f), 0, 0, Random.Range(.35f, .6f), Color.white);
            }
            if (streakT <= 0)
            {
                streakT = .045f;
                AddPart(PStreak, worldX + Wl + 60, Random.Range(Hl * .08f, GY + 70), -900, 0, 0, Random.Range(120f, 260f), 0, 0, .4f, Color.white);
            }
            if (turboT <= 0) { turboT = 0; charge = 0; Sfx.Play("swoosh", .6f); }
        }

        /// <summary>Debris bounces on the ground a couple of times, then lies still and scrolls away with the world.</summary>
        void DebrisFloor(Part p, float dt)
        {
            if (p.g == 0f) { p.vx *= Mathf.Max(0f, 1f - 3.5f * dt); p.vr *= Mathf.Max(0f, 1f - 6f * dt); return; }
            float floor = BASE + 6 - p.r * 8f;
            if (p.y > floor && p.vy > 0)
            {
                p.y = floor;
                if (p.vy > 170f) { p.vy *= -.4f; p.vx *= .8f; p.vr *= .6f; }
                else { p.vy = 0; p.g = 0; }
            }
        }

        Vector3 ShakeOffset()
        {
            float m = shakeMag + (turboT > 0 ? 1.8f : 0f);
            if (m < .05f) return Vector3.zero;
            return new Vector3(Mathf.Sin(t * 83f) * m, Mathf.Cos(t * 71f) * m, 0) / 100f;
        }

        void RenderSmash()
        {
            int lit = turboT > 0 ? Mathf.CeilToInt(pips.Length * turboT / TurboSeconds) : charge;
            float u = Mathf.Clamp(.8f / S, 1f, 1.8f), py = Wl < Hl ? 172f : 112f;   // bigger and lower on a phone held upright, clear of the slots
            for (int i = 0; i < pips.Length; i++)
            {
                var r = pips[i]; r.enabled = Started && Hl > 0;
                bool on = i < lit;
                float s = (on ? .72f + .1f * Mathf.Sin(t * 10f + i) : .6f) * u;
                r.transform.position = U(Wl * .5f + (i - 1.5f) * 36f * u, py);
                r.transform.localScale = Vector3.one * s;
                r.color = on ? Color.white : new Color(.55f, .56f, .62f, .5f);
            }
        }
    }
}
