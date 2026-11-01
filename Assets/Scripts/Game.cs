using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigWheels
{
    /// <summary>
    /// Big Wheels: a no-lose monster truck toy for a 3-year-old.
    /// Positions are kept in "logical px" (y down) and converted to Unity units (100 px = 1 unit) only when rendering.
    /// </summary>
    public partial class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Big Wheels").AddComponent<Game>();
        }

        // ---------- tuning ----------
        const float Gravity = 1900f, Speed = 250f, Tau = Mathf.PI * 2f;
        public const int SlotCount = 3;
        const float WorldSeconds = 75f;
        static readonly string[] AnimalSfx = { "moo", "baa", "quack", "crab", "squawk", "peng", "roar", "hoho", "hoot", "yip", "huff" };
        static int WorldSticker(int w) => w == 3 ? 25 : 12 + w;
        static int AnimalSticker(int kind) => kind < 8 ? 4 + kind : 26 + (kind - 8);

        // ---------- layout (logical px) ----------
        public Vector2 ScreenOverride;                 // set by tests; zero = use the real screen
        float W = 800, H = 560, S = 1, Wl = 860, Hl = 560, GY = 400, BASE = 414, TX = 230;

        // ---------- state ----------
        public bool Started { get; private set; }
        float worldX, t, spawnX, animalX;
        readonly List<Obj> objs = new();
        readonly List<Part> parts = new();
        readonly List<Animal> animals = new();
        public int Slots; public int[] SlotCols = new int[SlotCount]; public float[] SlotPop = new float[SlotCount]; public float Celeb;
        int popCombo; float comboT;
        public int ColorIndex, TruckType, World;
        public bool[] Costume = new bool[3];            // 0 party hat, 1 big eyes, 2 flag
        public float Clock => t;
        int pauseRefs; float playSeconds; bool noAward;
        bool paused => pauseRefs > 0;
        /// <summary>True while the sticker book, parent corner or break screen is up: the world stands still.</summary>
        public bool Paused => paused;
        public void Hold(bool on) { pauseRefs = Mathf.Max(0, pauseRefs + (on ? 1 : -1)); Sfx.PauseEngine(paused); }
        public void ResetBreakTimer() { playSeconds = 0; }
        public void OpenParent() => hud.OpenParent();
        public void CloseParent() => hud.CloseParent();
        public void DebugBreak() => hud.ShowBreak();
        public void DebugEndBreak() => hud.HideBreak();
        /// <summary>Parent setting: keep the world on one theme (-1 = let it change by itself).</summary>
        public void SetWorldLock(int w)
        {
            worldTimer = 0;
            if (w < 0) return;
            noAward = true; ApplyTheme(w); noAward = false;
        }
        public void OpenBook() => hud.OpenBook();
        public void CloseBook() => hud.CloseBook();

        /// <summary>Gives a sticker the first time something is discovered, with a pop-up that flies to the book.</summary>
        void Award(int id)
        {
            if (!Started || hud == null || noAward) return;
            if (!Stickers.Add(id)) return;
            hud.Reward(id); Sfx.Play("tada"); Sfx.Say("yay", false);
            Stars(worldX + Wl * .5f, Hl * .35f, 16, new[] { Palette.Sun, Color.white, Palette.Hex("#ff6eb4") }, 360);
        }
        float worldTimer, wipeT, sunBounce, digT, lidT, sprayT, sirenCd, snowFade, nightFade;
        bool wipeDark;
        const int NStars = 44, NFlies = 22;
        readonly float[] starX = new float[NStars], starY = new float[NStars], starP = new float[NStars], flyX = new float[NFlies], flyY = new float[NFlies], flyP = new float[NFlies];
        SpriteRenderer nightVeil, beam;
        Pool skyStars, flyGlow, flyCore;
        const int Flakes = 70;
        readonly float[] flakeX = new float[Flakes], flakeY = new float[Flakes], flakeS = new float[Flakes];
        bool wipeApplied, digDumped;

        static void Kill(GameObject go) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }

        class Truck
        {
            public float h, vh, tilt, flipT, flipD, flipRot, bob, bobV, wheel, lastGH, puff, washT;
            public bool air, flipped;
            public List<Mud> mud = new();
        }
        class Mud { public float x, y, r, k; public Color col; }
        class Obj
        {
            public char t; public float x, w, h, s, sq, diz, hh, ph, r, tm, eat, scare, hop; public int ci; public bool inside, dead, quacked;
            public SpriteRenderer sr; public SpriteRenderer[] extra;
        }
        class Animal { public int kind; public float x, hopT; public bool cheered; public SpriteRenderer sr; }
        class Part { public int kind; public float x, y, vx, vy, g, r, rot, vr, life, max; public Color col; }
        class Theme { public Sprite sky, hillFar, hillNear, sun, sunHappy; public Color grass, dirt, dirtLt, dirtDk, bg, cloud = Color.white; public bool shells, pines, glow, rays = true; }
        readonly Truck truck = new();
        TruckDef Def => Trucks.All[TruckType];
        public float TruckHeight => truck.h;
        public float WorldX => worldX;
        public int ObjectCount => objs.Count;
        public int AnimalCount => animals.Count;

        // ---------- scene objects ----------
        Camera cam; Material mat; Hud hud;
        SpriteRenderer sky, sun, sunRays, grass, dirt, dirtEdge, shadow, body, wipe, hat, pole, cloth, arm, lid, beacon;
        readonly SpriteRenderer[] clouds = new SpriteRenderer[6], hillFar = new SpriteRenderer[2], hillNear = new SpriteRenderer[2];
        readonly float[] cloudX = new float[6], cloudY = new float[6], cloudS = new float[6];
        Transform truckRoot, pivot, bodyT, worldRoot;
        readonly SpriteRenderer[] wheels = new SpriteRenderer[2], eyes = new SpriteRenderer[2], pupils = new SpriteRenderer[2];
        readonly List<SpriteRenderer> mudSrs = new();
        Pool stems, petals, centers, pebbles, pDot, pStar, pSq, pBubble;
        Theme[] themes;

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
            Art.Build(); Art2.Build(); Art3.Build(); SmashArt.Build(); Trucks.Build();
            Sfx.Init(gameObject);

            mat = Resources.Load<Material>("SpriteUnlit");
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));

            cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera") { tag = "MainCamera" }; cam = cg.AddComponent<Camera>(); }
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Palette.Sky2;
            cam.transform.position = new Vector3(0, 0, -10); cam.transform.rotation = Quaternion.identity;

            themes = new[]
            {
                new Theme { sun = Art.SunNormal, sunHappy = Art.SunHappy, sky = Art.Sky, hillFar = Art.HillFar, hillNear = Art.HillNear, grass = Palette.Grass, dirt = Palette.Dirt, dirtLt = Palette.DirtLt, dirtDk = Palette.DirtDk, bg = Palette.Sky2 },
                new Theme { sun = Art.SunNormal, sunHappy = Art.SunHappy, sky = Art2.SkyBeach, hillFar = Art2.HillSea, hillNear = Art2.HillDunes, grass = Palette.Hex("#f6dfa0"), dirt = Palette.Hex("#e6bd7a"),
                            dirtLt = Palette.Hex("#f3d596"), dirtDk = Palette.Hex("#c99a55"), bg = Palette.Hex("#fff1c9"), shells = true },
                new Theme { sun = Art.SunNormal, sunHappy = Art.SunHappy, sky = Art2.SkySnow, hillFar = Art2.HillMountains, hillNear = Art2.HillSnow, grass = Palette.Hex("#f4f9ff"), dirt = Palette.Hex("#dbe9f5"),
                            dirtLt = Palette.Hex("#eef6fc"), dirtDk = Palette.Hex("#b9d0e6"), bg = Palette.Hex("#eef6fc"), pines = true },
                new Theme { sun = Art3.MoonSky, sunHappy = Art3.MoonSkyHappy, sky = Art2.SkyNight, hillFar = Art2.HillNightFar, hillNear = Art2.HillNightNear,
                            grass = Palette.Hex("#1f3a55"), dirt = Palette.Hex("#3a3560"), dirtLt = Palette.Hex("#4b4580"), dirtDk = Palette.Hex("#2b2750"),
                            bg = Palette.Hex("#0a0e30"), cloud = new Color(.30f, .33f, .6f, 1f), glow = true, rays = false }
            };

            worldRoot = new GameObject("World").transform;
            BuildScenery();
            truckRoot = new GameObject("Truck").transform; truckRoot.SetParent(worldRoot, false);
            pivot = new GameObject("Pivot").transform; pivot.SetParent(truckRoot, false);
            RebuildTruck();
            stems = new Pool(this, Art.White, -52, worldRoot); petals = new Pool(this, Art.Dot, -51, worldRoot); centers = new Pool(this, Art.Dot, -50, worldRoot);
            pebbles = new Pool(this, Art.Dot, -54, worldRoot);
            pDot = new Pool(this, Art.Dot, 20, worldRoot); pStar = new Pool(this, Art.Star, 21, worldRoot);
            pSq = new Pool(this, Art.Square, 22, worldRoot); pBubble = new Pool(this, Art.Bubble, 23, worldRoot);
            SmashInit();

            skyStars = new Pool(this, Art.Star, -95, worldRoot); flyGlow = new Pool(this, Art.Dot, 41, worldRoot); flyCore = new Pool(this, Art.Dot, 42, worldRoot);
            hud = new Hud(this, cam);

            spawnX = worldX + TX + 520; animalX = worldX + TX + 350;
            for (int i = 0; i < Flakes; i++) { flakeX[i] = Random.value; flakeY[i] = Random.value; flakeS[i] = Random.value; }
            for (int i = 0; i < NStars; i++) { starX[i] = Random.value; starY[i] = Random.Range(.02f, .6f); starP[i] = Random.Range(0f, 6.3f); }
            for (int i = 0; i < NFlies; i++) { flyX[i] = Random.value; flyY[i] = Random.Range(.45f, .95f); flyP[i] = Random.Range(0f, 6.3f); }
            ApplyTheme(Settings.WorldLock >= 0 ? Settings.WorldLock : 0);
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
            grass = MakeSR("Grass", Art.White, -60);
            dirt = MakeSR("Dirt", Art.White, -56);
            dirtEdge = MakeSR("DirtEdge", Art.White, -55);
            shadow = MakeSR("Shadow", Art.Shadow, -10);
            nightVeil = MakeSR("NightVeil", Art.White, 40); nightVeil.color = new Color(.04f, .06f, .23f, 0);
            wipe = MakeSR("Wipe", Art.White, 50); wipe.color = new Color(1, 1, 1, 0);
        }

        void ApplyTheme(int w)
        {
            World = w; var th = themes[w]; Award(WorldSticker(w));
            sky.sprite = th.sky; cam.backgroundColor = th.bg;
            for (int i = 0; i < 2; i++) { hillFar[i].sprite = th.hillFar; hillNear[i].sprite = th.hillNear; }
            grass.color = th.grass; dirt.color = th.dirt; dirtEdge.color = th.dirtDk;
        }

        /// <summary>Builds the truck sprites for the current type, color and costumes.</summary>
        void RebuildTruck()
        {
            for (int i = pivot.childCount - 1; i >= 0; i--) Kill(pivot.GetChild(i).gameObject);
            mudSrs.Clear(); arm = lid = beacon = null;
            var d = Def;
            pivot.localPosition = new Vector3(0, d.PivotY / 100f, 0);
            body = MakeSR("Body", d.Body[ColorIndex], 0, pivot); bodyT = body.transform;
            for (int i = 0; i < 2; i++)
            {
                eyes[i] = MakeSR("Eye", Art.EyeWhite, 2, bodyT);
                eyes[i].transform.localPosition = new Vector3(d.Eyes[i].x / 100f, -d.Eyes[i].y / 100f, 0);
                pupils[i] = MakeSR("Pupil", Art.Pupil, 3, eyes[i].transform);
                wheels[i] = MakeSR("Wheel", Art.WheelToy[ColorIndex], 5, pivot);
                wheels[i].transform.localPosition = new Vector3(d.WheelX[i] / 100f, (d.WheelR - d.PivotY) / 100f, 0);
                wheels[i].transform.localScale = Vector3.one * (d.WheelR / 46f);
            }
            if (d.Arm != null) { arm = MakeSR("Arm", d.Arm[ColorIndex], 3, bodyT); arm.transform.localPosition = new Vector3(d.Joint.x / 100f, -d.Joint.y / 100f, 0); }
            if (d.Lid != null) { lid = MakeSR("Lid", d.Lid[ColorIndex], 1, bodyT); lid.transform.localPosition = new Vector3(d.Hinge.x / 100f, -d.Hinge.y / 100f, 0); }
            if (d.Beacon != Vector2.zero)
            {
                beacon = MakeSR("Beacon", Art.Dot, 4, bodyT);
                beacon.transform.localPosition = new Vector3(d.Beacon.x / 100f, -d.Beacon.y / 100f, 0); beacon.transform.localScale = Vector3.one * .17f;
            }
            beam = MakeSR("Beam", Art3.Beam, 41, bodyT); beam.transform.localPosition = new Vector3(d.Headlight.x / 100f, -d.Headlight.y / 100f, 0);
            hat = MakeSR("Hat", Art2.Hat, 6, bodyT); hat.transform.localPosition = new Vector3(d.Hat.x / 100f, -d.Hat.y / 100f, 0);
            pole = MakeSR("Pole", Art2.FlagPole, 6, bodyT); pole.transform.localPosition = new Vector3(d.Flag.x / 100f, -d.Flag.y / 100f, 0);
            cloth = MakeSR("Cloth", Art2.FlagCloth, 6, bodyT); cloth.transform.localPosition = new Vector3(d.Flag.x / 100f, -d.Flag.y / 100f + .72f, 0);
            ApplyCostumes();
        }

        void ApplyCostumes()
        {
            hat.enabled = Costume[0]; pole.enabled = cloth.enabled = Costume[2];
            for (int i = 0; i < 2; i++) eyes[i].transform.localScale = Vector3.one * (Def.EyeScale * (Costume[1] ? 1.45f : 1f));
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
            if (hitStop > 0) hitStop = Mathf.Max(0, hitStop - dt);
            if (!paused) UpdateSim(hitStop > 0 ? dt * .25f : dt);
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
        Vector2 SunPos => new Vector2(Wl * .74f, Mathf.Max(175, GY * .36f));

        /// <summary>A point on the truck (truck coords) in screen-relative logical px, including tilt and flip.</summary>
        Vector2 TruckPt(Vector2 p)
        {
            float ang = truck.tilt + truck.flipRot, c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            float vx = p.x, vy = p.y + Def.PivotY;
            return new Vector2(TX + vx * c - vy * s, BASE - truck.h - Def.PivotY + vx * s + vy * c);
        }

        // ================= actions =================
        /// <summary>A tap at a screen position: sun, animals and the world all react before the truck jumps.</summary>
        public void Tap(Vector2 px)
        {
            if (paused) return;
            if (!Started) { StartGame(); return; }
            float lx = px.x / S, ly = (H - px.y) / S;
            if ((new Vector2(lx, ly) - SunPos).sqrMagnitude < 75f * 75f) { SunTap(); return; }
            foreach (var a in animals)
                if (Mathf.Abs(lx - (a.x - worldX)) < 62 && ly > GY - 95 && ly < GY + 18) { Poke(a, 1f); Award(AnimalSticker(a.kind)); return; }
            Act();
        }

        void SunTap()
        {
            sunBounce = 1; Sfx.Play("sparkle"); Award(24);
            if (wipeT <= 0 && Settings.WorldLock < 0) StartWipe();
        }

        void StartWipe() { wipeT = .0001f; wipeApplied = false; worldTimer = 0; wipeDark = World == 3 || (World + 1) % themes.Length == 3; Sfx.Play("swoosh"); }

        public void Act()
        {
            if (paused) return;
            if (!Started) { StartGame(); return; }
            if (!truck.air)
            {
                truck.air = true; truck.vh = 760; truck.bobV += 160;
                Horn(); Sfx.Play("boing");
                Stars(worldX + TX - 60, BASE - 6, 5, new[] { Palette.Hex("#e9c9a0"), Palette.Hex("#d4aa7a") }, 120);
            }
            else if (truck.flipD == 0)
            {
                float tl = TimeToLand(truck.h, truck.vh);
                if (tl > .35f) { truck.flipD = tl * .88f; truck.flipT = 0; truck.flipped = true; Sfx.Play("swoosh"); Horn(); }
            }
            TruckSpecial();
        }

        void Horn()
        {
            switch (TruckType)
            {
                case Trucks.Digger: Sfx.Play("beep"); break;
                case Trucks.Fire: if (sirenCd <= 0) { Sfx.Play("siren"); sirenCd = 1.2f; } break;
                case Trucks.Garbage: Sfx.Play("lowhonk"); break;
                default: Sfx.Play("honk"); break;
            }
        }

        /// <summary>The thing each truck does when you tap.</summary>
        void TruckSpecial()
        {
            if (TruckType == Trucks.Digger) StartDig();
            else if (TruckType == Trucks.Fire) { if (sprayT <= 0) Sfx.Play("spray"); sprayT = .7f; }
        }

        void StartDig() { if (digT <= 0) { digT = .7f; digDumped = false; } }

        public void StartGame()
        {
            if (Started) return;
            Started = true; Sfx.StartEngine();
            Slots = 0; Celeb = 0; System.Array.Clear(SlotPop, 0, SlotCount);
            Horn(); Sfx.Play("boing");
            Award(TruckType); Award(WorldSticker(World));
        }

        public void SetColor(int i)
        {
            if (!Started) StartGame();
            ColorIndex = i; body.sprite = Def.Body[i];
            foreach (var w in wheels) w.sprite = Art.WheelToy[i];
            if (arm) arm.sprite = Def.Arm[i];
            if (lid) lid.sprite = Def.Lid[i];
            truck.bobV += 300;
            Sfx.Play("sparkle");
            Stars(worldX + TX, BASE - truck.h - 100, 12, new[] { Palette.Truck[i], Color.white, Palette.Sun }, 320);
            Sfx.Say(Palette.Names[i], true);
        }

        public void SetTruck(int i)
        {
            if (!Started) StartGame();
            if (i == TruckType) { truck.bobV += 300; Horn(); return; }
            TruckType = i; RebuildTruck(); truck.bobV += 400; Award(i);
            Sfx.Play("sparkle"); Horn();
            Stars(worldX + TX, BASE - truck.h - 90, 14, new[] { Palette.Sun, Color.white, Palette.Truck[ColorIndex] }, 320);
        }

        public void ToggleCostume(int i)
        {
            if (!Started) StartGame();
            Costume[i] = !Costume[i]; ApplyCostumes(); truck.bobV += 250; if (Costume[i]) Award(23);
            Sfx.Play("sparkle");
            Stars(worldX + TX, BASE - truck.h - 120, 8, new[] { Palette.Sun, Color.white }, 240);
        }

        /// <summary>Test hook: jump straight to a world.</summary>
        public void SetWorld(int w) { ApplyTheme(w % themes.Length); }

        /// <summary>Test hook: drop an obstacle in front of the truck.</summary>
        public void DebugSpawn(char type, float dx = 420f) { SpawnAt(type, worldX + TX + dx); }
        public void DebugAnimal(int kind, float dx = 400f) { SpawnAnimal(worldX + TX + dx, kind); }

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
            if (truck.flipped) { truck.flipped = false; Confetti(cx, BASE - 140, 26, 360); Sfx.Play("sparkle"); Award(15); }
        }

        void Launch(float vh, bool autoFlip)
        {
            truck.air = true; truck.vh = vh;
            if (autoFlip) { truck.flipD = TimeToLand(truck.h, vh) * .86f; truck.flipT = 0; truck.flipped = true; }
        }

        void CrushCar(Obj c)
        {
            c.s = 1; c.sq = 0; c.diz = 1.6f;
            Sfx.Play("squash"); AddShake(3f); FeedCharge();
            Stars(c.x, BASE - 30, 12, new[] { Palette.Sun, Palette.Hex("#ff9a1f"), Color.white }, 340);
            truck.bobV += 260;
            if (!truck.air && GroundH(worldX + TX + 40) == 0) { truck.air = true; truck.vh = 300; }
            if (TruckType == Trucks.Garbage) { c.eat = .75f; lidT = .9f; Sfx.Play("munch"); }
            else if (TruckType == Trucks.Digger) StartDig();
            else if (TruckType == Trucks.Fire) sprayT = Mathf.Max(sprayT, .4f);
            if (Celeb > 0) { Sfx.Say("yay", false); return; }
            int n = Slots++;
            SlotCols[n] = c.ci; SlotPop[n] = 1;
            Sfx.Say(new[] { "one", "two", "three" }[n], true);
            if (Slots >= SlotCount)
            {
                Celeb = 2.6f; Sfx.Play("fanfare"); Award(17);
                Confetti(worldX + TX, BASE - 200, 40, 500);
                Sfx.Say("yay", true);
            }
        }

        void HitJelly(Obj j, float cx)
        {
            j.s = 1; j.sq = 0;
            Color col = Palette.Truck[Art2.JellyPalette[j.ci]];
            Sfx.Play("splat"); Award(18);
            truck.bobV += 200;
            for (int i = 0; i < 16; i++)
                AddPart(0, j.x + Random.Range(-50f, 50f), BASE - 40, Random.Range(-200f, 200f), Random.Range(-560f, -180f), 1500, Random.Range(5f, 11f), 0, 0, Random.Range(.5f, .9f), col);
            Stars(j.x, BASE - 50, 6, new[] { col, Color.white }, 260);
            for (int i = 0; i < 3 && truck.mud.Count < 18; i++)
                truck.mud.Add(new Mud { x = Random.Range(Def.MudMin.x, Def.MudMax.x), y = Random.Range(Def.MudMin.y, Def.MudMax.y), r = Random.Range(6f, 12f), k = Random.Range(0, 6f), col = col });
        }

        // ================= world generation =================
        void Spawn()
        {
            float r = Random.value;
            char type = r < .22f ? 'c' : r < .34f ? 'b' : r < .42f ? 'r' : r < .48f ? 'p' : r < .52f ? 'w' : r < .59f ? 't' : r < .66f ? 'j' : r < .74f ? 's' : r < .83f ? 'k' : r < .92f ? 'm' : 'n';
            if (type == 'w' && truck.mud.Count < 4) type = 'c';
            float w = SpawnAt(type, spawnX);
            spawnX += w + Random.Range(330f, 560f);
        }

        float SpawnAt(char type, float x)
        {
            float w = 0;
            switch (type)
            {
                case 'c': objs.Add(NewObj('c', x + 60, 120, Random.Range(0, 7))); w = 120; break;
                case 'b':
                    for (int i = 0; i < 3; i++) { var b = NewObj('b', x + i * 74 + Random.Range(-8f, 8f), 0, Random.Range(0, 7)); b.hh = Random.Range(150f, 330f); objs.Add(b); }
                    w = 222; break;
                case 'r':
                    var ramp = NewObj('r', x, 180, 0); ramp.h = 80; objs.Add(ramp);
                    foreach (var (dx, hh) in new[] { (110f, 250f), (180f, 290f), (250f, 245f) })
                    { var b = NewObj('b', x + 180 + dx, 0, Random.Range(0, 7)); b.hh = hh; objs.Add(b); }
                    w = 180 + 330; break;
                case 'p': objs.Add(NewObj('p', x + 110, 220, 0)); w = 220; break;
                case 'w': objs.Add(NewObj('w', x + 150, 300, 0)); w = 300; break;
                case 't':
                    objs.Add(NewObj('t', x + 85, 170, 0));
                    foreach (var (dx, hh) in new[] { (110f, 250f), (180f, 290f), (250f, 250f) })
                    { var b = NewObj('b', x + 85 + dx, 0, Random.Range(0, 7)); b.hh = hh; objs.Add(b); }
                    w = 170 + 260; break;
                case 'j': objs.Add(NewObj('j', x + 75, 150, Random.Range(0, 4))); w = 150; break;
                case 'k': objs.Add(NewObj('k', x + 70, 140, 0)); w = 140; break;
                case 'm': objs.Add(NewObj('m', x + 65, 130, 0)); w = 130; break;
                case 'n': objs.Add(NewObj('n', x + 85, 170, 0)); w = 170; break;
                default: objs.Add(NewObj('s', x + 160, 320, 0)); w = 320; break;
            }
            return w;
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
                case 't': o.sr = MakeSR("Trampoline", Art2.Trampoline, -22); break;
                case 'j': o.sr = MakeSR("Jelly", Art2.Jelly[ci], -21); break;
                case 'k': case 'm': case 'n': o.sr = MakeSR("Smashable", SmashSprite(type), -18); break;
                case 's':
                    o.sr = MakeSR("Stream", Art2.StreamWater, -42);
                    o.extra = new SpriteRenderer[2];
                    for (int i = 0; i < 2; i++) o.extra[i] = MakeSR("Duck", Art2.Animal[Art2.Duck], -41);
                    break;
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

        void SpawnAnimal(float x, int kind = -1)
        {
            if (kind < 0) kind = World == 0 ? Random.Range(0, 3) : World == 1 ? Random.Range(3, 5) : World == 2 ? Random.Range(5, 8) : Random.Range(8, 11);
            var a = new Animal { kind = kind, x = x, sr = MakeSR("Animal", Art2.Animal[kind], -45) };
            animals.Add(a);
        }

        void Poke(Animal a, float vol)
        {
            a.hopT = 1; Sfx.Play(AnimalSfx[a.kind], vol);
            Stars(a.x, GY - 50, 6, new[] { Palette.Sun, Color.white, Palette.Hex("#ff6eb4") }, 170);
        }

        Obj RampAt(float x) { foreach (var o in objs) if (o.t == 'r' && x >= o.x && x <= o.x + o.w) return o; return null; }
        float GroundH(float x) { var r = RampAt(x); return r != null ? r.h * (x - r.x) / r.w : 0f; }

        // ================= simulation =================
        void UpdateSim(float dt)
        {
            if (Started && Settings.BreakMinutes > 0)
            {
                playSeconds += dt;
                if (playSeconds >= Settings.BreakMinutes * 60f) { playSeconds = 0; hud.ShowBreak(); }
            }
            SmashStep(dt);
            t += dt; worldX += Speed * spdMul * dt; truck.wheel += Speed * spdMul * dt / Def.WheelR; sirenCd -= dt;
            float cx = worldX + TX;
            while (spawnX < worldX + Wl + 300) Spawn();
            while (animalX < worldX + Wl + 300) { SpawnAnimal(animalX); animalX += Random.Range(380f, 760f); }

            float gNow = GroundH(cx + 40);
            if (!truck.air)
            {
                if (truck.lastGH > 6 && gNow < truck.lastGH - 6) { Launch(800, true); Sfx.Play("whee"); Horn(); }
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
            StepAnimals(cx, dt);
            StepTruckSpecials(cx, dt);

            truck.puff -= dt;
            if (truck.puff <= 0 && truck.flipD == 0)
            {
                truck.puff = .14f;
                var pp = TruckPt(Def.Puff);
                AddPart(4, worldX + pp.x, pp.y, -60, -70, -30, 7, 0, 0, .7f, new Color(.87f, .89f, .93f));
            }

            if (comboT > 0) { comboT -= dt; if (comboT <= 0) popCombo = 0; }
            for (int i = 0; i < SlotCount; i++) if (SlotPop[i] > 0) SlotPop[i] = Mathf.Max(0, SlotPop[i] - dt * 2.2f);
            if (sunBounce > 0) sunBounce = Mathf.Max(0, sunBounce - dt * 1.6f);
            snowFade += ((World == 2 ? 1f : 0f) - snowFade) * Mathf.Min(1f, dt * 2f);
            nightFade += ((World == 3 ? 1f : 0f) - nightFade) * Mathf.Min(1f, dt * 2f);
            for (int i = 0; i < NFlies; i++)
            {
                flyX[i] += Mathf.Sin(t * .7f + flyP[i] * 1.7f) * 22f * dt / Wl - 6f * dt / Wl;
                flyY[i] += Mathf.Cos(t * .9f + flyP[i] * 2.3f) * 16f * dt / Hl;
                if (flyX[i] < 0f) flyX[i] += 1f; if (flyX[i] > 1f) flyX[i] -= 1f;
                flyY[i] = Mathf.Clamp(flyY[i], .4f, .97f);
            }
            for (int i = 0; i < Flakes; i++)
            {
                flakeY[i] += (45f + flakeS[i] * 70f) * dt / Hl;
                flakeX[i] += Mathf.Sin(t * 1.3f + i) * 14f * dt / Wl - 20f * dt / Wl;
                if (flakeY[i] > 1f) { flakeY[i] -= 1f; flakeX[i] = Random.value; }
                if (flakeX[i] < 0f) flakeX[i] += 1f;
            }
            if (Celeb > 0)
            {
                Celeb -= dt;
                if (Random.value < dt * 40) AddPart(2, worldX + Random.Range(0, Wl), -10, Random.Range(-60f, 60f), Random.Range(80f, 220f), 260, Random.Range(5f, 9f), Random.Range(0, Tau), Random.Range(-9f, 9f), Random.Range(2f, 3f), RandomColor());
                if (Celeb <= 0) { Slots = 0; System.Array.Clear(SlotPop, 0, SlotCount); }
            }

            // the world changes by itself now and then, with a soft white wipe
            worldTimer += dt;
            if (wipeT <= 0 && worldTimer > WorldSeconds && Celeb <= 0 && Settings.WorldLock < 0) StartWipe();
            if (wipeT > 0)
            {
                wipeT += dt / 1.3f;
                if (wipeT >= .5f && !wipeApplied) { wipeApplied = true; ApplyTheme((World + 1) % themes.Length); }
                if (wipeT >= 1f) wipeT = 0;
            }

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i]; p.life -= dt;
                if (p.life <= 0) { parts.RemoveAt(i); continue; }
                p.x += p.vx * dt; p.y += p.vy * dt; p.vy += p.g * dt; p.rot += p.vr * dt;
                if (p.kind >= PCrate && p.kind <= PPin) DebrisFloor(p, dt);
            }
            Sfx.SetEngine(46 + (truck.air ? 22 : 0) + (turboT > 0 ? 18 : 0) + 5 * Mathf.Sin(t * 14));
        }

        void StepAnimals(float cx, float dt)
        {
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                var a = animals[i];
                if (a.x < worldX - 300) { Kill(a.sr.gameObject); animals.RemoveAt(i); continue; }
                if (!a.cheered && Mathf.Abs(cx - a.x) < 260) { a.cheered = true; Poke(a, .5f); }
                if (a.hopT > 0) a.hopT = Mathf.Max(0, a.hopT - dt / .8f);
            }
        }

        void StepTruckSpecials(float cx, float dt)
        {
            if (digT > 0)
            {
                digT -= dt;
                if (!digDumped && digT < .35f)
                {
                    digDumped = true; Sfx.Play("thump", .5f);
                    var bp = TruckPt(new Vector2(190, -70));
                    for (int i = 0; i < 8; i++)
                        AddPart(0, worldX + bp.x, bp.y, Random.Range(40f, 220f), Random.Range(-380f, -120f), 1300, Random.Range(4f, 8f), 0, 0, Random.Range(.6f, 1f), pick(new[] { Palette.DirtDk, Palette.Dirt, Palette.Hex("#6b4428") }));
                }
            }
            if (lidT > 0) lidT -= dt;
            if (sprayT > 0)
            {
                sprayT -= dt;
                var np = TruckPt(Def.Nozzle);
                for (int i = 0; i < 3; i++)
                    AddPart(0, worldX + np.x, np.y, Random.Range(300f, 420f), Random.Range(-440f, -300f), 900, Random.Range(4f, 7.5f), 0, 0, Random.Range(.7f, 1.1f), new Color(.45f, .78f, 1f));
            }
        }

        static T pick<T>(T[] a) => a[Random.Range(0, a.Length)];

        void StepObj(Obj o, float cx, float dt)
        {
            switch (o.t)
            {
                case 'c':
                    if (o.s == 0) { if (Mathf.Abs(cx - o.x) < o.w / 2 + 24 && truck.h < 95) CrushCar(o); }
                    else
                    {
                        if (o.sq < 1) o.sq = Mathf.Min(1, o.sq + dt / .14f);
                        else if (o.eat > 0) { o.eat -= dt; if (o.eat <= 0) o.dead = true; }
                        if (o.diz > 0) o.diz -= dt;
                    }
                    break;
                case 'b':
                    if (o.dead) break;
                    {
                        float by = BASE - o.hh + Mathf.Sin(t * 2 + o.ph) * 8, bx = o.x;
                        float nx = Mathf.Clamp(bx, cx - Def.HalfW, cx + Def.HalfW), ny = Mathf.Clamp(by, BASE - truck.h - Def.Height, BASE - truck.h);
                        bool water = false;
                        if (sprayT > 0) { var np = TruckPt(Def.Nozzle); water = bx > worldX + np.x + 40 && bx < worldX + np.x + 260 && by < np.y + 20 && by > np.y - 190; }
                        if (water || (bx - nx) * (bx - nx) + (by - ny) * (by - ny) < (o.r * .9f) * (o.r * .9f))
                        {
                            o.dead = true; comboT = 1.6f;
                            Sfx.Pop(popCombo++);
                            Confetti(bx, by, 14, 220); Stars(bx, by, 6, new[] { Palette.Truck[o.ci], Color.white }, 220);
                            Sfx.Say(Palette.Names[o.ci], false);
                            Award(16);
                        }
                    }
                    break;
                case 'p':
                    {
                        bool inside = Mathf.Abs(cx - o.x) < o.w / 2 + 10 && truck.h < 40;
                        if (inside && !o.inside) { Sfx.Play("splash"); MudSplash(cx + 70, BASE + 4, 12, Palette.Mud); Award(22); truck.bobV += 120; }
                        o.inside = inside;
                        if (inside)
                        {
                            o.tm -= dt;
                            if (o.tm <= 0)
                            {
                                o.tm = .12f; MudSplash(cx + (Random.value < .5f ? -78 : 78), BASE + 2, 3, Palette.Mud);
                                if (truck.mud.Count < 18)
                                    truck.mud.Add(new Mud { x = Random.Range(Def.MudMin.x, Def.MudMax.x), y = Random.Range(Def.MudMin.y, Def.MudMax.y), r = Random.Range(5f, 12f), k = Random.Range(0, 6f), col = Palette.Mud });
                            }
                        }
                    }
                    break;
                case 's':
                    {
                        bool near = Mathf.Abs(cx - o.x) < 300;
                        if (near && !o.quacked) { o.quacked = true; o.scare = 1; Sfx.Play("quack", .7f); }
                        if (o.scare > 0) o.scare = Mathf.Max(0, o.scare - dt / .9f);
                        bool inside = Mathf.Abs(cx - o.x) < o.w / 2 + 10 && truck.h < 40;
                        var water = new Color(.45f, .78f, 1f);
                        if (inside && !o.inside) { Sfx.Play("splash"); MudSplash(cx + 70, BASE + 4, 12, water); Award(19); truck.bobV += 120; }
                        o.inside = inside;
                        if (inside)
                        {
                            o.tm -= dt;
                            if (o.tm <= 0)
                            {
                                o.tm = .1f; MudSplash(cx + (Random.value < .5f ? -78 : 78), BASE + 2, 3, water);
                                if (truck.mud.Count > 0) { truck.mud.RemoveAt(truck.mud.Count - 1); Sfx.Blip(); }
                            }
                        }
                    }
                    break;
                case 't':
                    if (o.tm > 0) o.tm = Mathf.Max(0, o.tm - dt);
                    if (!truck.air && Mathf.Abs(cx - o.x) < 60 && truck.h < 25 && o.tm <= 0)
                    {
                        o.tm = .3f; Launch(950, true);
                        Sfx.Play("tramp"); Award(20); Horn(); truck.bobV += 200;
                        Stars(o.x, BASE - 50, 8, new[] { Palette.Sun, Color.white, Palette.Hex("#3d8bff") }, 260);
                    }
                    break;
                case 'j':
                    if (o.s == 0) { if (Mathf.Abs(cx - o.x) < o.w / 2 + 20 && truck.h < 70) HitJelly(o, cx); }
                    else if (o.sq < 1) o.sq = Mathf.Min(1, o.sq + dt / .2f);
                    break;
                case 'k': case 'm': case 'n':
                    StepSmash(o, cx);
                    break;
                case 'w':
                    if (Mathf.Abs(cx - o.x) < 150 && truck.h < 120)
                    {
                        truck.washT -= dt; Award(21);
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
        void MudSplash(float x, float y, int n, Color baseCol)
        {
            for (int i = 0; i < n; i++)
            {
                var c = Color.Lerp(baseCol, Random.value < .5f ? Color.white : Color.black, Random.Range(0f, .15f));
                AddPart(0, x + Random.Range(-40f, 40f), y, Random.Range(-160f, 160f), Random.Range(-520f, -160f), 1500, Random.Range(4f, 10f), 0, 0, Random.Range(.5f, .9f), c);
            }
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
            cam.transform.position = new Vector3(Wl / 200f, -Hl / 200f, -10) + ShakeOffset();
            var th = themes[World];

            sky.transform.position = U(0, 0); sky.transform.localScale = new Vector3((Wl + 2) / 100f / .04f, GY / 100f / .64f, 1);

            float big = (1 + .22f * Mathf.Min(1, Celeb) * (1 + Mathf.Sin(t * 12) * .2f)) * (1 + .25f * Mathf.Sin(sunBounce * Mathf.PI));
            Vector3 sp = U(SunPos.x, SunPos.y);
            sun.transform.position = sp; sun.transform.localScale = Vector3.one * big; sun.sprite = (Celeb > 0 || sunBounce > 0) ? th.sunHappy : th.sun; sunRays.enabled = th.rays;
            sunRays.transform.position = sp; sunRays.transform.localScale = Vector3.one * big; sunRays.transform.rotation = Quaternion.Euler(0, 0, -t * .2f * Mathf.Rad2Deg);
            float span = Wl + 400;
            for (int i = 0; i < clouds.Length; i++)
            {
                float x = (((cloudX[i] - worldX * .06f - t * 6) % span) + span) % span - 200;
                clouds[i].transform.position = U(x, Mathf.Max(40, GY * cloudY[i] + 40));
                clouds[i].transform.localScale = Vector3.one * cloudS[i]; clouds[i].color = Color.Lerp(Color.white, themes[3].cloud, nightFade);
            }

            Hill(hillFar, Art.HillFarPeriod, .12f, GY - 24);
            Hill(hillNear, Art.HillNearPeriod, .25f, GY - 24);

            Quad(grass, 0, GY - 24, Wl + 2, GY + 4);
            Quad(dirt, 0, GY, Wl + 2, Hl + 4);
            Quad(dirtEdge, 0, GY, Wl + 2, GY + 6);
            DrawGroundDetail(th);

            RenderNight();
            foreach (var a in animals) RenderAnimal(a);
            foreach (var o in objs) RenderObj(o);
            RenderTruck();
            RenderParts();
            RenderSmash();

            float wa = wipeT > 0 ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(wipeT)) : 0f;
            Quad(wipe, 0, 0, Wl + 2, Hl + 2); wipe.color = wipeDark ? new Color(.05f, .07f, .2f, wa) : new Color(1, 1, 1, wa); wipe.enabled = wa > .001f;
        }

        /// <summary>Stars, a dark veil over the world, fireflies and the truck's headlight beam; all fade with nightFade.</summary>
        void RenderNight()
        {
            skyStars.Begin(); flyGlow.Begin(); flyCore.Begin();
            bool on = nightFade > .01f;
            if (on)
            {
                for (int i = 0; i < NStars; i++)
                {
                    var r = skyStars.Next(); float tw = .55f + .45f * Mathf.Sin(t * 2.2f + starP[i]), sz = 5f + 6f * Hash(i + 11);
                    r.transform.position = U(starX[i] * Wl, starY[i] * GY); r.transform.rotation = Quaternion.Euler(0, 0, -t * 10f - starP[i] * 20f);
                    r.transform.localScale = Vector3.one * (sz * (.8f + .3f * tw) / 50f); r.color = new Color(1f, .96f, .75f, tw * nightFade);
                }
                for (int i = 0; i < NFlies; i++)
                {
                    float a = (.5f + .5f * Mathf.Sin(t * 2f + flyP[i] * 3f)) * nightFade;
                    Vector3 pos = U(flyX[i] * Wl, flyY[i] * Hl);
                    var g = flyGlow.Next(); g.transform.position = pos; g.transform.localScale = Vector3.one * .26f; g.color = new Color(.9f, 1f, .4f, .22f * a);
                    var c = flyCore.Next(); c.transform.position = pos; c.transform.localScale = Vector3.one * .07f; c.color = new Color(1f, 1f, .7f, a);
                }
            }
            skyStars.End(); flyGlow.End(); flyCore.End();
            nightVeil.enabled = on; if (on) { Quad(nightVeil, 0, 0, Wl + 2, Hl + 2); nightVeil.color = new Color(.04f, .06f, .23f, .26f * nightFade); }
            beam.enabled = on; if (on) beam.color = new Color(1f, 1f, 1f, nightFade);
        }

        void Hill(SpriteRenderer[] pair, float period, float par, float baseY)
        {
            float off = (worldX * par) % period;
            for (int i = 0; i < 2; i++) pair[i].transform.position = U(-off + i * period, baseY);
        }

        void DrawGroundDetail(Theme th)
        {
            stems.Begin(); petals.Begin(); centers.Begin(); pebbles.Begin();
            var shellCols = new[] { Palette.Hex("#ff9fbd"), Color.white, Palette.Hex("#ffb35c"), Palette.Hex("#c9a7ff") };
            int k0 = Mathf.FloorToInt(worldX / 110) - 1, k1 = Mathf.FloorToInt((worldX + Wl) / 110) + 1;
            for (int k = k0; k <= k1; k++)
            {
                float h = Hash(k), x = k * 110 + h * 60 - worldX, y = GY - 16 + Hash(k + 7) * 8;
                if (th.shells)
                {
                    Dot(petals.Next(), x, y - 3, 7, 5, shellCols[Idx(Hash(k + 3), shellCols.Length)]);
                    Dot(centers.Next(), x, y - 3, 2.4f, 1.8f, new Color(1, 1, 1, .8f));
                    continue;
                }
                if (th.pines)
                {
                    var tr = stems.Next(); Quad(tr, x - 2f, y - 10, x + 2f, y); tr.color = Palette.Hex("#6b4428");
                    Dot(petals.Next(), x, y - 20, 9, 13, Palette.Hex("#2f8f56"));
                    Dot(centers.Next(), x, y - 28, 5.5f, 4f, new Color(1, 1, 1, .95f));
                    continue;
                }
                if (th.glow)
                {
                    var nc = new[] { Palette.Hex("#7df9ff"), Palette.Hex("#ff9fe5"), Palette.Hex("#fff27a"), Palette.Hex("#b6ff8a") };
                    var gs = stems.Next(); Quad(gs, x - 1.5f, y - 12, x + 1.5f, y); gs.color = Palette.Hex("#1f6a4a");
                    Dot(petals.Next(), x, y - 15, 6.5f, 6.5f, nc[Idx(Hash(k + 3), nc.Length)]);
                    Dot(centers.Next(), x, y - 15, 2.6f, 2.6f, Color.white);
                    continue;
                }
                var col = Palette.Truck[Idx(Hash(k + 3), Palette.Truck.Length)];
                var st = stems.Next(); Quad(st, x - 1.5f, y - 12, x + 1.5f, y); st.color = Palette.Hex("#2f8f45");
                Dot(petals.Next(), x, y - 15, 6, 6, col);
                Dot(centers.Next(), x, y - 15, 2.4f, 2.4f, Color.white);
            }
            int j0 = Mathf.FloorToInt(worldX / 90) - 1, j1 = Mathf.FloorToInt((worldX + Wl) / 90) + 1;
            for (int k = j0; k <= j1; k++)
                for (int n = 0; n < 3; n++)
                {
                    float h = Hash(k * 3 + n + 99), x = k * 90 + Hash(k + n * 17) * 80 - worldX, y = GY + 24 + Hash(k * 5 + n) * Mathf.Max(60, Hl - GY - 40);
                    Dot(pebbles.Next(), x, y, 6 + h * 9, 3 + h * 4, n % 2 == 1 ? th.dirtLt : th.dirtDk);
                }
            stems.End(); petals.End(); centers.End(); pebbles.End();
        }

        /// <summary>Hash value in [0,1] to an array index that can never run off the end.</summary>
        static int Idx(float h, int len) => Mathf.Clamp(Mathf.FloorToInt(h * len), 0, len - 1);
        static float Hash(float k) { float x = Mathf.Sin(k * 127.1f + 311.7f) * 43758.5453f; return x - Mathf.Floor(x); }

        void RenderAnimal(Animal a)
        {
            float sx = a.x - worldX;
            float hop = a.hopT > 0 ? 30f * Mathf.Abs(Mathf.Sin(a.hopT * Mathf.PI * 3)) * a.hopT : 0f;
            float idle = Mathf.Sin(t * 2.2f + a.x) * .02f;
            a.sr.transform.position = U(sx, GY + 6 - hop);
            a.sr.transform.localScale = new Vector3(1 - idle + (a.hopT > 0 ? .05f * Mathf.Sin(a.hopT * 20) : 0), 1 + idle, 1);
        }

        void RenderObj(Obj o)
        {
            float sx = o.x - worldX;
            switch (o.t)
            {
                case 'c':
                    {
                        float e = o.sq * o.sq * (3 - 2 * o.sq);
                        bool near = o.s == 0 && Mathf.Abs(worldX + TX - o.x) < 320;
                        float fly = o.eat > 0 && o.sq >= 1 ? 1 - o.eat / .75f : 0f;
                        Vector2 pos = new Vector2(sx, BASE);
                        float shrink = 1f;
                        if (fly > 0)
                        {
                            float f = fly * fly * (3 - 2 * fly);
                            var m = TruckPt(Def.Mouth);
                            pos = Vector2.Lerp(pos, m, f); pos.y -= 70f * Mathf.Sin(f * Mathf.PI); shrink = 1 - .85f * f;
                        }
                        o.sr.transform.position = U(pos.x, pos.y);
                        o.sr.transform.localScale = new Vector3((1 + .32f * e) * shrink, (1 - .84f * e) * shrink, 1);
                        o.sr.transform.rotation = Quaternion.Euler(0, 0, near ? -Mathf.Sin(t * 40) * .04f * Mathf.Rad2Deg : (fly > 0 ? fly * 360f : 0));
                        bool dizzy = o.s != 0 && o.diz > 0 && fly <= 0;
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
                case 'r': case 'p': case 'k': case 'm': case 'n':
                    o.sr.transform.position = U(sx, BASE);
                    break;
                case 't':
                    {
                        float k = o.tm > 0 ? Mathf.Sin(Mathf.PI * (1 - o.tm / .3f)) : 0f;
                        o.sr.transform.position = U(sx, BASE);
                        o.sr.transform.localScale = new Vector3(1 + .12f * k, 1 - .45f * k, 1);
                        break;
                    }
                case 'j':
                    {
                        float e = o.sq * o.sq * (3 - 2 * o.sq), wob = o.s == 0 ? .04f * Mathf.Sin(t * 6 + o.ph) : .05f * Mathf.Sin(t * 14) * (1 - e);
                        o.sr.transform.position = U(sx, BASE);
                        o.sr.transform.localScale = new Vector3(1 + .25f * e - wob, 1 - .65f * e + wob, 1);
                        break;
                    }
                case 's':
                    o.sr.transform.position = U(sx, BASE);
                    for (int i = 0; i < 2; i++)
                    {
                        float dx = i == 0 ? -70 : 55, hop = o.scare > 0 ? 26f * Mathf.Abs(Mathf.Sin(o.scare * Mathf.PI * 3)) * o.scare : 0f;
                        o.extra[i].transform.position = U(sx + dx, BASE + 4 + Mathf.Sin(t * 3 + i * 2) * 3 - hop);
                        o.extra[i].transform.localScale = Vector3.one * .6f;
                    }
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
            var d = Def;
            truckRoot.position = U(TX, BASE - truck.h);
            float rot = truck.tilt + truck.flipRot;
            pivot.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            bodyT.localPosition = new Vector3(0, -d.PivotY / 100f - truck.bob / 100f, 0);
            for (int i = 0; i < 2; i++) wheels[i].transform.localRotation = Quaternion.Euler(0, 0, -truck.wheel * Mathf.Rad2Deg);

            float ey = Mathf.Clamp(-truck.vh / 180f, -6f, 6f);
            for (int i = 0; i < 2; i++) pupils[i].transform.localPosition = new Vector3(3 / 100f, -ey / 100f, 0);

            // truck-specific moving parts
            if (arm)
            {
                float z = 6f * Mathf.Sin(t * 1.7f);
                if (digT > 0) z += -38f * Mathf.Sin(Mathf.PI * (1 - digT / .7f));
                arm.transform.localRotation = Quaternion.Euler(0, 0, z);
            }
            if (lid) lid.transform.localRotation = Quaternion.Euler(0, 0, lidT > 0 ? 55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(1 - lidT / .9f)) : 0f);
            if (beacon) beacon.color = Mathf.Sin(t * 10) > 0 ? Palette.Truck[0] : Palette.Truck[4];
            if (cloth.enabled)
            {
                cloth.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 9) * 6f);
                cloth.transform.localScale = new Vector3(.9f + .12f * Mathf.Sin(t * 9 + 1), 1, 1);
            }
            if (hat.enabled) hat.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.4f) * 3f - truck.bob * .3f);

            // mud / jelly splats stuck to the body
            while (mudSrs.Count < truck.mud.Count) mudSrs.Add(MakeSR("Mud", Art.Mud, 1, bodyT));
            for (int i = 0; i < mudSrs.Count; i++)
            {
                bool on = i < truck.mud.Count; mudSrs[i].enabled = on;
                if (!on) continue;
                var m = truck.mud[i];
                mudSrs[i].transform.localPosition = new Vector3(m.x / 100f, -m.y / 100f, 0);
                mudSrs[i].transform.localRotation = Quaternion.Euler(0, 0, -m.k * Mathf.Rad2Deg);
                mudSrs[i].transform.localScale = new Vector3(m.r * 1.2f / 14f, m.r / 11f, 1);
                mudSrs[i].color = m.col;
            }

            float sh = Mathf.Max(0, 1 - truck.h / 260f);
            Dot(shadow, TX, BASE + 2, d.HalfW * 1.1f * sh + 20, 12 * sh + 3, new Color(.16f, .12f, .08f, .28f * sh));
        }

        void RenderParts()
        {
            pDot.Begin(); pStar.Begin(); pSq.Begin(); pBubble.Begin();
            pCrate.Begin(); pPlank.Begin(); pBrick.Begin(); pPin.Begin(); pFlame.Begin();
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
                    case PCrate: r = pCrate.Next(); r.transform.localScale = Vector3.one * p.r; c = Color.white; a = Mathf.Clamp01(p.life / p.max * 2f); break;
                    case PPlank: r = pPlank.Next(); r.transform.localScale = Vector3.one * p.r; c = Color.white; a = Mathf.Clamp01(p.life / p.max * 2f); break;
                    case PBrick: r = pBrick.Next(); r.transform.localScale = Vector3.one * p.r; a = Mathf.Clamp01(p.life / p.max * 2f); break;
                    case PPin: r = pPin.Next(); r.transform.localScale = Vector3.one * p.r; c = Color.white; a = Mathf.Clamp01(p.life / p.max * 2f); break;
                    case PFlame:
                        {
                            float k = p.life / p.max;
                            r = pFlame.Next(); r.transform.localScale = Vector3.one * (p.r * (.4f + k) / 14f);
                            c = k > .66f ? new Color(1f, .82f, .3f) : k > .33f ? new Color(1f, .6f, .15f) : new Color(1f, .3f, .1f);
                            a = Mathf.Clamp01(k * 2f); break;
                        }
                    case PStreak: r = pSq.Next(); r.transform.localScale = new Vector3(p.r / 100f, .03f, 1); c = Color.white; a *= .5f; break;
                    default:
                        r = pDot.Next(); r.transform.localScale = Vector3.one * (p.r * (2 - p.life / p.max) * 2 / 100f); a *= .7f; break;
                }
                r.transform.position = U(sx, p.y);
                r.transform.rotation = Quaternion.Euler(0, 0, -p.rot * Mathf.Rad2Deg);
                c.a = a; r.color = c;
            }
            if (snowFade > .01f)
                for (int i = 0; i < Flakes; i++)
                {
                    var r = pDot.Next(); float fr = 1.6f + flakeS[i] * 2.6f;
                    r.transform.position = U(flakeX[i] * Wl, flakeY[i] * Hl); r.transform.rotation = Quaternion.identity;
                    r.transform.localScale = Vector3.one * (fr * 2 / 100f); r.color = new Color(1, 1, 1, .9f * snowFade);
                }
            pDot.End(); pStar.End(); pSq.End(); pBubble.End();
            pCrate.End(); pPlank.End(); pBrick.End(); pPin.End(); pFlame.End();
        }
    }
}
