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

            Shot(cam, rt, Path.Combine(dir, tag + "_0_attract.png"));
            g.StartGame();
            int shot = 1;
            int flips = 0;
            for (int f = 0; f < 60 * 40; f++)
            {
                if (f % 90 == 45) g.Act();
                if (f % 90 == 70 && ++flips % 2 == 0) g.Act();
                g.Tick(1f / 60f);
                if (f == 150 || f == 600 || f == 1200 || f == 2000) Shot(cam, rt, Path.Combine(dir, $"{tag}_{shot++}.png"));
            }
            Debug.Log($"[BigWheels] {tag}: slots={g.Slots} objs={g.ObjectCount} worldX={g.WorldX:F0} h={g.TruckHeight:F1}");
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
