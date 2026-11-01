using UnityEngine;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>The main menu, shown before the game starts: pick a view, customise, sticker book, settings, quit.</summary>
    public class MenuUi
    {
        readonly Game game; readonly GameObject root; readonly RectTransform content;
        readonly Text bestLbl;
        int lastBest = -1;
        const float W = 380f, H = 660f;

        public MenuUi(Game game, Transform canvasRoot, System.Action settings, System.Action customise, System.Action book)
        {
            this.game = game;
            var bg = Kit.Rt("MainMenu", canvasRoot); Kit.Stretch(bg);
            Kit.Img(bg, null, new Color(.04f, .05f, .09f, .72f));
            root = bg.gameObject;
            content = Kit.Rt("Content", bg); content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f); content.sizeDelta = new Vector2(W, H);

            // title with a hard outline, then a rule and a tagline
            var t1 = Title(content, "BIG", 74, new Vector2(0, -6)); t1.color = Palette.Hex("#ffb02e");
            var t2 = Title(content, "WHEELS", 74, new Vector2(0, -78)); t2.color = Color.white;
            var rule = Kit.Rt("Rule", content); Kit.Place(rule, 60, 168, 260, 5); Kit.Img(rule, null, Palette.Hex("#d9631a"));
            var tag = Kit.Label(content, "SMASH  •  CRUSH  •  RAM", 20, FontStyle.Bold, TextAnchor.UpperCenter, new Color(1, 1, 1, .75f));
            Kit.Place(tag.rectTransform, 0, 182, W, 28);

            Text l;
            Kit.Btn(content, "SIDE VIEW", Palette.Hex("#d9631a"), 0, 228, W, 74, 34, () => game.BeginPlay(false), out l);
            Kit.Btn(content, "COCKPIT VIEW", Palette.Hex("#2a65b5"), 0, 312, W, 74, 34, () => game.BeginPlay(true), out l);
            Kit.Btn(content, "Customize", Palette.Hex("#3a4150"), 0, 402, 184, 56, 24, () => customise(), out l);
            Kit.Btn(content, "Stickers", Palette.Hex("#3a4150"), 196, 402, 184, 56, 24, () => book(), out l);
            Kit.Btn(content, "Settings", Palette.Hex("#3a4150"), 0, 468, DisplayOptions.Show ? 184f : W, 56, 24, () => settings(), out l);
            if (DisplayOptions.Show) Kit.Btn(content, "Quit", Palette.Hex("#6b2a2a"), 196, 468, 184, 56, 24, Application.Quit, out l);

            var best = Kit.Label(content, "", 24, FontStyle.Bold, TextAnchor.UpperCenter, Palette.Hex("#ffd23f"));
            Kit.Place(best.rectTransform, 0, 548, W, 32); bestLbl = best;
            var hint = Kit.Label(content, "Steer: arrows or A / D     Jump: Space\nGas / brake: pedals     Pause: Esc", 17, FontStyle.Normal, TextAnchor.UpperCenter, new Color(1, 1, 1, .6f));
            Kit.Place(hint.rectTransform, 0, 590, W, 56);
        }

        static Text Title(Transform parent, string s, int size, Vector2 pos)
        {
            var t = Kit.Label(parent, s, size, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            Kit.Place(t.rectTransform, pos.x, -pos.y, W, 90);
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.05f, .05f, .1f, 1f); ol.effectDistance = new Vector2(4f, -4f);
            return t;
        }

        public void Refresh(bool show)
        {
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;
            if (Settings.Best != lastBest) { lastBest = Settings.Best; bestLbl.text = lastBest > 0 ? "BEST  " + lastBest.ToString("N0") : ""; }
        }

        public void Fit(float cw, float ch)
        {
            float s = Mathf.Min(1f, (ch - 20f) / H, (cw - 20f) / W);
            content.localScale = Vector3.one * Mathf.Max(.4f, s);
        }
    }

    /// <summary>The pause screen: resume, customise (in the cab), settings, a switch between the two views, and back to the main menu.</summary>
    public class PauseUi
    {
        readonly Game game; readonly GameObject root; readonly RectTransform card, resumeRt, customRt, settingsRt, viewRt, menuRt; readonly Text viewLbl;
        bool open;
        public bool IsOpen => open;

        public PauseUi(Game game, Transform canvasRoot, System.Action settings, System.Action customise)
        {
            this.game = game;
            var dim = Kit.Dim(canvasRoot, "PauseScreen", out card, 400, 520);
            root = dim.gameObject;
            var title = Kit.Label(card, "PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1); title.rectTransform.pivot = new Vector2(.5f, 1);
            title.rectTransform.offsetMin = new Vector2(0, -96); title.rectTransform.offsetMax = new Vector2(0, -22);
            Text l;
            resumeRt = Kit.Btn(card, "Resume", Palette.Hex("#2f8a4a"), 40, 110, 320, 66, 32, Close, out l);
            customRt = Kit.Btn(card, "Customize", Palette.Hex("#2b4f8a"), 40, 192, 320, 62, 28, () => customise(), out l);
            settingsRt = Kit.Btn(card, "Settings", Palette.Hex("#4a4f5c"), 40, 270, 320, 62, 28, () => settings(), out l);
            viewRt = Kit.Btn(card, "", Palette.Hex("#6b4a8a"), 40, 348, 320, 62, 26, () => { game.ToggleView(); Refresh(); }, out viewLbl);
            menuRt = Kit.Btn(card, "Main menu", Palette.Hex("#6b2a2a"), 40, 426, 320, 62, 28, () => { Close(); game.ReturnToMenu(); }, out l);
            root.SetActive(false);
        }

        void Refresh()
        {
            viewLbl.text = game.Cockpit ? "Switch to side view" : "Switch to cockpit view";
            bool cb = game.Cockpit; float y = 192;
            customRt.gameObject.SetActive(cb);
            if (cb) y += 78;
            Kit.Place(settingsRt, 40, y, 320, 62); y += 78;
            Kit.Place(viewRt, 40, y, 320, 62); y += 78;
            Kit.Place(menuRt, 40, y, 320, 62);
            card.sizeDelta = new Vector2(400, y + 62 + 32);
        }

        public void Open()
        {
            if (open) return;
            open = true; game.ReleasePedals(); game.Hold(true); Refresh(); root.SetActive(true);
            Sfx.Play("swoosh", .4f);
        }

        public void Close()
        {
            if (!open) return;
            open = false; game.Hold(false); root.SetActive(false);
        }
    }
}
