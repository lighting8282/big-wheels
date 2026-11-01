using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigWheels
{
    // First-person cockpit mode: a pseudo-3D road (OutRun style) seen from inside the truck. The same junkyard props, cars, cogs and creatures
    // come down the road as scaled sprites, and everything in the cab moves: wheel, hands, gauges, switches, dice, wipers, lights.
    public partial class Game
    {
        public bool Cockpit { get; private set; }

        const float PxM = .016f, RoadHalf = 3.15f, HitZ = 6.2f, ObjFar = 190f;
        class CObj
        {
            public char t; public float x, wz, y, size = 1f, sy = 1f, hw = 1.2f, ph, hop; public int ci, kind; public bool done, flip, dead; public SpriteRenderer sr;
        }
        class CP
        {
            public Sprite sp; public Color col; public float wx, wh, wz, vx, vh, vz, rot, vr, size, life, max, grav, grow; public bool rest, glass;
        }
        class Splat { public SpriteRenderer sr; public Vector2 pos; public float alpha, size; public bool on; }

        GameObject cockRoot; Transform cabT;
        SpriteRenderer cSky, cSun, cRoad, cVeil, cFlash, cGlow, cFrame, cHood, cWheelRing, cMark, cCracks, cNitro, cNitroGlow, cNitroFlap, cMic, cShift, cSticker;
        readonly SpriteRenderer[] cCloud = new SpriteRenderer[4], cHillFar = new SpriteRenderer[2], cHillNear = new SpriteRenderer[2];
        readonly SpriteRenderer[] cSpokes = new SpriteRenderer[3], cGloves = new SpriteRenderer[2], cSleeves = new SpriteRenderer[2];
        readonly SpriteRenderer[] cNeedles = new SpriteRenderer[4], cLevers = new SpriteRenderer[4], cDice = new SpriteRenderer[2], cWipers = new SpriteRenderer[2];
        readonly SpriteRenderer[] cPips = new SpriteRenderer[4], cHoodFlames = new SpriteRenderer[2], cLeds = new SpriteRenderer[3], cLights = new SpriteRenderer[2], cStreaks = new SpriteRenderer[26];
        readonly Splat[] splats = new Splat[24];
        readonly List<CObj> cobjs = new();
        readonly List<CP> cps = new();
        readonly List<SpriteRenderer> cpSrs = new();
        Pool cFlakes;

        float cz, cpx, cpv, csteer, cspeed = 20f, cair, cairV, cairT, ccurv, cskyX, cDamage, cFlashT, cBob, cBobV, cHeat, cRpm = .3f, cTurbo, cShiftT, cWipePh, cNitroGlowT;
        float cNextRow = 45f, cNextAnimal = 70f, cNextScene = 25f, cPtrX, lastPv;
        int cThemeIdx = -1, cGear;
        bool cPtrDown, cWipeActive, cWipeReq, nitroReady;
        readonly float[] cHand = { -55f, 55f }, cDiceA = new float[2], cDiceV = new float[2], cCloudX = new float[4];
        float dbgSteer = float.NaN, dbgGas;
        Color32[] roadPix; Texture2D roadTex; int roadW, roadH;
        float cyh, cf, chc, ccx;                     // camera: horizon line, focal length, height, centre x (logical px / metres)

        static readonly Color[] RoadA = { Palette.Hex("#34363e"), Palette.Hex("#4a3a38"), Palette.Hex("#4a5058"), Palette.Hex("#23252e") };
        static readonly Color[] RoadB = { Palette.Hex("#2f3138"), Palette.Hex("#443533"), Palette.Hex("#434a52"), Palette.Hex("#202229") };
        static readonly Color[] HorizonCol = { Palette.Hex("#ff8a3d"), Palette.Hex("#ffe0a0"), Palette.Hex("#cfd8e2"), Palette.Hex("#3a2a5a") };

        // ---------- switching between the side view and the cab ----------
        public void ToggleView() => SetCockpit(!Cockpit);

        public void SetCockpit(bool on)
        {
            if (Cockpit == on || cockRoot == null) return;
            Cockpit = on;
            worldRoot.gameObject.SetActive(!on); cockRoot.SetActive(on); hud.SetCockpit(on);
            cPtrDown = false;
            if (on) { cThemeIdx = -1; ClearCockpit(); }
            Sfx.Play("swoosh", .6f);
        }

        void ClearCockpit()
        {
            foreach (var o in cobjs) if (o.sr) Kill(o.sr.gameObject);
            cobjs.Clear(); cps.Clear();
            foreach (var s in splats) { s.on = false; s.sr.enabled = false; }
            cz = 0; cpx = 0; cpv = 0; csteer = 0; cair = 0; cairV = 0; cDamage = 0; nitroReady = false;
            cNextRow = 45f; cNextAnimal = 70f; cNextScene = 25f;
        }

        public void DebugDrive(float steer, float gas) { dbgSteer = steer; dbgGas = gas; }
        public void DebugCSpawn(char type, float dz, float x) { CAdd(type, x, cz + dz); }
        public void DebugNitro() { nitroReady = true; CockpitNitro(); }
        public float CabX => cpx;
        public int CockpitObjects => cobjs.Count;

        // ---------- pointer (touch or mouse) steering ----------
        public void PointerMove(Vector2 px) { if (Cockpit && cPtrDown) cPtrX = px.x; }
        public void PointerUp() { cPtrDown = false; }

        float CabScale() => Mathf.Min(Wl / 760f, Hl * .0024f);

        void CockpitTap(float lx, float ly, Vector2 px)
        {
            float s = CabScale(), bx = Wl * .5f + 263f * s, by = Hl - 30f * s, r = 42f * s;
            if (nitroReady && (new Vector2(lx, ly) - new Vector2(bx, by)).sqrMagnitude < r * r) { CockpitNitro(); return; }
            cPtrDown = true; cPtrX = px.x;
        }

        /// <summary>Hop the truck: a short jump that clears low props and cars, with a thump on landing.</summary>
        public void CockpitJump()
        {
            if (!Cockpit || !Started || paused || cair > 0f || cairV > 0f) return;
            cairV = 8.6f; cair = .02f; cairT = 0f; cBobV += 140f;
            Sfx.Play("boing"); Horn(); AddShake(2f);
        }
        public void DebugJump() { CockpitJump(); }
        public bool Airborne => cair > 0f || cairV > 0f;

        void CockpitNitro()
        {
            if (!nitroReady || turboT > 0) return;
            nitroReady = false; StartTurbo(); cNitroGlowT = 1f;
        }

        // ================= building the cab and the road scene (once) =================
        void CockpitInit()
        {
            CabArt.Build();
            cockRoot = new GameObject("Cockpit");
            Transform R = cockRoot.transform;
            SpriteRenderer S(string n, Sprite sp, int order) => MakeSR(n, sp, order, R);

            cSky = S("Sky", Art.Sky, 0); cSun = S("Sun", Art.SunNormal, 1);
            for (int i = 0; i < cCloud.Length; i++) { cCloud[i] = S("Smoke", Art.Cloud, 2); cCloudX[i] = i * 300 + Random.Range(0, 120f); }
            for (int i = 0; i < 2; i++) { cHillFar[i] = S("HillFar", Art.HillFar, 3); cHillNear[i] = S("HillNear", Art.HillNear, 4); }
            cRoad = S("Road", null, 5);
            for (int i = 0; i < cLights.Length; i++) { cLights[i] = S("Headlight", Art.Dot, 6); cLights[i].color = new Color(1, 1, 1, 0); }
            cVeil = S("Veil", Art.White, 1150); cVeil.color = new Color(.04f, .06f, .23f, 0);
            cGlow = S("Glow", Art.White, 1152); cGlow.color = new Color(1, .5f, .1f, 0);
            for (int i = 0; i < cStreaks.Length; i++) { cStreaks[i] = S("Streak", Art.White, 1154); cStreaks[i].color = new Color(1, 1, 1, 0); }
            cFlakes = new Pool(this, Art.Dot, 1156, R);
            cFlash = S("Flash", Art.White, 2100); cFlash.color = new Color(1, 1, 1, 0);

            cabT = new GameObject("Cab").transform; cabT.SetParent(R, false);
            SpriteRenderer P(Sprite sp, float x, float y, int order)
            {
                var r = MakeSR("Cab", sp, order, cabT); r.transform.localPosition = new Vector3(x / 100f, -y / 100f, 0); return r;
            }
            for (int i = 0; i < splats.Length; i++) { splats[i] = new Splat { sr = P(Art.Mud, 0, 0, 1600) }; splats[i].sr.enabled = false; }
            cCracks = P(CabArt.Cracks, 0, 0, 1610); cCracks.color = new Color(1, 1, 1, 0);
            cWipers[0] = P(CabArt.Wiper, -150, -62, 1620); cWipers[1] = P(CabArt.Wiper, 150, -62, 1620);
            cHood = P(CabArt.Hood[0], 0, 0, 1700);
            cHoodFlames[0] = P(SmashArt.Flame, -176, -64, 1705); cHoodFlames[1] = P(SmashArt.Flame, 176, -64, 1705);
            cFrame = P(CabArt.Frame, 0, 0, 1800);
            cSticker = P(CabArt.DashSticker[0], -300, -28, 1811);
            float[,] g = { { -228, -26, 1f }, { -160, -22, .8f }, { 176, -22, .55f }, { 212, -26, .72f } };
            for (int i = 0; i < 4; i++) { cNeedles[i] = P(CabArt.Needle, g[i, 0], g[i, 1], 1810); cNeedles[i].transform.localScale = Vector3.one * g[i, 2]; }
            for (int i = 0; i < 4; i++) cLevers[i] = P(CabArt.Lever, 142 + i * 22, -43, 1812);
            float[,] led = { { 330, -32 }, { -120, -48 }, { 154, -396 } };
            for (int i = 0; i < 3; i++) { cLeds[i] = P(Art.Dot, led[i, 0], led[i, 1], 1813); cLeds[i].transform.localScale = Vector3.one * .07f; }
            cNitroGlow = P(Art.Dot, 263, -30, 1814); cNitroGlow.transform.localScale = Vector3.one * .5f; cNitroGlow.color = new Color(1, .5f, .1f, 0);
            cNitro = P(CabArt.NitroBtn, 263, -30, 1815);
            cNitroFlap = P(CabArt.NitroFlap, 263, -46, 1816);
            for (int i = 0; i < 4; i++) { cPips[i] = P(SmashArt.Flame, -168 + i * 24, -392, 1816); cPips[i].transform.localScale = Vector3.one * .5f; }
            cShift = P(CabArt.Lever, 305, -20, 1817); cShift.transform.localScale = Vector3.one * 3.2f; cShift.color = new Color(1f, .45f, .4f);
            cWheelRing = P(CabArt.WheelRing, 0, 0, 1900);
            for (int i = 0; i < 3; i++) cSpokes[i] = P(CabArt.Spoke, 0, 14, 1905);
            cMark = P(Art.Roundrect, 0, 0, 1906); cMark.transform.localScale = new Vector3(.3f, .12f, 1); cMark.color = Palette.Hex("#d33a2c");
            for (int i = 0; i < 2; i++) { cSleeves[i] = P(CabArt.Sleeve, 0, 0, 1908); cGloves[i] = P(CabArt.Glove, 0, 0, 1910); }
            cDice[0] = P(CabArt.Dice, -12, -368, 2001); cDice[1] = P(CabArt.Dice, 14, -366, 2001); cDice[1].color = new Color(1f, .8f, .6f);
            cMic = P(CabArt.Mic, 172, -378, 2002);
            cockRoot.SetActive(false);
        }

        // ================= input =================
        void ReadInput(out float steer, out float gas, out bool nitro, out bool wipe)
        {
            steer = 0; gas = 0; nitro = false; wipe = false;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) steer -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) steer += 1f;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) gas += 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) gas -= 1f;
                if (kb.enterKey.wasPressedThisFrame || kb.nKey.wasPressedThisFrame) nitro = true;
                if (kb.zKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) wipe = true;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                steer += gp.leftStick.x.ReadValue();
                gas += gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue();
                if (gp.buttonEast.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame) nitro = true;
                if (gp.buttonWest.wasPressedThisFrame) wipe = true;
            }
            if (PedalGas) gas += 1f;
            if (PedalBrake) gas -= 1f;
            if (cPtrDown) steer = Mathf.Clamp((cPtrX / Mathf.Max(1f, W) - .5f) * 3f, -1f, 1f);
            steer = Mathf.Clamp(steer, -1f, 1f); gas = Mathf.Clamp(gas, -1f, 1f);
            if (!float.IsNaN(dbgSteer)) { steer = dbgSteer; gas = dbgGas; }
        }

        // ================= objects on the road =================
        char PickType()
        {
            float r = Random.value;
            return r < .12f ? 'k' : r < .21f ? 'm' : r < .29f ? 'n' : r < .39f ? 'x' : r < .46f ? 'g' : r < .52f ? 'f' : r < .70f ? 'c' : r < .82f ? 'b' : r < .88f ? 't' : r < .95f ? 'j' : 'p';
        }

        CObj CAdd(char type, float x, float wz)
        {
            var o = new CObj { t = type, x = x, wz = wz, ci = Random.Range(0, 7), ph = Random.value * 6f };
            Sprite sp;
            switch (type)
            {
                case 'c': sp = Art.Car[o.ci]; o.size = 1.15f; break;
                case 'b': sp = Art.Balloon[o.ci]; o.y = Random.Range(1.3f, 3f); o.hw = 1.6f; break;
                case 't': sp = Art2.Trampoline; o.hw = 1.8f; break;
                case 'j': o.ci = Random.Range(0, 4); sp = Art2.Jelly[o.ci]; o.hw = 1.5f; break;
                case 'p': sp = Art.Puddle; o.hw = 1.8f; o.sy = .5f; o.size = 1.3f; break;
                default: sp = SmashSprite(type); o.hw = (type == 'g' || type == 'f') ? 1.5f : type == 'n' ? 1.4f : 1.2f; break;
            }
            o.sr = MakeSR("C", sp, 100, cockRoot.transform);
            cobjs.Add(o); return o;
        }

        void CAddAnimal(float x, float wz)
        {
            int kind = World == 0 ? Random.Range(0, 3) : World == 1 ? Random.Range(3, 5) : World == 2 ? Random.Range(5, 8) : Random.Range(8, 11);
            var o = new CObj { t = 'a', x = x, wz = wz, kind = kind, flip = x > 0, size = 1.25f, ph = Random.value * 6f };
            o.sr = MakeSR("Creature", Art2.Animal[kind], 100, cockRoot.transform);
            cobjs.Add(o);
        }

        void CAddScenery(float x, float wz)
        {
            int[] pick = World == 0 ? new[] { 3, 0 } : World == 1 ? new[] { 1, 4 } : World == 2 ? new[] { 2, 4 } : new[] { 0, 0 };
            int idx = pick[Random.Range(0, pick.Length)];
            var o = new CObj { t = 's', x = x, wz = wz, kind = idx, flip = x > 0 && Random.value < .5f };
            o.size = idx == 0 ? 3.2f : idx == 1 ? 2.2f : idx == 2 ? 3f : idx == 3 ? 2.4f : 2.2f;
            o.sr = MakeSR("Scenery", CabArt.Scenery[idx], 100, cockRoot.transform);
            cobjs.Add(o);
        }

        void SpawnRows()
        {
            float horizon = cz + 175f;
            while (cNextRow < horizon)
            {
                float[] lanes = { -2.1f, 0f, 2.1f };
                for (int i = 2; i > 0; i--) { int j = Random.Range(0, i + 1); (lanes[i], lanes[j]) = (lanes[j], lanes[i]); }
                int n = 1 + (Random.value < .5f ? 1 : 0) + (Random.value < .2f ? 1 : 0);
                for (int i = 0; i < n; i++) CAdd(PickType(), lanes[i] + Random.Range(-.25f, .25f), cNextRow);
                cNextRow += Random.Range(17f, 33f);
            }
            while (cNextAnimal < horizon)
            {
                CAddAnimal((Random.value < .5f ? -1f : 1f) * Random.Range(4.8f, 7f), cNextAnimal);
                cNextAnimal += Random.Range(40f, 85f);
            }
            while (cNextScene < horizon)
            {
                CAddScenery((Random.value < .5f ? -1f : 1f) * Random.Range(7f, 15f), cNextScene);
                cNextScene += Random.Range(12f, 26f);
            }
        }

        // ================= flying pieces =================
        void CBurst(Sprite sp, int n, Vector3 at, float spread, Color col, float sMin, float sMax, float up, float fwd, float grav = 16f, float life = 3f)
        {
            for (int i = 0; i < n; i++)
            {
                var p = new CP
                {
                    sp = sp, col = col, wx = at.x + Random.Range(-spread, spread), wh = at.y + Random.Range(-.5f, .9f), wz = at.z + Random.Range(-.8f, .8f),
                    vx = Random.Range(-5f, 5f), vh = Random.Range(.3f, 1f) * up, vz = Random.Range(-.1f, 1f) * fwd,
                    rot = Random.Range(0f, 360f), vr = Random.Range(-540f, 540f), size = Random.Range(sMin, sMax), life = Random.Range(life * .6f, life), grav = grav
                };
                p.max = p.life; cps.Add(p);
            }
            while (cps.Count > 280) cps.RemoveAt(0);
        }

        void CStar(float wx, float wh, float wz, float size, Color col)
        {
            cps.Add(new CP { sp = Art.Star, col = col, wx = wx, wh = wh, wz = wz, size = size, life = .28f, max = .28f, grow = .9f, vr = 80f });
        }

        void AddSplat(Color col, int n, float sizeMul = 1f)
        {
            for (int i = 0; i < n; i++)
            {
                Splat s = null;
                foreach (var c in splats) if (!c.on) { s = c; break; }
                if (s == null) s = splats[Random.Range(0, splats.Length)];
                s.on = true; s.pos = new Vector2(Random.Range(-170f, 170f), Random.Range(-290f, -105f));
                s.size = Random.Range(.45f, 1f) * sizeMul; s.alpha = 1f; s.sr.color = col; s.sr.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }
        }

        // ================= what happens when the truck reaches something =================
        void CockpitHit(CObj o)
        {
            float dx = Mathf.Abs(o.x - cpx);
            switch (o.t)
            {
                case 'k': case 'm': case 'n': case 'x': case 'g': case 'f':
                    if (cair > 1.1f || dx > o.hw + 1.5f) return; CSmash(o); break;
                case 'c':
                    if (cair > 1.4f || dx > o.hw + 1.5f) return; CarHit(o); break;
                case 'b':
                    if (dx > o.hw + 1.1f) return;
                    o.done = true; o.dead = true; comboT = 1.6f; Sfx.Pop(popCombo++);
                    CBurst(Art.Star, 8, new Vector3(o.x, o.y, o.wz - 1f), .6f, Palette.Truck[o.ci], .35f, .6f, 6f, 6f, 8f, 1f);
                    AddScorePt(10, Wl * .5f / Wl + (o.x - cpx) * .05f, .5f); Award(16); break;
                case 't':
                    if (dx > o.hw + .8f || cair > .3f) return;
                    o.done = true; cairV = 9.5f; cair = .02f; cairT = 0; Sfx.Play("tramp"); AddShake(5f); cBobV += 60f;
                    CBurst(Art.Star, 8, new Vector3(o.x, .3f, o.wz - 1f), 1.2f, Palette.Sun, .4f, .7f, 5f, 4f, 10f, .9f); Award(20); break;
                case 'j': case 'p':
                    if (dx > o.hw + .9f || cair > .6f) return;
                    o.done = true; o.dead = o.t == 'j';
                    if (o.t == 'j') { var c = Palette.Truck[Art2.JellyPalette[o.ci]]; AddSplat(c, 3, 1f); Sfx.Play("splat"); AddScorePt(30, .5f, .5f); Award(18); }
                    else { AddSplat(Palette.Mud, 2, .9f); Sfx.Play("splash"); Award(22); }
                    cBobV += 40f; break;
            }
        }

        void CarHit(CObj o)
        {
            o.done = true; o.dead = true;
            Sfx.Play("squash"); AddShake(5f); FeedCharge(); AddScorePt(50, .5f + (o.x - cpx) * .05f, .5f);
            var at = new Vector3(o.x, .6f, o.wz);
            cps.Add(new CP { sp = Art.Car[o.ci], col = Color.white, wx = at.x, wh = at.y, wz = at.z, vx = Random.Range(-3f, 3f), vh = 11f, vz = cspeed + 9f, rot = 0, vr = Random.Range(-420f, 420f), size = 1.15f, life = 2.2f, max = 2.2f, grav = 16f });
            CBurst(SmashArt.Brick, 9, at, 1.2f, Palette.Truck[o.ci], .5f, .85f, 8f, 16f);
            cBobV += 90f;
            if (Celeb > 0) { Sfx.Say("yay", false); return; }
            int n = Slots++;
            SlotCols[n] = o.ci; SlotPop[n] = 1;
            Sfx.Say(new[] { "one", "two", "three" }[n], true);
            if (Slots >= SlotCount) { Celeb = 2.6f; Sfx.Play("fanfare"); Award(17); AddScorePt(300, .5f, .4f); cFlashT = .5f; }
        }

        void CSmash(CObj o)
        {
            if (o.done) return;
            o.done = true; o.dead = true;
            bool tb = turboT > 0; float k = tb ? 1.3f : 1f;
            var at = new Vector3(o.x, 1f, o.wz);
            Color wood = Palette.Hex("#d9a05b");
            int pts = 100;
            switch (o.t)
            {
                case 'k':
                    Sfx.Play("crash");
                    CBurst(SmashArt.CrateBit, 8, at, 1.4f, Color.white, 1f, 1.5f, 9f * k, 20f * k);
                    CBurst(SmashArt.Plank, 18, at, 1.4f, wood, .8f, 1.3f, 9f * k, 22f * k); break;
                case 'm':
                    Sfx.Play("crunch"); Award(29); pts = 150;
                    CBurst(SmashArt.Brick, 28, at, 1.4f, Palette.Hex("#ee6a4c"), .7f, 1.2f, 9f * k, 22f * k);
                    CBurst(SmashArt.Brick, 10, at, 1.4f, Palette.Hex("#ffb08a"), .6f, 1f, 9f * k, 22f * k); break;
                case 'n':
                    Sfx.Play("tires"); pts = 150;
                    CBurst(SmashArt.Pin, 8, at, 1.4f, Color.white, 1.1f, 1.7f, 11f * k, 22f * k, 15f, 3.4f); break;
                case 'g':
                    Sfx.Play("shatter"); pts = 120; cDamage += .25f;
                    CBurst(SmashArt.Shard, 36, at, 1.5f, Palette.Hex("#bfe8ff"), .8f, 1.6f, 10f * k, 22f * k, 14f, 3f);
                    CBurst(Art.Star, 10, at, 1.2f, Color.white, .4f, .8f, 8f, 10f, 10f, .8f); break;
                case 'f':
                    Sfx.Play("snap"); pts = 100;
                    CBurst(SmashArt.Plank, 18, at, 1.5f, new Color(1f, .97f, .88f), .7f, 1.2f, 8f * k, 22f * k); break;
                default: // 'x'
                    Sfx.Play("boom"); pts = 250; cFlashT = .4f;
                    CBurst(SmashArt.FlameBlob, 30, at, 1.4f, Palette.Hex("#ffb347"), 1f, 2.2f, 7f, 8f, -3f, 1f);
                    CBurst(Art.Dot, 12, at, 1.4f, new Color(.28f, .28f, .32f, .8f), .5f, 1.1f, 4f, 4f, -1f, 1.6f);
                    CBurst(SmashArt.Plank, 14, at, 1.2f, new Color(.85f, .3f, .24f), .8f, 1.2f, 12f, 22f);
                    CStar(o.x - .6f, 1.6f, o.wz - 1f, 1.8f, Palette.Hex("#ff7a1a")); break;
            }
            CStar(o.x, 1.2f, Mathf.Max(o.wz - 1.5f, cz + 4.5f), 1.3f, Palette.Hex("#ff7a1a")); CStar(o.x, 1.2f, Mathf.Max(o.wz - 1.6f, cz + 4.4f), .8f, Color.white);
            CBurst(Art.Dot, 7, at, 1.4f, new Color(.87f, .82f, .74f, .8f), .4f, .8f, 3f, 5f, -1f, 1.1f);
            bool boom = o.t == 'x';
            AddShake(boom ? 15f : tb ? 9f : 6f);
            hitStop = boom ? .12f : tb ? .05f : .09f;
            cBobV += boom ? 160f : 100f;
            AddScorePt(pts, .5f + (o.x - cpx) * .05f, .46f);
            FeedCharge();
            if (boom)
                foreach (var other in cobjs)
                {
                    if (other.done || other == o || Mathf.Abs(other.wz - o.wz) > 18f || Mathf.Abs(other.x - o.x) > 9f) continue;
                    if (IsSmashable(other.t)) CSmash(other); else if (other.t == 'c') CarHit(other);
                }
        }

        // ================= one step of the cockpit world =================
        void CockpitUpdate(float dt)
        {
            t += dt;
            if (Started && Settings.BreakMinutes > 0)
            {
                playSeconds += dt;
                if (playSeconds >= Settings.BreakMinutes * 60f) { playSeconds = 0; hud.ShowBreak(); }
            }
            var kbp = Keyboard.current; var gpp = Gamepad.current;
            ReadInput(out float steerIn, out float gas, out bool nitro, out bool wipe);
            if (!Started) { steerIn = Mathf.Sin(t * .6f) * .5f; gas = 0; nitro = false; wipe = false; }
            if (nitro) CockpitNitro();
            if ((kbp != null && kbp.spaceKey.wasPressedThisFrame) || (gpp != null && gpp.buttonSouth.wasPressedThisFrame)) CockpitJump();
            if ((kbp != null && kbp.pKey.wasPressedThisFrame) || (gpp != null && gpp.buttonNorth.wasPressedThisFrame)) SetColor((ColorIndex + 1) % Palette.Truck.Length);
            if (wipe) cWipeReq = true;

            // turbo, shake and score timers (the side-view versions run in UpdateSim, which does not run here)
            shakeMag = Mathf.Max(0, shakeMag - dt * 40f);
            if (turboT > 0) { turboT -= dt; if (turboT <= 0) { turboT = 0; charge = 0; Sfx.Play("swoosh", .6f); } }
            cTurbo += ((turboT > 0 ? 1f : 0f) - cTurbo) * Mathf.Min(1f, dt * 3f);
            StepScore(dt);
            if (comboT > 0) { comboT -= dt; if (comboT <= 0) popCombo = 0; }
            for (int i = 0; i < SlotCount; i++) if (SlotPop[i] > 0) SlotPop[i] = Mathf.Max(0, SlotPop[i] - dt * 2.2f);
            if (Celeb > 0) { Celeb -= dt; if (Celeb <= 0) { Slots = 0; System.Array.Clear(SlotPop, 0, SlotCount); } }
            nightFade += ((World == 3 ? 1f : 0f) - nightFade) * Mathf.Min(1f, dt * 2f);
            snowFade += ((World == 2 ? 1f : 0f) - snowFade) * Mathf.Min(1f, dt * 2f);
            cFlashT = Mathf.Max(0, cFlashT - dt * 1.8f);
            worldTimer += dt;
            if (worldTimer > WorldSeconds && Celeb <= 0 && Settings.WorldLock < 0) { worldTimer = 0; cFlashT = 1f; ApplyTheme((World + 1) % themes.Length); }

            // driving
            ccurv = .0014f * Mathf.Sin(cz * .0055f) + .0007f * Mathf.Sin(cz * .013f);
            float target = 22f + gas * 9f; if (!Started) target = 18f;
            if (turboT > 0) target *= 1.5f;
            if (Mathf.Abs(cpx) > 3.5f) target *= .65f;
            cspeed += (target - cspeed) * Mathf.Min(1f, dt * (gas >= 0f ? 1.1f : 2.2f));
            csteer += (steerIn - csteer) * Mathf.Min(1f, dt * 7f);
            float maxLat = 9f * Mathf.Clamp(cspeed / 22f, .6f, 1.25f);
            cpv += (csteer * maxLat - cpv) * Mathf.Min(1f, dt * 4f);
            cpx += cpv * dt - ccurv * cspeed * cspeed * .35f * dt;
            if (Mathf.Abs(cpx) > 5.4f) { cpx = Mathf.Sign(cpx) * 5.4f; cpv *= -.2f; AddShake(4f); Sfx.Play("thump", .4f); }
            if (Mathf.Abs(cpx) > 3.5f) shakeMag = Mathf.Max(shakeMag, 1.4f);
            float dz = cspeed * dt;
            cz += dz;
            cskyX += (ccurv * cspeed * 1500f + cpv * 4f) * dt;

            // jumping off a launch pad
            if (cair > 0f || cairV > 0f)
            {
                cairT += dt; cairV -= 22f * dt; cair += cairV * dt;
                if (cair <= 0f)
                {
                    cair = 0f;
                    if (cairV < -3f) { Sfx.Play("thump"); AddShake(8f); cBobV += 200f; if (cairT > .9f) AddScorePt(100, .5f, .42f); }
                    cairV = 0f;
                }
            }

            // spring for the hood and cab
            cBobV += (-160f * cBob - 12f * cBobV) * dt; cBob += cBobV * dt; cBob = Mathf.Clamp(cBob, -14f, 18f);
            cBobV += Mathf.Sin(cz * 3.1f) * cspeed * .02f;

            SpawnRows();

            // objects: move, hit, remove
            for (int i = cobjs.Count - 1; i >= 0; i--)
            {
                var o = cobjs[i];
                float rz = o.wz - cz, prev = rz + dz;
                if (o.t == 'a') { if (!o.done && rz < 26f) { o.done = true; o.hop = 1f; Sfx.Play(AnimalSfx[o.kind], .5f); } if (o.hop > 0) o.hop = Mathf.Max(0, o.hop - dt / .8f); }
                else if (o.t != 's' && !o.done && prev >= HitZ && rz < HitZ) CockpitHit(o);
                if (o.dead || rz < .6f) { if (o.sr) Kill(o.sr.gameObject); cobjs.RemoveAt(i); }
            }

            // flying pieces
            for (int i = cps.Count - 1; i >= 0; i--)
            {
                var p = cps[i];
                p.life -= dt;
                if (p.life <= 0f) { cps.RemoveAt(i); continue; }
                p.vh -= p.grav * dt; p.wh += p.vh * dt; p.wx += p.vx * dt; p.wz += p.vz * dt; p.rot += p.vr * dt;
                if (p.grav > 0f && p.wh < 0f && p.vh < 0f)
                {
                    p.wh = 0f;
                    if (-p.vh > 2.5f) { p.vh = -p.vh * .38f; p.vx *= .8f; p.vz *= .8f; p.vr *= .6f; }
                    else { p.vh = 0f; p.rest = true; }
                }
                if (p.rest) { p.vz *= Mathf.Max(0f, 1f - 4f * dt); p.vx *= Mathf.Max(0f, 1f - 4f * dt); p.vr *= Mathf.Max(0f, 1f - 5f * dt); }
                float rz = p.wz - cz;
                if (p.grav > 0f && !p.rest && rz < 1.7f && rz > .2f && Mathf.Abs(p.wx - cpx) < 2.2f && p.wh > .5f && p.wh < 3.4f)
                {                                       // something hits the windshield
                    cDamage = Mathf.Min(1.2f, cDamage + .05f); AddShake(2.5f); cBobV += 30f;
                    if (Random.value < .12f) AddSplat(p.col.a > .5f && p.sp == Art.Dot ? p.col : Palette.Mud, 1, .8f);
                    cps.RemoveAt(i); continue;
                }
                if (rz < .3f) cps.RemoveAt(i);
            }
            if (cDamage > 0f) cDamage = Mathf.Max(0f, cDamage - dt * .02f);

            // windshield: mud, wipers
            int dirty = 0; foreach (var s in splats) if (s.on) dirty++;
            if (dirty >= 2 && !cWipeActive) cWipeReq = true;
            if (cWipeReq && !cWipeActive) { cWipeActive = true; cWipePh = 0f; cWipeReq = false; Sfx.Play("wipe", .5f); }
            if (cWipeActive) { cWipePh += dt / .7f; if (cWipePh >= 1f) { cWipeActive = false; cWipePh = 0f; } }

            // the dice, mirror and mic swing about with every turn and bump
            float latAcc = (cpv - lastPv) / Mathf.Max(.001f, dt); lastPv = cpv;
            for (int i = 0; i < 2; i++)
            {
                float k = i == 0 ? 60f : 48f, c = i == 0 ? 3.4f : 2.8f, sdt = Mathf.Min(dt, .02f);   // a small fixed step keeps the spring stable
                cDiceV[i] += (-k * cDiceA[i] - c * cDiceV[i] - latAcc * 16f + cBobV * .4f) * sdt;
                cDiceA[i] = Mathf.Clamp(cDiceA[i] + cDiceV[i] * sdt, -55f, 55f);
                if (float.IsNaN(cDiceA[i]) || float.IsNaN(cDiceV[i])) { cDiceA[i] = 0f; cDiceV[i] = 0f; }
            }
            cHeat = Mathf.Clamp01(cHeat + dt * (turboT > 0 ? .06f : -.02f));
            cNitroGlowT = Mathf.Max(0, cNitroGlowT - dt * 2f);
            Sfx.SetEngine(46 + cspeed * .5f + (turboT > 0 ? 14 : 0) + 4f * Mathf.Sin(t * 14f));
        }

        // ================= drawing =================
        float RoadOff(float rz) => ccurv * rz * rz * .5f;

        void EnsureRoad()
        {
            int rw = 512, rh = Mathf.Max(16, Mathf.CeilToInt(rw * (Hl * .6f) / Wl));
            if (roadTex != null && roadTex.width == rw && roadTex.height == rh) return;
            if (roadTex != null) Destroy(roadTex);
            roadTex = new Texture2D(rw, rh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            roadPix = new Color32[rw * rh]; roadW = rw; roadH = rh;
            cRoad.sprite = Sprite.Create(roadTex, new Rect(0, 0, rw, rh), Vector2.zero, rw / (Wl / 100f), 0, SpriteMeshType.FullRect);
        }

        void SpanFill(int o, float x0, float x1, Color32 c)
        {
            int a = Mathf.Clamp(Mathf.RoundToInt(x0 / Wl * roadW), 0, roadW), b = Mathf.Clamp(Mathf.RoundToInt(x1 / Wl * roadW), 0, roadW);
            for (int i = a; i < b; i++) roadPix[o + i] = c;
        }

        void DrawRoad()
        {
            EnsureRoad();
            var th = themes[World];
            Color rA = RoadA[World], rB = RoadB[World], gA = th.dirt, gB = th.dirtDk, shoulder = th.grass, hz = HorizonCol[World];
            Color32 edge = new Color32(240, 180, 0, 255), lane = new Color32(207, 211, 220, 255), rumbleR = new Color32(190, 40, 40, 255), rumbleW = new Color32(225, 225, 230, 255);
            float yTop = Hl * .4f, rowH = (Hl - yTop) / roadH;
            for (int r = 0; r < roadH; r++)
            {
                int o = r * roadW;
                float yRow = Hl - (r + .5f) * rowH, dy = yRow - cyh;
                if (dy < 1.2f) { for (int i = 0; i < roadW; i++) roadPix[o + i] = default; continue; }
                float z = cf * chc / dy, k = dy / chc, fog = Mathf.Clamp01((z - 60f) / 150f);
                int band = Mathf.FloorToInt((z + cz) / 7f) & 1;
                float cxr = ccx + (RoadOff(z) - cpx) * k, half = RoadHalf * k;
                SpanFill(o, 0, Wl, (Color32)Color.Lerp(Color.Lerp(gA, gB, band), hz, fog));
                SpanFill(o, cxr - half - 1.4f * k, cxr + half + 1.4f * k, (Color32)Color.Lerp(shoulder, hz, fog));
                Color32 rum = band == 0 ? rumbleR : rumbleW;
                SpanFill(o, cxr - half - .55f * k, cxr - half, (Color32)Color.Lerp(rum, hz, fog)); SpanFill(o, cxr + half, cxr + half + .55f * k, (Color32)Color.Lerp(rum, hz, fog));
                SpanFill(o, cxr - half, cxr + half, (Color32)Color.Lerp(band == 0 ? rA : rB, hz, fog));
                SpanFill(o, cxr - half, cxr - half + .2f * k, (Color32)Color.Lerp(edge, hz, fog)); SpanFill(o, cxr + half - .2f * k, cxr + half, (Color32)Color.Lerp(edge, hz, fog));
                if (Mathf.Repeat(z + cz, 8f) < 4.2f)
                {
                    Color32 ln = (Color32)Color.Lerp(lane, hz, fog);
                    float d = .09f * k;
                    SpanFill(o, cxr - 1.05f * k - d, cxr - 1.05f * k + d, ln); SpanFill(o, cxr + 1.05f * k - d, cxr + 1.05f * k + d, ln);
                }
            }
            roadTex.SetPixelData(roadPix, 0); roadTex.Apply(false);
        }

        void CockpitTheme()
        {
            if (cThemeIdx == World) return;
            cThemeIdx = World; var th = themes[World];
            cSky.sprite = th.sky; cSun.sprite = th.sun;
            for (int i = 0; i < 2; i++) { cHillFar[i].sprite = th.hillFar; cHillNear[i].sprite = th.hillNear; }
            Camera.main.backgroundColor = th.bg;
        }

        /// <summary>Projects a point ahead of the truck (rz metres away, lateral wx, height wh) onto the screen.</summary>
        Vector2 Proj(float rz, float wx, float wh, out float k)
        {
            k = cf / rz;
            return new Vector2(ccx + (wx + RoadOff(rz) - cpx) * k, cyh + (chc - wh) * k);
        }

        void CockpitRender()
        {
            CockpitTheme();
            cam.orthographicSize = Hl / 200f;
            cam.transform.position = new Vector3(Wl / 200f, -Hl / 200f, -10) + ShakeOffset();
            cyh = Hl * .4f + cair * 12f; cf = Mathf.Min(Hl, Wl * .62f) * (1.15f + .12f * cTurbo); chc = 1.8f + cair; ccx = Wl * .5f;
            var th = themes[World];

            // sky, sun, smoke, skylines
            cSky.transform.position = U(0, 0); cSky.transform.localScale = new Vector3((Wl + 2) / 100f / .04f, cyh / 100f / .64f, 1);
            float sh = Hl / 560f;
            cSun.transform.position = U(Mathf.Repeat(Wl * .72f - cskyX * .05f + Wl * .25f, Wl * 1.5f) - Wl * .25f, cyh - Hl * .12f); cSun.transform.localScale = Vector3.one * .9f * sh;
            float span = Wl + 400f;
            for (int i = 0; i < cCloud.Length; i++)
            {
                float x = Mathf.Repeat(cCloudX[i] - cskyX * .5f - t * 5f, span) - 200f;
                cCloud[i].transform.position = U(x, cyh - Hl * (.34f - i * .05f)); cCloud[i].transform.localScale = Vector3.one * (.9f + .15f * i) * sh;
                cCloud[i].color = Color.Lerp(Color.white, themes[3].cloud, nightFade);
            }
            float pf = Art.HillFarPeriod * sh, pn = Art.HillNearPeriod * sh;
            float offF = Mathf.Repeat(cskyX * .12f * sh, pf), offN = Mathf.Repeat(cskyX * .25f * sh, pn);
            for (int i = 0; i < 2; i++)
            {
                cHillFar[i].transform.position = U(-offF + i * pf, cyh); cHillFar[i].transform.localScale = Vector3.one * sh;
                cHillNear[i].transform.position = U(-offN + i * pn, cyh); cHillNear[i].transform.localScale = Vector3.one * sh;
            }

            DrawRoad();
            cRoad.transform.position = U(0, Hl); cRoad.transform.localScale = Vector3.one;

            // headlight pools on the road at night
            for (int i = 0; i < 2; i++)
            {
                float rz = 14f + i * 14f; var ps = Proj(rz, cpx, 0f, out float kk);
                cLights[i].transform.position = U(ps.x, ps.y); cLights[i].transform.localScale = new Vector3(6f * kk / 100f, 1.8f * kk / 100f, 1);
                cLights[i].color = new Color(1f, .95f, .72f, (.12f - i * .05f) * nightFade);
            }

            // props, creatures and scenery
            foreach (var o in cobjs)
            {
                float rz = o.wz - cz;
                if (rz < 1.5f || rz > ObjFar) { o.sr.enabled = false; continue; }
                float hop = o.t == 'a' && o.hop > 0 ? .5f * Mathf.Abs(Mathf.Sin(o.hop * Mathf.PI * 3)) * o.hop : 0f;
                float wy = o.t == 'b' ? o.y + Mathf.Sin(t * 2f + o.ph) * .15f : hop;
                var ps = Proj(rz, o.x, wy, out float k);
                float s = PxM * k * o.size;
                o.sr.enabled = true; o.sr.transform.position = U(ps.x, ps.y);
                o.sr.transform.localScale = new Vector3(s * (o.flip ? -1f : 1f), s * o.sy, 1f);
                o.sr.transform.rotation = o.t == 'b' ? Quaternion.Euler(0, 0, -(t * 80f + o.ph * 57f)) : Quaternion.identity;
                o.sr.sortingOrder = 100 + Mathf.Clamp(Mathf.RoundToInt(1000f - rz * 5f), 0, 1000);
                o.sr.color = new Color(1, 1, 1, Mathf.Clamp01((ObjFar - rz) / 40f));
            }
            // debris, fire, smoke and stars
            int used = 0;
            foreach (var p in cps)
            {
                float rz = p.wz - cz;
                if (rz < .4f || rz > 150f) continue;
                if (used == cpSrs.Count) cpSrs.Add(MakeSR("Piece", p.sp, 100, cockRoot.transform));
                var r = cpSrs[used++]; r.enabled = true; r.sprite = p.sp;
                var ps = Proj(rz, p.wx, p.wh, out float k);
                float s = PxM * k * p.size * (1f + p.grow * (1f - p.life / p.max));
                r.transform.position = U(ps.x, ps.y); r.transform.localScale = Vector3.one * s;
                r.transform.rotation = Quaternion.Euler(0, 0, -p.rot);
                var c = p.col; float fade = Mathf.Clamp01(p.life / p.max * 2f);
                if (p.sp == SmashArt.FlameBlob) { float q = p.life / p.max; c = q > .66f ? new Color(1f, .82f, .3f) : q > .33f ? new Color(1f, .6f, .15f) : new Color(1f, .3f, .1f); }
                c.a *= fade; r.color = c;
                r.sortingOrder = 101 + Mathf.Clamp(Mathf.RoundToInt(1000f - rz * 5f), 0, 1000);
            }
            for (int i = used; i < cpSrs.Count; i++) cpSrs[i].enabled = false;

            // snow, turbo streaks, tints and flashes
            cFlakes.Begin();
            if (snowFade > .01f)
                for (int i = 0; i < Flakes; i++)
                {
                    flakeY[i] += (45f + flakeS[i] * 70f) * Time.unscaledDeltaTime / Hl; flakeX[i] -= 20f * Time.unscaledDeltaTime / Wl;
                    if (flakeY[i] > 1f) { flakeY[i] -= 1f; flakeX[i] = Random.value; }
                    if (flakeX[i] < 0f) flakeX[i] += 1f;
                    var fr = cFlakes.Next(); fr.transform.position = U(flakeX[i] * Wl, flakeY[i] * Hl * .8f);
                    fr.transform.localScale = Vector3.one * ((1.6f + flakeS[i] * 2.6f) * 2f / 100f); fr.color = new Color(1, 1, 1, .85f * snowFade);
                }
            cFlakes.End();
            for (int i = 0; i < cStreaks.Length; i++)
            {
                var sr = cStreaks[i];
                if (cTurbo < .05f) { sr.color = new Color(1, 1, 1, 0); continue; }
                float a = i * 2.399f, ph = Mathf.Repeat(t * 1.6f + i * .137f, 1f), rad = (30f + ph * ph * Wl * .7f), len = 8f + ph * 90f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .55f);
                sr.transform.position = U(ccx + dir.x * rad, cyh + 10f + dir.y * rad);
                sr.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                sr.transform.localScale = new Vector3(len / 2f, .7f, 1f);
                sr.color = new Color(1, 1, 1, .4f * cTurbo * ph);
            }
            Quad(cVeil, 0, 0, Wl + 2, Hl + 2); cVeil.color = new Color(.04f, .06f, .23f, .38f * nightFade);
            Quad(cGlow, 0, 0, Wl + 2, Hl + 2); cGlow.color = new Color(1f, .45f, .08f, cTurbo * (.1f + .04f * Mathf.Sin(t * 22f)));
            Quad(cFlash, 0, 0, Wl + 2, Hl + 2); cFlash.color = new Color(1f, .93f, .8f, Mathf.Clamp01(cFlashT) * .6f);

            RenderCab();
        }

        void RenderCab()
        {
            float s = CabScale();
            float m = shakeMag * .5f + (turboT > 0 ? 1.2f : 0f) + (Mathf.Abs(cpx) > 3.5f ? 1.5f : 0f);
            Vector2 sk = new Vector2(Mathf.Sin(t * 71f), Mathf.Cos(t * 83f)) * m;
            cabT.position = U(Wl * .5f + sk.x, Hl + sk.y + cBob * .1f); cabT.localScale = Vector3.one * s;
            cabT.rotation = Quaternion.Euler(0, 0, -cpv * .35f);
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);

                        
            // cosmetics chosen in the customise panel
            int suitI = Cos(1), wheelI = Cos(2), hangI = Cos(3), stickI = Cos(4);
            Color wheelTint = WheelTints[wheelI];
            cWheelRing.color = wheelTint; for (int i = 0; i < 3; i++) cSpokes[i].color = wheelTint;
            for (int i = 0; i < 2; i++) cSleeves[i].color = SuitTints[suitI];
            for (int i = 0; i < 2; i++) { cDice[i].enabled = hangI < 3; if (hangI < 3) cDice[i].sprite = CabArt.Hang[hangI]; }
            cMic.enabled = true;
            cSticker.enabled = stickI < 3; if (stickI < 3) cSticker.sprite = CabArt.DashSticker[stickI];

            // hood (tinted per paint colour) with a bounce
            cHood.sprite = CabArt.Hood[ColorIndex];
            cHood.transform.localPosition = new Vector3(0, -cBob / 100f * .8f, 0);
            for (int i = 0; i < 2; i++)
            {
                var f = cHoodFlames[i];
                float fl = cTurbo * (.8f + .35f * Mathf.Sin(t * 31f + i * 2f));
                f.transform.localScale = Vector3.one * fl * (.9f + .2f * Mathf.Sin(t * 17f + i));
                f.color = new Color(1f, .75f + .2f * Mathf.Sin(t * 23f + i), .3f, cTurbo);
            }
            cCracks.color = new Color(1, 1, 1, Mathf.Clamp01(cDamage * 1.4f));

            // steering wheel: the rim stays, the marker, spokes and hands move around it
            float th = csteer * 105f;
            Vector2 P(float a) => new Vector2(124f * Mathf.Sin(a * Mathf.Deg2Rad), 14f - 92f * Mathf.Cos(a * Mathf.Deg2Rad));
            Vector2 mk = P(th); cMark.transform.localPosition = new Vector3(mk.x / 100f, -mk.y / 100f, 0); cMark.transform.localRotation = Quaternion.Euler(0, 0, -th * .85f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 e = P(th + 90f + i * 90f); float dx = e.x, dy = e.y - 14f, len = Mathf.Sqrt(dx * dx + dy * dy);
                cSpokes[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dx, dy) * Mathf.Rad2Deg);
                cSpokes[i].transform.localScale = new Vector3(1f, len / 100f, 1f);
            }
            for (int j = 0; j < 2; j++)
            {
                float baseA = j == 0 ? -55f : 55f, want = baseA + (th - 110f * Mathf.Round(th / 110f));
                cHand[j] = Mathf.MoveTowards(cHand[j], want, 520f * dt);
                Vector2 hp = P(cHand[j]); hp.y += Mathf.Sin(t * 9f + j) * (1f + cspeed * .05f) + cBob * .3f;
                cGloves[j].transform.localPosition = new Vector3(hp.x / 100f, -hp.y / 100f, 0); cGloves[j].transform.localRotation = Quaternion.Euler(0, 0, -cHand[j] * .45f);
                cSleeves[j].transform.localPosition = new Vector3(hp.x / 100f, -(hp.y + 6f) / 100f, 0);
                cSleeves[j].transform.localScale = new Vector3(1f, Mathf.Max(.1f, (10f - hp.y) / 100f), 1f);
            }

            // gauges: speedometer, rev counter (it climbs through the gears), fuel and oil temperature
            int gear = Mathf.FloorToInt(cspeed / 9f); if (gear != cGear) { cGear = gear; cShiftT = .3f; }
            cShiftT = Mathf.Max(0f, cShiftT - dt);
            float rpmT = .22f + .62f * Mathf.Repeat(cspeed, 9f) / 9f + (cShiftT > 0f ? -.15f : 0f);
            cRpm += (rpmT - cRpm) * Mathf.Min(1f, dt * 9f);
            float[] gv = { Mathf.Clamp01(cspeed / 40f) + Mathf.Sin(t * 30f) * .006f, cRpm + Mathf.Sin(t * 37f) * .015f, .72f + Mathf.Sin(t * .7f) * .03f - cz * 0f, .3f + .5f * cHeat + Mathf.Sin(t * 5f) * .01f };
            for (int i = 0; i < 4; i++) cNeedles[i].transform.localRotation = Quaternion.Euler(0, 0, -(135f + 270f * Mathf.Clamp01(gv[i])) - 90f);

            // switches, lights and the nitro button
            bool[] sw = { turboT > 0, cWipeActive, nightFade > .5f, nitroReady };
            for (int i = 0; i < 4; i++)
            {
                float want = sw[i] ? -22f : 22f, cur = cLevers[i].transform.localEulerAngles.z; if (cur > 180f) cur -= 360f;
                cLevers[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowards(cur, want, 300f * dt));
            }
            cLeds[0].color = (turboT > 0 || cDamage > .6f) && Mathf.Sin(t * 14f) > 0 ? new Color(1f, .2f, .15f) : new Color(.35f, .08f, .08f);
            cLeds[1].color = Settings.Volume > 0 ? new Color(.3f, 1f, .45f) : new Color(.1f, .25f, .12f);
            cLeds[2].color = Mathf.Sin(t * 6f + Mathf.Abs(cpv)) > .2f ? new Color(.5f, 1f, .55f) : new Color(.12f, .3f, .15f);
            float ng = nitroReady ? .5f + .5f * Mathf.Sin(t * 12f) : cNitroGlowT;
            cNitroGlow.color = new Color(1f, .45f, .1f, ng * .8f); cNitroGlow.transform.localScale = Vector3.one * (.5f + .25f * ng);
            cNitro.transform.localScale = Vector3.one * (turboT > 0 ? .86f : 1f);
            cNitroFlap.transform.localScale = new Vector3(1f, Mathf.Lerp(cNitroFlap.transform.localScale.y, nitroReady ? .12f : 1f, Mathf.Min(1f, dt * 12f)), 1f);
            int lit = turboT > 0 ? Mathf.CeilToInt(4f * turboT / TurboSeconds) : charge;
            for (int i = 0; i < 4; i++)
            {
                bool on = i < lit;
                cPips[i].color = on ? Color.white : new Color(.45f, .46f, .52f, .55f);
                cPips[i].transform.localScale = Vector3.one * ((on ? .5f + .05f * Mathf.Sin(t * 10f + i) : .42f));
            }
            float lean = Mathf.Clamp((gv[0] - .5f) * 30f, -12f, 14f) + (cShiftT > 0f ? Mathf.Sin(cShiftT * 30f) * 8f : 0f);
            cShift.transform.localRotation = Quaternion.Euler(0, 0, -lean);

            // dice, mic and the wipers
            for (int i = 0; i < 2; i++) cDice[i].transform.localRotation = Quaternion.Euler(0, 0, cDiceA[i]);
            cMic.transform.localRotation = Quaternion.Euler(0, 0, cDiceA[0] * .6f + Mathf.Sin(t * 3f) * 2f);
            float wa = cWipeActive ? Mathf.Sin(cWipePh * Mathf.PI) * 75f : 0f;
            cWipers[0].transform.localRotation = Quaternion.Euler(0, 0, wa);
            cWipers[0].transform.localScale = new Vector3(.72f, 1f, 1f); cWipers[1].transform.localScale = new Vector3(-.72f, 1f, 1f); cWipers[1].transform.localRotation = Quaternion.Euler(0, 0, -wa);
            foreach (var sp in splats)
            {
                if (!sp.on) continue;
                if (cWipeActive) sp.alpha -= dt * 4f;                  // the wipers clear the whole glass in one sweep
                sp.alpha -= dt * .55f;                                 // and dirt dries and falls off on its own within a couple of seconds
                if (sp.alpha <= 0f) { sp.on = false; sp.sr.enabled = false; continue; }
                sp.sr.enabled = true; sp.sr.transform.localPosition = new Vector3(sp.pos.x / 100f, -sp.pos.y / 100f, 0);
                sp.sr.transform.localScale = Vector3.one * sp.size * 1.4f;
                var c = sp.sr.color; c.a = Mathf.Clamp01(sp.alpha) * .7f; sp.sr.color = c;
            }
        }
    }
}
