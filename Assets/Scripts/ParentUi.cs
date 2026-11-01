using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>
    /// A button that must be HELD for a couple of seconds to count as "parent" input.
    /// A quick tap fires OnTap; holding to the end fires OnHold; a ring fills while holding.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action OnTap, OnHold;
        public Image Progress;
        public float HoldSeconds = 2f;
        bool down, fired; float t;

        public void OnPointerDown(PointerEventData e) { down = true; fired = false; t = 0; }
        public void OnPointerUp(PointerEventData e) { if (down && !fired && t < .6f) OnTap?.Invoke(); Cancel(); }
        public void OnPointerExit(PointerEventData e) { Cancel(); }
        void Cancel() { down = false; t = 0; if (Progress) Progress.fillAmount = 0; }

        void Update()
        {
            if (!down || fired) return;
            t += Time.unscaledDeltaTime;
            if (Progress) Progress.fillAmount = Mathf.Clamp01((t - .3f) / (HoldSeconds - .3f));
            if (t >= HoldSeconds) { fired = true; if (Progress) Progress.fillAmount = 0; OnHold?.Invoke(); }
        }
    }

    /// <summary>
    /// The parent corner: opened by holding the speaker button for 2 seconds.
    /// Volume, engine hum, world lock, play-time reminder and a (two-tap) sticker reset. Also owns the "time for a break" screen.
    /// </summary>
    public class ParentUi
    {
        static readonly int[] BreakChoices = { 0, 10, 20, 30 };
        static readonly string[] WorldNames = { "Auto", "Farm", "Beach", "Snow", "Night" };
        static Font font;
        static Font F => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        readonly Game game;
        readonly GameObject dim, breakGo;
        readonly RectTransform card, zRt, truckRt, moonRt;
        readonly Slider volume;
        readonly Text engineLbl, worldLbl, breakLbl, resetLbl, screenLbl, sizeLbl;
        readonly RectTransform[] starRt = new RectTransform[7];
        float cw, ch, resetArm, resetDone;
        bool open, breakOn;
        public bool IsOpen => open || breakOn;

        public ParentUi(Game game, Transform canvasRoot)
        {
            this.game = game;
            var ink = Palette.Ink;

            // ---- dimmed backdrop + card ----
            var dimRt = Rt("ParentDim", canvasRoot); Stretch(dimRt);
            Img(dimRt, null, new Color(0, 0, 0, .6f));
            dim = dimRt.gameObject;

            card = Rt("ParentCard", dimRt); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.sizeDelta = new Vector2(620, 100);
            var ci = Img(card, Art3.Card, Palette.Hex("#fffaf0")); ci.type = Image.Type.Sliced;
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 20, 20); v.spacing = 9; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var fit = card.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Row(Label(card, "Parent corner", 32, FontStyle.Bold, TextAnchor.MiddleCenter), 44);

            // volume row
            var vr = Rt("VolumeRow", card); Row(vr, 54);
            var h = vr.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14; h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            var vl = Label(vr, "Volume", 26, FontStyle.Normal, TextAnchor.MiddleLeft); var vle = vl.gameObject.AddComponent<LayoutElement>(); vle.preferredWidth = 150;
            volume = MakeSlider(vr);
            volume.value = Settings.Volume;
            volume.onValueChanged.AddListener(x => { Settings.Volume = x; Sfx.ApplyVolume(); });

            MakeButton(card, "", Palette.Hex("#d9edff"), () =>
            {
                Settings.EngineOn = !Settings.EngineOn; Sfx.SetEngineEnabled(Settings.EngineOn && game.Started); Refresh();
            }, out engineLbl);
            MakeButton(card, "", Palette.Hex("#d3f5bd"), () =>
            {
                Settings.WorldLock = Settings.WorldLock >= 3 ? -1 : Settings.WorldLock + 1; game.SetWorldLock(Settings.WorldLock); Refresh();
            }, out worldLbl);
            MakeButton(card, "", Palette.Hex("#ffeab0"), () =>
            {
                int i = System.Array.IndexOf(BreakChoices, Settings.BreakMinutes); Settings.BreakMinutes = BreakChoices[(i + 1) % BreakChoices.Length];
                game.ResetBreakTimer(); Refresh();
            }, out breakLbl);
            if (DisplayOptions.Show)
            {
                MakeButton(card, "", Palette.Hex("#e0e7ff"), () => { DisplayOptions.ToggleFullscreen(); Refresh(); }, out screenLbl);
                MakeButton(card, "", Palette.Hex("#e0e7ff"), () => { DisplayOptions.NextWindowSize(); Refresh(); }, out sizeLbl);
            }
            MakeButton(card, "", Palette.Hex("#ffd6e8"), () =>
            {
                if (resetArm > 0) { Stickers.ResetAll(); game.ResetScores(); resetArm = 0; resetDone = 1.6f; Sfx.Play("splat", .6f); }
                else resetArm = 3f;
                Refresh();
            }, out resetLbl);
            MakeButton(card, "Done", Palette.Hex("#ffffff"), Close, out _);

            dim.SetActive(false);

            // ---- the calm "time for a break" screen ----
            var bk = Rt("BreakScreen", canvasRoot); Stretch(bk);
            Img(bk, null, Palette.Hex("#17224d"));
            breakGo = bk.gameObject;
            for (int i = 0; i < starRt.Length; i++)
            {
                starRt[i] = Rt("Star" + i, bk);
                starRt[i].anchorMin = starRt[i].anchorMax = new Vector2(.1f + .13f * i, .88f - .09f * (i % 3));
                starRt[i].sizeDelta = new Vector2(46, 46);
                Img(starRt[i], Art.Star, Palette.Sun).raycastTarget = false;
            }
            moonRt = Rt("Moon", bk); moonRt.anchorMin = moonRt.anchorMax = new Vector2(.5f, .78f); moonRt.sizeDelta = new Vector2(200, 200);
            Img(moonRt, Art3.Moon, Color.white).raycastTarget = false;
            truckRt = Rt("SleepyTruck", bk); truckRt.anchorMin = truckRt.anchorMax = new Vector2(.5f, .27f); truckRt.sizeDelta = Vector2.zero; truckRt.localScale = Vector3.one * 1.2f;
            var z = Label(bk, "Z z z", 80, FontStyle.Bold, TextAnchor.MiddleCenter); z.color = Palette.Sun; z.raycastTarget = false;
            zRt = z.rectTransform; zRt.anchorMin = zRt.anchorMax = new Vector2(.68f, .62f); zRt.sizeDelta = new Vector2(300, 100);

            var go = Rt("ContinueButton", bk); go.anchorMin = go.anchorMax = new Vector2(.5f, .12f); go.sizeDelta = new Vector2(440, 96);
            var gi = Img(go, Art3.Card, Palette.Hex("#d3f5bd")); gi.type = Image.Type.Sliced;
            var gl = Label(go, "Parent: hold to keep playing", 24, FontStyle.Bold, TextAnchor.MiddleCenter); Stretch(gl.rectTransform);
            var ring = Rt("Progress", go); Stretch(ring);
            var pr = Img(ring, Art3.Card, new Color(1, .85f, .3f, .8f)); pr.type = Image.Type.Filled; pr.fillMethod = Image.FillMethod.Horizontal; pr.fillAmount = 0; pr.raycastTarget = false;
            var hb = go.gameObject.AddComponent<HoldButton>(); hb.Progress = pr; hb.OnHold = EndBreak;
            gl.transform.SetAsLastSibling();
            breakGo.SetActive(false);

            Refresh();
        }

        public void Fit(float cw, float ch)
        {
            this.cw = cw; this.ch = ch;
            card.sizeDelta = new Vector2(Mathf.Min(620f, cw - 32f), card.sizeDelta.y);
        }

        public void Open()
        {
            if (open || breakOn) return;
            open = true; game.Hold(true); dim.SetActive(true); resetArm = 0; resetDone = 0;
            volume.SetValueWithoutNotify(Settings.Volume); Refresh();
            Sfx.Play("sparkle", .6f);
        }

        public void Close()
        {
            if (!open) return;
            open = false; game.Hold(false); dim.SetActive(false);
        }

        public void ShowBreak()
        {
            if (breakOn || open) return;
            breakOn = true; game.Hold(true);
            BuildSleepyTruck();
            breakGo.SetActive(true);
            Sfx.Play("swoosh", .5f);
        }

        public void EndBreak()
        {
            if (!breakOn) return;
            breakOn = false; game.Hold(false); breakGo.SetActive(false); game.ResetBreakTimer();
            Sfx.Play("sparkle", .6f);
        }

        /// <summary>Rebuilds the sleeping truck picture (body, wheels, closed eyes) for the current truck type and color.</summary>
        void BuildSleepyTruck()
        {
            for (int i = truckRt.childCount - 1; i >= 0; i--)
            {
                var go = truckRt.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
            }
            var d = Trucks.All[game.TruckType]; var sp = d.Body[game.ColorIndex];
            var b = Rt("Body", truckRt); b.anchorMin = b.anchorMax = new Vector2(.5f, .5f);
            b.sizeDelta = sp.rect.size / 2f; b.pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height); b.anchoredPosition = Vector2.zero;
            Img(b, sp, Color.white).raycastTarget = false;
            for (int i = 0; i < 2; i++)
            {
                var w = Rt("Wheel", truckRt); w.anchorMin = w.anchorMax = new Vector2(.5f, .5f); w.pivot = new Vector2(.5f, .5f);
                w.sizeDelta = Vector2.one * (100f * d.WheelR / 46f); w.anchoredPosition = new Vector2(d.WheelX[i], d.WheelR);
                Img(w, Art.Wheel, Color.white).raycastTarget = false;
                var e = Rt("SleepEye", truckRt); e.anchorMin = e.anchorMax = new Vector2(.5f, .5f); e.pivot = new Vector2(.5f, .5f);
                e.sizeDelta = new Vector2(44f, 28f) * d.EyeScale; e.anchoredPosition = new Vector2(d.Eyes[i].x, -d.Eyes[i].y);
                Img(e, Art3.SleepEye, Color.white).raycastTarget = false;
            }
        }

        /// <summary>Rewrites the button labels from the saved settings.</summary>
        void Refresh()
        {
            engineLbl.text = "Engine hum: " + (Settings.EngineOn ? "On" : "Off");
            worldLbl.text = "World: " + WorldNames[Settings.WorldLock + 1];
            breakLbl.text = "Break reminder: " + (Settings.BreakMinutes == 0 ? "Off" : Settings.BreakMinutes + " min");
            if (screenLbl != null)
            {
                screenLbl.text = "Screen: " + (Settings.FullScreen ? "Fullscreen" : "Windowed");
                sizeLbl.text = Settings.FullScreen ? "Leave fullscreen" : "Enlarge window: " + DisplayOptions.SizeLabel;
            }
            resetLbl.text = resetDone > 0 ? "Stickers and score reset" : resetArm > 0 ? "Tap again: reset stickers + score" : "Reset stickers and score";
        }

        public void Tick(float time)
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            if (open)
            {
                if (resetArm > 0) { resetArm -= dt; if (resetArm <= 0) Refresh(); }
                if (resetDone > 0) { resetDone -= dt; if (resetDone <= 0) Refresh(); }
                float need = card.rect.height + 24f;
                card.localScale = Vector3.one * (need > ch && ch > 100 ? ch / need : 1f);
            }
            if (breakOn)
            {
                for (int i = 0; i < starRt.Length; i++) starRt[i].localScale = Vector3.one * (.75f + .35f * Mathf.Sin(time * 2f + i * 1.3f));
                moonRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * .8f) * 4f);
                zRt.anchoredPosition = new Vector2(Mathf.Sin(time * 1.6f) * 18f, Mathf.Sin(time * 2.2f) * 10f);
                truckRt.localScale = Vector3.one * (1.2f * (1f + .02f * Mathf.Sin(time * 1.5f)));
            }
        }

        // ---------- UI helpers ----------
        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static Image Img(RectTransform rt, Sprite s, Color c)
        {
            var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; return i;
        }
        static Text Label(Transform parent, string s, int size, FontStyle style, TextAnchor anchor)
        {
            var rt = Rt("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F; t.text = s; t.fontSize = size; t.fontStyle = style; t.alignment = anchor; t.color = Palette.Ink; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
        static void Row(Component c, float height) { var le = c.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = height; }
        static void Row(RectTransform c, float height) { var le = c.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = height; }

        static void MakeButton(Transform parent, string label, Color col, UnityAction click, out Text lbl)
        {
            var rt = Rt("Button", parent); Row(rt, 56);
            var img = Img(rt, Art3.Card, col); img.type = Image.Type.Sliced;
            rt.gameObject.AddComponent<Button>().onClick.AddListener(click);
            lbl = Label(rt, label, 26, FontStyle.Bold, TextAnchor.MiddleCenter); Stretch(lbl.rectTransform);
        }

        static Slider MakeSlider(Transform parent)
        {
            var root = Rt("VolumeSlider", parent);
            var le = root.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1; le.preferredHeight = 54;
            var bg = Rt("Background", root); bg.anchorMin = new Vector2(0, .3f); bg.anchorMax = new Vector2(1, .7f); bg.offsetMin = bg.offsetMax = Vector2.zero;
            Img(bg, Art3.Card, Palette.Hex("#d9dbe8")).type = Image.Type.Sliced;
            var area = Rt("Fill Area", root); area.anchorMin = new Vector2(0, .3f); area.anchorMax = new Vector2(1, .7f); area.offsetMin = new Vector2(8, 0); area.offsetMax = new Vector2(-8, 0);
            var fill = Rt("Fill", area); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            Img(fill, Art3.Card, Palette.Hex("#38c172")).type = Image.Type.Sliced;
            var slide = Rt("Handle Slide Area", root); slide.anchorMin = new Vector2(0, .5f); slide.anchorMax = new Vector2(1, .5f); slide.sizeDelta = new Vector2(-44, 44); slide.anchoredPosition = Vector2.zero;
            var handle = Rt("Handle", slide); handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1); handle.sizeDelta = new Vector2(44, 0);
            var hi = Img(handle, Art.UiRingFilled, Color.white);
            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hi; s.direction = Slider.Direction.LeftToRight; s.minValue = 0; s.maxValue = 1;
            return s;
        }
    }
}
