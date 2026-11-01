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
            int s = 0;

            // long random-ish session, cycling trucks and worlds, to shake out exceptions
            Step(g, 60 * 160, f =>
            {
                if (f % 100 == 20) g.Act();
                if (f % 100 == 55) g.Act();
                if (f % 600 == 0) g.SetTruck((f / 600) % 4);
                if (f % 900 == 0) g.ToggleCostume((f / 900) % 3);
                if (f % 1000 == 0) g.SetColor((f / 1000) % 7);
                if (f == 60 * 76) s = 1;
            });
            Shot(cam, rt, P("40_end"));
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
