using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigWheels
{
    /// <summary>
    /// Big Wheels: a no-lose monster truck toy for a 3-year-old.
    /// Faithful port of the web prototype. Positions are kept in "logical px" (y down) and converted to
    /// Unity units (100 px = 1 unit) only when rendering, so the tuning numbers match the prototype.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Big Wheels").AddComponent<Game>();
        }

        // ---------- tuning ----------
        const float Gravity = 1900f, Speed = 250f, Tau = Mathf.PI * 2f;

        // ---------- layout (logical px) ----------
        public Vector2 ScreenOverride;                 // set by tests; zero = use the real screen
        float W = 800, H = 560, S = 1, Wl = 860, Hl = 560, GY = 400, BASE = 414, TX = 230;

        // ---------- world state ----------
        public bool Started { get; private set; }
        float worldX, t, spawnX;
        readonly List<Obj> objs = new();
        readonly List<Part> parts = new();
        public int Slots; public int[] SlotCols = new int[5]; public float[] SlotPop = new float[5]; public float Celeb;
        int popCombo; float comboT;
        public int ColorIndex;
        public float Clock => t;
        static void Kill(GameObject go) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }

        class Truck
        {
            public float h, vh, tilt, flipT, flipD, flipRot, bob, bobV, wheel, lastGH, puff, washT;
            public bool air, flipped;
            public List<Mud> mud = new();
        }
        class Mud { public float x, y, r, k; }
        class Obj
        {
            public char t; public float x, w, h, s, sq, diz, hh, ph, r, tm; public int ci; public bool inside, dead;
            public SpriteRenderer sr; public SpriteRenderer[] extra;
        }
        class Part { public int kind; public float x, y, vx, vy, g, r, rot, vr, life, max; public Color col; }
        readonly Truck truck = new();
        public float TruckHeight => truck.h;
        public float WorldX => worldX;
        public int ObjectCount => objs.Count;

        // ---------- scene objects ----------
        Camera cam; Material mat; Hud hud;
        SpriteRenderer sky, sun, sunRays, grass, dirt, dirtEdge, shadow, body;
        SpriteRenderer[] clouds = new SpriteRenderer[6], hillFar = new SpriteRenderer[2], hillNear = new SpriteRenderer[2];
        float[] cloudX = new float[6], cloudY = new float[6], cloudS = new float[6];
        Transform truckRoot, pivot, bodyT;
        SpriteRenderer[] wheels = new SpriteRenderer[2], eyes = new SpriteRenderer[2], pupils = new SpriteRenderer[2];
        readonly List<SpriteRenderer> mudSrs = new();
        Pool stems, petals, centers, pebbles, pDot, pStar, pSq, pBubble;
        Transform worldRoot;

        class Pool
        {
            readonly Game g; readonly List<SpriteRenderer> l = new(); readonly Sprite sp; readonly int order; readonly Transform parent; int n;
            public Pool(Game g, Sprite sp, int order, Transform parent) { this.g = g; this.sp = sp; this.order = order; this.parent = parent; }
            public void Begin() => n = 0;
            public SpriteRenderer Next()
            {
                if (n == l.Count) l.Add(g.MakeSR("pooled", sp, order, parent));
                var r = l[n++]; r.enabled = true; return r;
            }
            public void End() { for (int i = n; i < l.Count; i++) l[i].enabled = false; }
        }

        // ================= boot =================
        void Start() { Boot(); }

        public void Boot()
        {
            if (I != null && I != this) return;
            I = this;
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Art.Build();
            Sfx.Init(gameObject);

            mat = Resources.Load<Material>("SpriteUnlit");
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));

            cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera") { tag = "MainCamera" }; cam = cg.AddComponent<Camera>(); }
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Palette.Sky2;
            cam.transform.position = new Vector3(0, 0, -10); cam.transform.rotation = Quaternion.identity;

            worldRoot = new GameObject("World").transform;
            BuildScenery();
            BuildTruck();
            stems = new Pool(this, Art.White, -52, worldRoot); petals = new Pool(this, Art.Dot, -51, worldRoot); centers = new Pool(this, Art.Dot, -50, worldRoot);
            pebbles = new Pool(this, Art.Dot, -54, worldRoot);
            pDot = new Pool(this, Art.Dot, 20, worldRoot); pStar = new Pool(this, Art.Star, 21, worldRoot);
            pSq = new Pool(this, Art.Square, 22, worldRoot); pBubble = new Pool(this, Art.Bubble, 23, worldRoot);

            hud = new Hud(this, cam);

            spawnX = worldX + TX + 520;
            ComputeLayout();
            Render();
        }

        void BuildScenery()
        {
            sky = MakeSR("Sky", Art.Sky, -100);
            sunRays = MakeSR("SunRays", Art.SunRays, -90); sun = MakeSR("Sun", Art.SunNormal, -89);
            for (int i = 0; i < 6; i++)
            {
                clouds[i] = MakeSR("Cloud", Art.Cloud, -85);
                cloudX[i] = i * 260 + Random.Range(0, 100f); cloudY[i] = Random.Range(.04f, .3f); cloudS[i] = Random.Range(.7f, 1.3f);
            }
            for (int i = 0; i < 2; i++) { hillFar[i] = MakeSR("HillFar", Art.HillFar, -80); hillNear[i] = MakeSR("HillNear", Art.HillNear, -70); }
            grass = MakeSR("Grass", Art.White, -60); grass.color = Palette.Grass;
            dirt = MakeSR("Dirt", Art.White, -56); dirt.color = Palette.Dirt;
            dirtEdge = MakeSR("DirtEdge", Art.White, -55); dirtEdge.color = Palette.DirtDk;
            shadow = MakeSR("Shadow", Art.Shadow, -10);
        }

        void BuildTruck()
        {
            truckRoot = new GameObject("Truck").transform; truckRoot.SetParent(worldRoot, false);
            pivot = new GameObject("Pivot").transform; pivot.SetParent(truckRoot, false); pivot.localPosition = new Vector3(0, .9f, 0);
            body = MakeSR("Body", Art.Body[0], 0, pivot); bodyT = body.transform;
            for (int i = 0; i < 2; i++)
            {
                float ex = i == 0 ? -26 : 12, ey = -160;
                eyes[i] = MakeSR("Eye", Art.EyeWhite, 1, bodyT); eyes[i].transform.localPosition = new Vector3(ex / 100f, -ey / 100f, 0);
                pupils[i] = MakeSR("Pupil", Art.Pupil, 2, eyes[i].transform);
                wheels[i] = MakeSR("Wheel", Art.Wheel, 5, pivot);
                wheels[i].transform.localPosition = new Vector3((i == 0 ? -78 : 78) / 100f, -.44f, 0);
            }
        }

        public SpriteRenderer MakeSR(string name, Sprite s, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : worldRoot, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sharedMaterial = mat; r.sortingOrder = order;
            return r;
        }

        // ================= loop =================
        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) Act();
            Tick(Mathf.Min(.05f, UnityEngine.Time.unscaledDeltaTime));
        }

        public void Tick(float dt)
        {
            ComputeLayout();
            UpdateSim(dt);
            Render();
            hud.Refresh(this);
        }

        void ComputeLayout()
        {
            Vector2 sz = ScreenOverride != Vector2.zero ? ScreenOverride : new Vector2(Screen.width, Screen.height);
            W = Mathf.Max(1, sz.x); H = Mathf.Max(1, sz.y);
            S = Mathf.Min(H / 560f, W / 860f);
            Wl = W / S; Hl = H / S;
            GY = Hl > 700 ? Hl * .64f : Hl * .72f;
            BASE = GY + 14; TX = Wl * .27f;
        }

        static Vector3 U(float sx, float sy, float z = 0) => new Vector3(sx / 100f, -sy / 100f, z);

        // ================= actions =================
        public void Act()
        {
            if (!Started) { StartGame(); return; }
            if (!truck.air)
            {
                truck.air = true; truck.vh = 760; truck.bobV += 160;
                Sfx.Play("honk"); Sfx.Play("boing");
                Stars(worldX + TX - 60, BASE - 6, 5, new[] { Palette.Hex("#e9c9a0"), Palette.Hex("#d4aa7a") }, 120);
            }
            else if (truck.flipD == 0)
            {
                float tl = TimeToLand(truck.h, truck.vh);
                if (tl > .35f) { truck.flipD = tl * .88f; truck.flipT = 0; truck.flipped = true; Sfx.Play("swoosh"); Sfx.Play("honk"); }
            }
        }

        public void StartGame()
        {
            if (Started) return;
            Started = true; Sfx.StartEngine();
            Slots = 0; Celeb = 0; System.Array.Clear(SlotPop, 0, 5);
            Sfx.Play("honk"); Sfx.Play("boing");
        }

        public void SetColor(int i)
        {
            if (!Started) StartGame();
            ColorIndex = i; body.sprite = Art.Body[i]; truck.bobV += 300;
            Sfx.Play("sparkle");
            Stars(worldX + TX, BASE - truck.h - 100, 12, new[] { Palette.Truck[i], Color.white, Palette.Sun }, 320);
            Sfx.Say(Palette.Names[i], true);
        }

        public bool ToggleMute() { Sfx.Muted = !Sfx.Muted; return Sfx.Muted; }

        static float TimeToLand(float h, float vh) => (vh + Mathf.Sqrt(vh * vh + 2 * Gravity * h)) / Gravity;

        void Land()
        {
            float fall = -truck.vh;
            truck.air = false; truck.vh = 0; truck.flipD = 0; truck.flipT = 0; truck.flipRot = 0;
            truck.bobV += Mathf.Min(520, fall * .5f);
            float cx = worldX + TX;
            if (fall > 300)
            {
                Sfx.Play("thump");
                Stars(cx, BASE - 4, 8, new[] { Palette.Hex("#e9c9a0"), Palette.Hex("#d4aa7a"), Palette.Hex("#c58f5c") }, 200);
            }
            if (truck.flipped) { truck.flipped = false; Confetti(cx, BASE - 140, 26, 360); Sfx.Play("sparkle"); }
        }

        void CrushCar(Obj c)
        {
            c.s = 1; c.sq = 0; c.diz = 1.6f;
            Sfx.Play("squash");
            Stars(c.x, BASE - 30, 12, new[] { Palette.Sun, Palette.Hex("#ff9a1f"), Color.white }, 340);
            truck.bobV += 260;
            if (!truck.air && GroundH(worldX + TX + 40) == 0) { truck.air = true; truck.vh = 300; }
            if (Celeb > 0) { Sfx.Say("yay", false); return; }
            int n = Slots++;
            SlotCols[n] = c.ci; SlotPop[n] = 1;
            Sfx.Say(new[] { "one", "two", "three", "four", "five" }[n], true);
            if (Slots >= 5)
            {
                Celeb = 2.6f; Sfx.Play("fanfare");
                Confetti(worldX + TX, BASE - 200, 40, 500);
                Sfx.Say("yay", true);
            }
        }

        // ================= world generation =================
        void Spawn()
        {
            float x = spawnX, r = Random.value;
            char type = r < .42f ? 'c' : r < .66f ? 'b' : r < .78f ? 'r' : r < .92f ? 'p' : 'w';
            if (type == 'w' && truck.mud.Count < 4) type = 'c';
            float w = 0;
            switch (type)
            {
                case 'c':
                    objs.Add(NewObj('c', x + 60, 120, Random.Range(0, 7))); w = 120; break;
                case 'b':
                    for (int i = 0; i < 3; i++) { var b = NewObj('b', x + i * 74 + Random.Range(-8f, 8f), 0, Random.Range(0, 7)); b.hh = Random.Range(150f, 330f); objs.Add(b); }
                    w = 222; break;
                case 'r':
                    var ramp = NewObj('r', x, 180, 0); ramp.h = 80; objs.Add(ramp);
                    foreach (var (dx, hh) in new[] { (110f, 250f), (180f, 290f), (250f, 245f) })
                    { var b = NewObj('b', x + 180 + dx, 0, Random.Range(0, 7)); b.hh = hh; objs.Add(b); }
                    w = 180 + 330; break;
                case 'p':
                    objs.Add(NewObj('p', x + 110, 220, 0)); w = 220; break;
                default:
                    objs.Add(NewObj('w', x + 150, 300, 0)); w = 300; break;
            }
            spawnX = x + w + Random.Range(330f, 560f);
        }

        Obj NewObj(char type, float x, float w, int ci)
        {
            var o = new Obj { t = type, x = x, w = w, ci = ci, r = 27, ph = Random.Range(0, 6f) };
            switch (type)
            {
                case 'c':
                    o.sr = MakeSR("Car", Art.Car[ci], -20);
                    o.extra = new SpriteRenderer[3];
                    for (int i = 0; i < 3; i++) { o.extra[i] = MakeSR("Dizzy", Art.Star, -19); o.extra[i].color = Palette.Sun; o.extra[i].enabled = false; }
                    break;
                case 'b': o.sr = MakeSR("Balloon", Art.Balloon[ci], 10); break;
                case 'r': o.sr = MakeSR("Ramp", Art.Ramp, -30); break;
                case 'p': o.sr = MakeSR("Puddle", Art.Puddle, -40); break;
                case 'w':
                    o.sr = MakeSR("WashBack", Art.WashBack, -25);
                    o.extra = new SpriteRenderer[13];
                    for (int i = 0; i < 13; i++)
                    {
                        o.extra[i] = MakeSR("Curtain", Art.Strip, 12);
                        o.extra[i].color = i % 2 == 1 ? new Color(.47f, .78f, 1f, .5f) : new Color(1, 1, 1, .5f);
                    }
                    break;
            }
            return o;
        }

        Obj RampAt(float x) { foreach (var o in objs) if (o.t == 'r' && x >= o.x && x <= o.x + o.w) return o; return null; }
        float GroundH(float x) { var r = RampAt(x); return r != null ? r.h * (x - r.x) / r.w : 0f; }

        // ================= simulation =================
        void UpdateSim(float dt)
        {
            t += dt; worldX += Speed * dt; truck.wheel += Speed * dt / 46f;
            float cx = worldX + TX;
            while (spawnX < worldX + Wl + 300) Spawn();

            float gNow = GroundH(cx + 40);
            if (!truck.air)
            {
                if (truck.lastGH > 6 && gNow < truck.lastGH - 6)
                {
                    truck.air = true; truck.vh = 800;
                    truck.flipD = TimeToLand(truck.h, 800) * .86f; truck.flipT = 0; truck.flipped = true;
                    Sfx.Play("whee"); Sfx.Play("honk");
                }
                else truck.h = gNow;
            }
            if (truck.air)
            {
                truck.vh -= Gravity * dt; truck.h += truck.vh * dt;
                if (truck.h <= gNow) { truck.h = gNow; if (truck.vh < 0) Land(); }
            }
            truck.lastGH = gNow;
            if (truck.flipD > 0)
            {
                truck.flipT = Mathf.Min(truck.flipD, truck.flipT + dt);
                float p = truck.flipT / truck.flipD;
                truck.flipRot = -Tau * (p * p * (3 - 2 * p));
            }
            float tiltTarget = truck.air ? Mathf.Clamp(-truck.vh / 2800f, -.3f, .3f) : (RampAt(cx + 40) != null ? -Mathf.Atan2(80, 180) : 0f);
            truck.tilt += (tiltTarget - truck.tilt) * Mathf.Min(1, dt * 10);
            truck.bobV += (-180 * truck.bob - 14 * truck.bobV) * dt; truck.bob += truck.bobV * dt;
            truck.bob = Mathf.Clamp(truck.bob, -14, 22);

            foreach (var o in objs) StepObj(o, cx, dt);
            objs.RemoveAll(o =>
            {
                bool gone = o.dead || o.x + (o.w > 0 ? o.w : 100) < worldX - 400;
                if (gone) { Kill(o.sr.gameObject); if (o.extra != null) foreach (var e in o.extra) if (e) Kill(e.gameObject); }
                return gone;
            });

            truck.puff -= dt;
            if (truck.puff <= 0 && truck.flipD == 0)
            {
                truck.puff = .14f;
                AddPart(4, cx - 98, BASE - truck.h - 195, -60, -70, -30, 7, 0, 0, .7f, new Color(.87f, .89f, .93f));
            }

            if (comboT > 0) { comboT -= dt; if (comboT <= 0) popCombo = 0; }
            for (int i = 0; i < 5; i++) if (SlotPop[i] > 0) SlotPop[i] = Mathf.Max(0, SlotPop[i] - dt * 2.2f);
            if (Celeb > 0)
            {
                Celeb -= dt;
                if (Random.value < dt * 40) AddPart(2, worldX + Random.Range(0, Wl), -10, Random.Range(-60f, 60f), Random.Range(80f, 220f), 260, Random.Range(5f, 9f), Random.Range(0, Tau), Random.Range(-9f, 9f), Random.Range(2f, 3f), RandomColor());
                if (Celeb <= 0) { Slots = 0; System.Array.Clear(SlotPop, 0, 5); }
            }

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i]; p.life -= dt;
                if (p.life <= 0) { parts.RemoveAt(i); continue; }
                p.x += p.vx * dt; p.y += p.vy * dt; p.vy += p.g * dt; p.rot += p.vr * dt;
            }
            Sfx.SetEngine(46 + (truck.air ? 22 : 0) + 5 * Mathf.Sin(t * 14));
        }

        void StepObj(Obj o, float cx, float dt)
        {
            switch (o.t)
            {
                case 'c':
                    if (o.s == 0) { if (Mathf.Abs(cx - o.x) < o.w / 2 + 24 && truck.h < 95) CrushCar(o); }
                    else { if (o.sq < 1) o.sq = Mathf.Min(1, o.sq + dt / .14f); if (o.diz > 0) o.diz -= dt; }
                    break;
                case 'b':
                    if (o.dead) break;
                    {
                        float by = BASE - o.hh + Mathf.Sin(t * 2 + o.ph) * 8, bx = o.x;
                        float nx = Mathf.Clamp(bx, cx - 105, cx + 105), ny = Mathf.Clamp(by, BASE - truck.h - 195, BASE - truck.h);
                        if ((bx - nx) * (bx - nx) + (by - ny) * (by - ny) < (o.r * .9f) * (o.r * .9f))
                        {
                            o.dead = true; comboT = 1.6f;
                            Sfx.Pop(popCombo++);
                            Confetti(bx, by, 14, 220); Stars(bx, by, 6, new[] { Palette.Truck[o.ci], Color.white }, 220);
                            Sfx.Say(Palette.Names[o.ci], false);
                        }
                    }
                    break;
                case 'p':
                    {
                        bool inside = Mathf.Abs(cx - o.x) < o.w / 2 + 10 && truck.h < 40;
                        if (inside && !o.inside) { Sfx.Play("splash"); MudSplash(cx + 70, BASE + 4, 12); truck.bobV += 120; }
                        o.inside = inside;
                        if (inside)
                        {
                            o.tm -= dt;
                            if (o.tm <= 0)
                            {
                                o.tm = .12f; MudSplash(cx + (Random.value < .5f ? -78 : 78), BASE + 2, 3);
                                if (truck.mud.Count < 18) truck.mud.Add(new Mud { x = Random.Range(-100f, 100f), y = Random.Range(-190f, -78f), r = Random.Range(5f, 12f), k = Random.Range(0, 6f) });
                            }
                        }
                    }
                    break;
                case 'w':
                    if (Mathf.Abs(cx - o.x) < 150 && truck.h < 120)
                    {
                        truck.washT -= dt;
                        if (truck.washT <= 0)
                        {
                            truck.washT = .08f;
                            if (truck.mud.Count > 0) truck.mud.RemoveAt(truck.mud.Count - 1);
                            Sfx.Blip();
                            for (int k = 0; k < 2; k++)
                                AddPart(3, cx + Random.Range(-110f, 110f), BASE - truck.h - Random.Range(20f, 190f), Random.Range(-20f, 20f), Random.Range(-90f, -30f), -20, k == 0 ? Random.Range(7f, 15f) : Random.Range(5f, 10f), 0, 0, Random.Range(.8f, 1.4f), Color.white);
                        }
                    }
                    break;
            }
        }

        // ================= particles =================
        static Color RandomColor() => Palette.Truck[Random.Range(0, Palette.Truck.Length)];
        void AddPart(int kind, float x, float y, float vx, float vy, float g, float r, float rot, float vr, float life, Color col) =>
            parts.Add(new Part { kind = kind, x = x, y = y, vx = vx, vy = vy, g = g, r = r, rot = rot, vr = vr, life = life, max = life, col = col });
        void Stars(float x, float y, int n, Color[] cols, float sp)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Random.Range(0, Tau), v = Random.Range(.4f, 1f) * sp;
                AddPart(1, x, y, Mathf.Cos(a) * v, Mathf.Sin(a) * v - 120, 700, Random.Range(7f, 13f), Random.Range(0, Tau), Random.Range(-6f, 6f), Random.Range(.6f, 1f), cols[Random.Range(0, cols.Length)]);
            }
        }
        void Confetti(float x, float y, int n, float spread)
        {
            for (int i = 0; i < n; i++)
                AddPart(2, x, y, Random.Range(-spread, spread), Random.Range(-600f, -150f), 900, Random.Range(5f, 9f), Random.Range(0, Tau), Random.Range(-9f, 9f), Random.Range(1f, 1.8f), RandomColor());
        }
        void MudSplash(float x, float y, int n)
        {
            var cols = new[] { Palette.Mud, Palette.Hex("#7a4f2e"), Palette.Hex("#8f5a34") };
            for (int i = 0; i < n; i++)
                AddPart(0, x + Random.Range(-40f, 40f), y, Random.Range(-160f, 160f), Random.Range(-520f, -160f), 1500, Random.Range(4f, 10f), 0, 0, Random.Range(.5f, .9f), cols[Random.Range(0, 3)]);
        }

        // ================= rendering =================
        static void Quad(SpriteRenderer r, float x0, float y0, float x1, float y1)
        {
            r.transform.position = U((x0 + x1) / 2, (y0 + y1) / 2);
            r.transform.localScale = new Vector3((x1 - x0) / 2f, (y1 - y0) / 2f, 1);   // White sprite is 2 logical px square
        }
        static void Dot(SpriteRenderer r, float x, float y, float rx, float ry, Color c)
        {
            r.transform.position = U(x, y); r.transform.localScale = new Vector3(rx * 2 / 100f, ry * 2 / 100f, 1); r.color = c;
        }

        void Render()
        {
            cam.orthographicSize = Hl / 200f;
            cam.transform.position = new Vector3(Wl / 200f, -Hl / 200f, -10);

            // sky (sprite is 0.04 x 0.64 units)
            sky.transform.position = U(0, 0); sky.transform.localScale = new Vector3((Wl + 2) / 100f / .04f, GY / 100f / .64f, 1);

            // sun + clouds
            float big = 1 + .22f * Mathf.Min(1, Celeb) * (1 + Mathf.Sin(t * 12) * .2f);
            Vector3 sp = U(Wl * .74f, Mathf.Max(175, GY * .36f));
            sun.transform.position = sp; sun.transform.localScale = Vector3.one * big; sun.sprite = Celeb > 0 ? Art.SunHappy : Art.SunNormal;
            sunRays.transform.position = sp; sunRays.transform.localScale = Vector3.one * big; sunRays.transform.rotation = Quaternion.Euler(0, 0, -t * .2f * Mathf.Rad2Deg);
            float span = Wl + 400;
            for (int i = 0; i < clouds.Length; i++)
            {
                float x = (((cloudX[i] - worldX * .06f - t * 6) % span) + span) % span - 200;
                clouds[i].transform.position = U(x, Mathf.Max(40, GY * cloudY[i] + 40));
                clouds[i].transform.localScale = Vector3.one * cloudS[i];
            }

            // hills (two copies side by side so the texture can wrap)
            Hill(hillFar, Art.HillFarPeriod, .12f, GY - 24);
            Hill(hillNear, Art.HillNearPeriod, .25f, GY - 24);

            // ground
            Quad(grass, 0, GY - 24, Wl + 2, GY + 4);
            Quad(dirt, 0, GY, Wl + 2, Hl + 4);
            Quad(dirtEdge, 0, GY, Wl + 2, GY + 6);
            DrawGroundDetail();

            foreach (var o in objs) RenderObj(o);
            RenderTruck();
            RenderParts();
        }

        void Hill(SpriteRenderer[] pair, float period, float par, float baseY)
        {
            float off = (worldX * par) % period;
            for (int i = 0; i < 2; i++) pair[i].transform.position = U(-off + i * period, baseY);
        }

        void DrawGroundDetail()
        {
            stems.Begin(); petals.Begin(); centers.Begin(); pebbles.Begin();
            int k0 = Mathf.FloorToInt(worldX / 110) - 1, k1 = Mathf.FloorToInt((worldX + Wl) / 110) + 1;
            for (int k = k0; k <= k1; k++)
            {
                float h = Hash(k), x = k * 110 + h * 60 - worldX, y = GY - 16 + Hash(k + 7) * 8;
                var col = Palette.Truck[Mathf.FloorToInt(Hash(k + 3) * Palette.Truck.Length)];
                var st = stems.Next(); Quad(st, x - 1.5f, y - 12, x + 1.5f, y); st.color = Palette.Hex("#2f8f45");
                Dot(petals.Next(), x, y - 15, 6, 6, col);
                Dot(centers.Next(), x, y - 15, 2.4f, 2.4f, Color.white);
            }
            int j0 = Mathf.FloorToInt(worldX / 90) - 1, j1 = Mathf.FloorToInt((worldX + Wl) / 90) + 1;
            for (int k = j0; k <= j1; k++)
                for (int n = 0; n < 3; n++)
                {
                    float h = Hash(k * 3 + n + 99), x = k * 90 + Hash(k + n * 17) * 80 - worldX, y = GY + 24 + Hash(k * 5 + n) * Mathf.Max(60, Hl - GY - 40);
                    Dot(pebbles.Next(), x, y, 6 + h * 9, 3 + h * 4, n % 2 == 1 ? Palette.DirtLt : Palette.DirtDk);
                }
            stems.End(); petals.End(); centers.End(); pebbles.End();
        }

        static float Hash(float k) { float x = Mathf.Sin(k * 127.1f + 311.7f) * 43758.5453f; return x - Mathf.Floor(x); }

        void RenderObj(Obj o)
        {
            float sx = o.x - worldX;
            switch (o.t)
            {
                case 'c':
                    {
                        float e = o.sq * o.sq * (3 - 2 * o.sq);
                        bool near = o.s == 0 && Mathf.Abs(worldX + TX - o.x) < 320;
                        o.sr.transform.position = U(sx, BASE);
                        o.sr.transform.localScale = new Vector3(1 + .32f * e, 1 - .84f * e, 1);
                        o.sr.transform.rotation = Quaternion.Euler(0, 0, near ? -Mathf.Sin(t * 40) * .04f * Mathf.Rad2Deg : 0);
                        bool dizzy = o.s != 0 && o.diz > 0;
                        for (int i = 0; i < 3; i++)
                        {
                            var d = o.extra[i]; d.enabled = dizzy;
                            if (!dizzy) continue;
                            float a = t * 6 + i * 2.1f;
                            d.transform.position = U(sx + Mathf.Cos(a) * 30, BASE - 34 + Mathf.Sin(a) * 8);
                            d.transform.rotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg); d.transform.localScale = Vector3.one * .16f;
                        }
                        break;
                    }
                case 'b':
                    o.sr.transform.position = U(sx, BASE - o.hh + Mathf.Sin(t * 2 + o.ph) * 8);
                    o.sr.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Sin(t * 1.5f + o.ph) * .06f * Mathf.Rad2Deg);
                    break;
                case 'r': case 'p':
                    o.sr.transform.position = U(sx, BASE);
                    break;
                case 'w':
                    o.sr.transform.position = U(sx, BASE);
                    for (int i = 0; i < 13; i++)
                    {
                        float len = 160 + Hash(i + 3) * 40, sw = Mathf.Sin(t * 3 + i) * 6;
                        var s = o.extra[i];
                        s.transform.position = U(sx - 132 + i * 22 + 7 + sw, BASE - 210);
                        s.transform.localScale = new Vector3(1, len / 200f, 1);
                    }
                    break;
            }
        }

        void RenderTruck()
        {
            float sy = BASE - truck.h;
            truckRoot.position = U(TX, sy);
            float rot = truck.tilt + truck.flipRot;
            pivot.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            bodyT.localPosition = new Vector3(0, -.9f - truck.bob / 100f, 0);
            body.sprite = Art.Body[ColorIndex];
            for (int i = 0; i < 2; i++) wheels[i].transform.localRotation = Quaternion.Euler(0, 0, -truck.wheel * Mathf.Rad2Deg);

            float ey = Mathf.Clamp(-truck.vh / 180f, -6f, 6f);
            for (int i = 0; i < 2; i++) pupils[i].transform.localPosition = new Vector3(3 / 100f, -ey / 100f, 0);

            // mud splats stuck to the body
            while (mudSrs.Count < truck.mud.Count) mudSrs.Add(MakeSR("Mud", Art.Mud, 3, bodyT));
            for (int i = 0; i < mudSrs.Count; i++)
            {
                bool on = i < truck.mud.Count; mudSrs[i].enabled = on;
                if (!on) continue;
                var m = truck.mud[i];
                mudSrs[i].transform.localPosition = new Vector3(m.x / 100f, -m.y / 100f, 0);
                mudSrs[i].transform.localRotation = Quaternion.Euler(0, 0, -m.k * Mathf.Rad2Deg);
                mudSrs[i].transform.localScale = new Vector3(m.r * 1.2f / 14f, m.r / 11f, 1);
            }

            float sh = Mathf.Max(0, 1 - truck.h / 260f);
            Dot(shadow, TX, BASE + 2, 120 * sh + 20, 12 * sh + 3, new Color(.16f, .12f, .08f, .28f * sh));
        }

        void RenderParts()
        {
            pDot.Begin(); pStar.Begin(); pSq.Begin(); pBubble.Begin();
            foreach (var p in parts)
            {
                float sx = p.x - worldX, a = Mathf.Clamp01(p.life / p.max * 1.6f);
                if (sx < -60 || sx > Wl + 60) continue;
                SpriteRenderer r; Color c = p.col;
                switch (p.kind)
                {
                    case 0: r = pDot.Next(); r.transform.localScale = Vector3.one * (p.r * 2 / 100f); break;
                    case 1: r = pStar.Next(); r.transform.localScale = Vector3.one * (p.r / 50f); break;
                    case 2: r = pSq.Next(); r.transform.localScale = new Vector3(p.r * 2 / 100f, p.r / 100f, 1); break;
                    case 3: r = pBubble.Next(); r.transform.localScale = Vector3.one * (p.r * 2 / 100f); c = Color.white; break;
                    default:
                        r = pDot.Next(); r.transform.localScale = Vector3.one * (p.r * (2 - p.life / p.max) * 2 / 100f); a *= .7f; break;
                }
                r.transform.position = U(sx, p.y);
                r.transform.rotation = Quaternion.Euler(0, 0, -p.rot * Mathf.Rad2Deg);
                c.a = a; r.color = c;
            }
            pDot.End(); pStar.End(); pSq.End(); pBubble.End();
        }
    }
}
