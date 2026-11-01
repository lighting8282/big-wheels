using System.IO;
using UnityEditor;
using UnityEngine;

namespace BigWheels.EditorTools
{
    /// <summary>One-time project setup plus a headless smoke test that plays the game and saves screenshots.</summary>
    public static class BigWheelsEditor
    {
        const string MatPath = "Assets/Resources/SpriteUnlit.mat";

        [MenuItem("Big Wheels/Setup Project")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Resources/Voice");
            if (AssetDatabase.LoadAssetAtPath<Material>(MatPath) == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                AssetDatabase.CreateAsset(new Material(shader), MatPath);
            }
            PlayerSettings.productName = "Big Wheels";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[BigWheels] setup done");
        }

        /// <summary>Run with: unity run &lt;project&gt; -- -executeMethod BigWheels.EditorTools.BigWheelsEditor.Smoke -shotDir &lt;dir&gt;</summary>
        public static void Smoke()
        {
            Setup();
            string dir = "Screenshots";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotDir") dir = args[i + 1];
            Directory.CreateDirectory(dir);
            try
            {
                Run(1280, 720, dir, "landscape");
                Run(390, 844, dir, "portrait");
            }
            catch (System.Exception e) { Debug.LogError("[BigWheels] smoke failed: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        static void Step(Game g, int frames, System.Action<int> each = null)
        {
            for (int f = 0; f < frames; f++) { each?.Invoke(f); g.Tick(1f / 60f); }
        }

        static void Run(int w, int h, string dir, string tag)
        {
            Random.InitState(7);
            Stickers.ResetAll(); Settings.ResetAll();
            Sfx.Muted = true;
            var go = new GameObject("Big Wheels");
            var g = go.AddComponent<Game>();
            g.ScreenOverride = new Vector2(w, h);
            g.Boot();
            var cam = Camera.main;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 1 };
            cam.targetTexture = rt;
            string P(string n) => Path.Combine(dir, tag + "_" + n + ".png");

            Shot(cam, rt, P("00_attract"));
            g.StartGame();
            Step(g, 22);
            Shot(cam, rt, P("01_first_sticker"));
            Step(g, 60);

            // every truck type, with all three costumes on, crushing a car and popping balloons
            for (int t = 0; t < 4; t++)
            {
                g.SetTruck(t);
                for (int i = 0; i < 3; i++) if (!g.Costume[i]) g.ToggleCostume(i);
                g.SetColor((t * 2 + 1) % 7);
                g.DebugSpawn('c', 330); g.DebugSpawn('b', 520);
                int shotAt = t == 2 ? 70 : 105;
                Step(g, shotAt + 1, f => { if (f == 55) g.Act(); if (f == 75 && t == 2) g.Act(); });
                Shot(cam, rt, P("1" + t + "_truck_" + Trucks.All[t].Name));
                Step(g, 160);
            }
            for (int i = 0; i < 3; i++) if (g.Costume[i]) g.ToggleCostume(i);
            g.SetTruck(0); g.SetColor(0);

            // beach world with its animals
            g.SetWorld(1); g.DebugAnimal(3, 260); g.DebugAnimal(4, 480);
            Step(g, 70);
            Shot(cam, rt, P("20_beach"));
            g.Tap(new Vector2(w * .5f, h * .5f));      // tap the middle of the screen (jump)
            Step(g, 60);
            Shot(cam, rt, P("21_beach_jump"));

            // snow world with its animals and falling snow
            g.SetWorld(2); g.DebugAnimal(5, 260); g.DebugAnimal(6, 470); g.DebugAnimal(7, 700);
            Step(g, 110);
            Shot(cam, rt, P("22_snow"));
            g.Tap(new Vector2(w * .5f, h * .5f));
            Step(g, 70);
            Shot(cam, rt, P("23_snow_jump"));

            // night world: stars, moon, fireflies, headlight beam, night animals
            g.SetWorld(3); g.DebugAnimal(8, 260); g.DebugAnimal(9, 470); g.DebugAnimal(10, 700);
            Step(g, 110);
            Shot(cam, rt, P("24_night"));
            g.Tap(new Vector2(w * .5f, h * .5f));
            Step(g, 70);
            Shot(cam, rt, P("25_night_jump"));

            // farm world: animals, then each new obstacle on its own
            g.SetWorld(0); g.DebugAnimal(0, 250); g.DebugAnimal(1, 520); g.DebugAnimal(2, 760);
            Step(g, 200);
            g.DebugSpawn('t', 330);
            Step(g, 112); Shot(cam, rt, P("30_trampoline"));
            Step(g, 200);
            g.DebugSpawn('j', 330);
            Step(g, 88); Shot(cam, rt, P("31_jelly"));
            Step(g, 200);
            g.DebugSpawn('s', 330);
            Step(g, 100); Shot(cam, rt, P("32_stream"));
            Step(g, 200);

            // smashable props: crates, a brick wall, bowling pins, then the turbo boost
            g.DebugSpawn('k', 330);
            Step(g, 66); Shot(cam, rt, P("33_crates"));
            Step(g, 200);
            g.DebugSpawn('m', 330);
            Step(g, 64); Shot(cam, rt, P("34_wall"));
            Step(g, 200);
            g.DebugSpawn('n', 330);
            Step(g, 64); Shot(cam, rt, P("35_pins"));
            Step(g, 200);
            g.DebugTurbo(); g.DebugSpawn('m', 420); g.DebugSpawn('k', 800);
            Step(g, 50); Shot(cam, rt, P("36_turbo"));
            Step(g, 40); Shot(cam, rt, P("37_turbo_smash"));
            Debug.Log($"[BigWheels] {tag}: turbo={g.Turbo} speedMul={g.SpeedMul:F2} (expect True ~1.5)");
            Step(g, 60 * 7);
            Debug.Log($"[BigWheels] {tag}: after turbo={g.Turbo} speedMul={g.SpeedMul:F2} charge={g.Charge} (expect False ~1.00)");
            Step(g, 200);
            int s = 0;

            // parent corner, break screen, and a locked world
            g.OpenParent(); Step(g, 8);
            Shot(cam, rt, P("60_parent"));
            g.CloseParent(); Step(g, 5);
            g.DebugBreak(); Step(g, 8);
            Shot(cam, rt, P("61_break"));
            g.DebugEndBreak(); Step(g, 5);
            Settings.BreakMinutes = 20; Settings.WorldLock = 1; Settings.Volume = .4f;
            Settings.ForgetCache();
            Debug.Log($"[BigWheels] {tag}: settings reload break={Settings.BreakMinutes} lock={Settings.WorldLock} vol={Settings.Volume:F1} (expect 20 1 0.4)");
            Settings.BreakMinutes = 0;
            g.SetWorldLock(1);
            int lockedWorld = -1;
            Step(g, 60 * 80, f => { if (f == 60 * 79) lockedWorld = g.World; });
            Debug.Log($"[BigWheels] {tag}: world after 80s with lock = {lockedWorld} (expect 1)");
            Settings.WorldLock = -1; Settings.Volume = 1f;

            // long random-ish session, cycling trucks and worlds, to shake out exceptions
            Step(g, 60 * 240, f =>
            {
                if (f % 100 == 20) g.Act();
                if (f % 100 == 55) g.Act();
                if (f % 600 == 0) g.SetTruck((f / 600) % 4);
                if (f % 900 == 0) g.ToggleCostume((f / 900) % 3);
                if (f % 1000 == 0) g.SetColor((f / 1000) % 7);
                if (f == 60 * 76) s = 1;
            });
            Shot(cam, rt, P("40_end"));
            g.OpenBook(); Step(g, 6);
            Shot(cam, rt, P("50_book"));
            int before = Stickers.Collected; Stickers.ForgetCache(); int after = Stickers.Collected;
            g.CloseBook(); Step(g, 5);
            Debug.Log($"[BigWheels] {tag}: stickers collected={before} after reload={after} (must match)");
            Stickers.ResetAll();
            Debug.Log($"[BigWheels] {tag}: slots={g.Slots} objs={g.ObjectCount} animals={g.AnimalCount} world={g.World} worldX={g.WorldX:F0}");
            cam.targetTexture = null; rt.Release();
            Object.DestroyImmediate(go);
            foreach (var c in Object.FindObjectsByType<Canvas>()) Object.DestroyImmediate(c.gameObject);
            foreach (var r in Object.FindObjectsByType<Transform>()) if (r != null && r.name == "World") Object.DestroyImmediate(r.gameObject);
            typeof(Game).GetProperty("I").SetValue(null, null);
        }

        static void Shot(Camera cam, RenderTexture rt, string path)
        {
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
