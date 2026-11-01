using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>Full-screen tap catcher: any tap that no button swallows goes to the game (sun, animals, jump).</summary>
    public class TapCatcher : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData e) { Game.I?.Tap(e.position); }
    }

    /// <summary>Picture-only UI: 3 counting slots, truck and costume buttons, 7 colour swatches, sound toggle and a big play button.</summary>
    public class Hud
    {
        const int Slots = Game.SlotCount, Trucks4 = 4, Costumes = 3;
        readonly RectTransform safe, playRt, canvasRt, rowRt, dockRt, sndRt, sideRt;
        readonly RectTransform[] slotRt = new RectTransform[Slots], carRt = new RectTransform[Slots], swatchRt = new RectTransform[7];
        readonly RectTransform[] truckRt = new RectTransform[Trucks4], costumeRt = new RectTransform[Costumes], spacerRt = new RectTransform[1];
        readonly Image[] slotBg = new Image[Slots], slotCar = new Image[Slots], swatch = new Image[7];
        readonly Image[] truckBg = new Image[Trucks4], truckIcon = new Image[Trucks4], costumeBg = new Image[Costumes];
        readonly Image sndIcon;
        readonly RectTransform gearRt, scoreRt;
        readonly Text scoreVal, comboTxt, bestTxt;
        class Pop { public Text t; public RectTransform rt; public float born = -99; }
        readonly Pop[] pops = new Pop[14];
        int nextPop, lastScore = -1, lastBest = -1, lastMult = -1;
        static Font font;
        static Font F => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        readonly HorizontalLayoutGroup rowLayout, dockLayout;
        readonly VerticalLayoutGroup sideLayout;
        readonly GameObject play;
        bool needLayout = true;
        StickerUi sticker;
        ParentUi parentUi;
        int lastColor = -1;
        Vector2 lastScreen;

        public Hud(Game game, Camera cam)
        {
            var cgo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRt = cgo.GetComponent<RectTransform>();
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f; canvas.sortingOrder = 100;
            var cs = cgo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1000, 700); cs.matchWidthOrHeight = .5f;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var tap = Rt("TapCatcher", cgo.transform); Stretch(tap);
            Img(tap, null, new Color(0, 0, 0, 0));
            tap.gameObject.AddComponent<TapCatcher>();

            safe = Rt("SafeArea", cgo.transform); Stretch(safe);

            // counting slots (top centre)
            var row = rowRt = Rt("Slots", safe); Anchor(row, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(Slots * 84 + (Slots - 1) * 14, 84));
            rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14; rowLayout.childAlignment = TextAnchor.MiddleCenter; rowLayout.childControlWidth = false; rowLayout.childControlHeight = false;
            for (int i = 0; i < Slots; i++)
            {
                slotRt[i] = Rt("Slot" + i, row); slotRt[i].sizeDelta = new Vector2(84, 84);
                slotBg[i] = Img(slotRt[i], Art.UiRing, Color.white); slotBg[i].raycastTarget = false;
                var car = carRt[i] = Rt("Car", slotRt[i]); Anchor(car, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(66, 44));
                slotCar[i] = Img(car, null, Color.white); slotCar[i].preserveAspect = true; slotCar[i].raycastTarget = false; slotCar[i].enabled = false;
            }

            // colour swatches (bottom centre)
            var dock = dockRt = Rt("Dock", safe); Anchor(dock, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(7 * 96 + 6 * 12, 120));
            dockLayout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            dockLayout.spacing = 12; dockLayout.childAlignment = TextAnchor.MiddleCenter; dockLayout.childControlWidth = false; dockLayout.childControlHeight = false;
            for (int i = 0; i < 7; i++)
            {
                int idx = i;
                swatchRt[i] = Rt("Swatch" + i, dock); swatchRt[i].sizeDelta = new Vector2(96, 96);
                swatch[i] = Img(swatchRt[i], Art.SwatchNormal[i], Color.white);
                swatchRt[i].gameObject.AddComponent<Button>().onClick.AddListener(() => game.SetColor(idx));
            }

            // sound toggle (top right)
            var snd = sndRt = Rt("Sound", safe); Anchor(snd, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(96, 96));
            Img(snd, Art.UiRingFilled, Color.white);
            var hold = snd.gameObject.AddComponent<HoldButton>();
            hold.OnTap = () => sndIcon.sprite = game.ToggleMute() ? Art.SpeakerOff : Art.SpeakerOn;
            hold.OnHold = () => parentUi.Open();
            var hr = Rt("HoldRing", snd); Stretch(hr);
            var hri = Img(hr, Art2.UiRingSel, new Color(1f, .78f, .2f, .95f)); hri.type = Image.Type.Filled; hri.fillMethod = Image.FillMethod.Radial360; hri.fillAmount = 0; hri.raycastTarget = false;
            hold.Progress = hri;
            var ic = Rt("Icon", snd); Anchor(ic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
            sndIcon = Img(ic, Art.SpeakerOn, Color.white); sndIcon.raycastTarget = false;

            // truck types + costumes (right edge, under the sound button)
            sideRt = Rt("Side", safe); Anchor(sideRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -130), new Vector2(64, 520));
            sideLayout = sideRt.gameObject.AddComponent<VerticalLayoutGroup>();
            sideLayout.spacing = 8; sideLayout.childAlignment = TextAnchor.UpperRight; sideLayout.childControlWidth = false; sideLayout.childControlHeight = false;
            for (int i = 0; i < Trucks4; i++)
            {
                int idx = i;
                truckRt[i] = Rt("Truck" + i, sideRt); truckRt[i].sizeDelta = new Vector2(64, 64);
                truckBg[i] = Img(truckRt[i], Art2.UiRingSel, Color.white);
                truckRt[i].gameObject.AddComponent<Button>().onClick.AddListener(() => game.SetTruck(idx));
                var icon = Rt("Icon", truckRt[i]); Anchor(icon, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(50, 36));
                truckIcon[i] = Img(icon, Trucks.All[i].Body[game.ColorIndex], Color.white); truckIcon[i].preserveAspect = true; truckIcon[i].raycastTarget = false;
            }
            spacerRt[0] = Rt("Spacer", sideRt); spacerRt[0].sizeDelta = new Vector2(64, 10);
            var costumeIcons = new[] { Art2.Hat, Art2.IconEyes, Art2.IconFlag };
            for (int i = 0; i < Costumes; i++)
            {
                int idx = i;
                costumeRt[i] = Rt("Costume" + i, sideRt); costumeRt[i].sizeDelta = new Vector2(64, 64);
                costumeBg[i] = Img(costumeRt[i], Art.UiRingFilled, Color.white);
                costumeRt[i].gameObject.AddComponent<Button>().onClick.AddListener(() => game.ToggleCostume(idx));
                var icon = Rt("Icon", costumeRt[i]); Anchor(icon, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 40));
                var im = Img(icon, costumeIcons[i], Color.white); im.preserveAspect = true; im.raycastTarget = false;
            }

            // big play button (centre)
            playRt = Rt("Play", safe); Anchor(playRt, new Vector2(.5f, .55f), new Vector2(.5f, .55f), Vector2.zero, new Vector2(300, 300));
            Img(playRt, Art.UiRingFilled, Color.white);
            playRt.gameObject.AddComponent<Button>().onClick.AddListener(game.StartGame);
            var tri = Rt("Triangle", playRt); Anchor(tri, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(8, 0), new Vector2(170, 170));
            Img(tri, Art.PlayTriangle, Color.white).raycastTarget = false;
            play = playRt.gameObject;

            // settings gear for the parent, and the score panel, stacked under the sticker book on the left
            gearRt = Rt("Gear", safe); Anchor(gearRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -112), new Vector2(76, 76));
            Img(gearRt, Art.UiRingFilled, Color.white);
            gearRt.gameObject.AddComponent<Button>().onClick.AddListener(() => parentUi.Open());
            var gicon = Rt("Icon", gearRt); Anchor(gicon, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(48, 48));
            Img(gicon, Art3.IconGear, Color.white).raycastTarget = false;

            scoreRt = Rt("Score", safe); Anchor(scoreRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -200), new Vector2(184, 112));
            var sbg = Img(scoreRt, Art3.Card, new Color(.07f, .1f, .22f, .74f)); sbg.type = Image.Type.Sliced; sbg.raycastTarget = false;
            var TL = new Vector2(0, 1); var TR = new Vector2(1, 1);
            AddText(scoreRt, "SCORE", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1, 1, 1, .7f), TL, new Vector2(14, -9), new Vector2(100, 20));
            scoreVal = AddText(scoreRt, "0", 40, FontStyle.Bold, TextAnchor.UpperLeft, Color.white, TL, new Vector2(14, -26), new Vector2(170, 50));
            comboTxt = AddText(scoreRt, "", 28, FontStyle.Bold, TextAnchor.UpperRight, Palette.Hex("#ffb13b"), TR, new Vector2(-12, -6), new Vector2(80, 32));
            bestTxt = AddText(scoreRt, "BEST 0", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1, 1, 1, .7f), TL, new Vector2(14, -84), new Vector2(166, 22));
            scoreRt.gameObject.SetActive(false);

            // floating "+points" popups
            for (int i = 0; i < pops.Length; i++)
            {
                var pt = AddText(cgo.transform, "", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(.5f, .5f), Vector2.zero, new Vector2(320, 64));
                var ol = pt.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.05f, .05f, .15f, .9f); ol.effectDistance = new Vector2(2.5f, -2.5f);
                pt.enabled = false;
                pops[i] = new Pop { t = pt, rt = pt.rectTransform };
            }

            sticker = new StickerUi(game, cgo.transform, safe);
            parentUi = new ParentUi(game, cgo.transform);
        }

        public void Refresh(Game g)
        {
            // keep the UI inside the device safe area (notches, rounded corners)
            Vector2 sc = new Vector2(Screen.width, Screen.height);
            if (sc != lastScreen || Time.frameCount % 120 == 0)
            {
                lastScreen = sc; needLayout = true;
                var r = Screen.safeArea;
                if (g.ScreenOverride != Vector2.zero || r.width <= 0) r = new Rect(0, 0, sc.x, sc.y);
                safe.anchorMin = new Vector2(r.xMin / sc.x, r.yMin / sc.y); safe.anchorMax = new Vector2(r.xMax / sc.x, r.yMax / sc.y);
                safe.offsetMin = safe.offsetMax = Vector2.zero;
            }
            if (needLayout) FitLayout();
            sticker.Refresh(g.Clock);
            parentUi.Tick(g.Clock);
            play.SetActive(!g.Started);
            if (!g.Started) playRt.localScale = Vector3.one * (1f + .08f * Mathf.Sin(g.Clock * 5f));

            scoreRt.gameObject.SetActive(g.Started);
            if (g.Score != lastScore) { lastScore = g.Score; scoreVal.text = g.Score.ToString("N0"); }
            if (g.Best != lastBest) { lastBest = g.Best; bestTxt.text = "BEST " + g.Best.ToString("N0"); }
            int mult = g.Mult; if (mult != lastMult) { lastMult = mult; comboTxt.text = mult > 1 ? "x" + mult : ""; comboTxt.color = g.Turbo ? Palette.Hex("#ff6a2a") : Palette.Hex("#ffb13b"); }
            scoreVal.rectTransform.localScale = Vector3.one * (1f + .2f * g.ScorePulse);
            foreach (var p in pops)
            {
                float age = g.Clock - p.born;
                bool on = age >= 0 && age < 1.1f;
                if (p.t.enabled != on) p.t.enabled = on;
                if (!on) continue;
                p.rt.anchoredPosition = new Vector2(0, 40f + 90f * age);
                p.rt.localScale = Vector3.one * (1f + .4f * Mathf.Clamp01(1f - age * 5f));
                var c = p.t.color; c.a = 1f - Mathf.Clamp01((age - .5f) / .6f); p.t.color = c;
            }

            for (int i = 0; i < Slots; i++)
            {
                bool filled = i < g.Slots;
                slotBg[i].sprite = filled ? Art.UiRingFilled : Art.UiRing;
                slotCar[i].enabled = filled;
                if (filled) slotCar[i].sprite = Art.Car[g.SlotCols[i]];
                float pop = g.SlotPop[i];
                float sc2 = 1f + .35f * Mathf.Sin(pop * Mathf.PI) + (g.Celeb > 0 ? .08f * Mathf.Sin(g.Clock * 14f + i) : 0f);
                slotRt[i].localScale = Vector3.one * sc2;
            }
            for (int i = 0; i < Trucks4; i++) truckBg[i].sprite = i == g.TruckType ? Art2.UiRingSel : Art.UiRingFilled;
            for (int i = 0; i < Costumes; i++) costumeBg[i].sprite = g.Costume[i] ? Art2.UiRingSel : Art.UiRingFilled;
            if (lastColor != g.ColorIndex)
            {
                lastColor = g.ColorIndex;
                for (int i = 0; i < 7; i++)
                {
                    bool sel = i == lastColor;
                    swatch[i].sprite = sel ? Art.SwatchSelected[i] : Art.SwatchNormal[i];
                    swatchRt[i].localScale = Vector3.one * (sel ? 1.2f : 1f);
                }
                for (int i = 0; i < Trucks4; i++) truckIcon[i].sprite = Trucks.All[i].Body[lastColor];
            }
        }

        /// <summary>Floats "+points" up from a spot on the screen (fx, fy = 0..1 from the bottom left). A bigger multiplier makes it bigger and warmer.</summary>
        public void Popup(string s, float fx, float fy, int mult)
        {
            var p = pops[nextPop++ % pops.Length];
            p.born = Game.I != null ? Game.I.Clock : 0f;
            int slot = nextPop % 4;                       // popups from one blast fan out instead of landing on each other
            p.rt.anchorMin = p.rt.anchorMax = new Vector2(Mathf.Clamp(fx + (slot - 1.5f) * .07f, .1f, .9f), Mathf.Clamp(fy + slot * .055f, .12f, .86f));
            p.t.text = s; p.t.fontSize = 26 + Mathf.Min(mult, 6) * 2;
            p.t.color = mult >= 4 ? Palette.Hex("#ff8a2a") : mult >= 2 ? Palette.Hex("#ffd23f") : Color.white;
        }

        static Text AddText(Transform parent, string s, int size, FontStyle style, TextAnchor al, Color col, Vector2 anchor, Vector2 pos, Vector2 sz)
        {
            var rt = Rt("Text", parent);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = sz;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F; t.text = s; t.fontSize = size; t.fontStyle = style; t.alignment = al; t.color = col; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public void Reward(int id) => sticker.Show(id);
        public void OpenParent() => parentUi.Open();
        public void CloseParent() => parentUi.Close();
        public void ShowBreak() => parentUi.ShowBreak();
        public void HideBreak() => parentUi.EndBreak();
        public void OpenBook() => sticker.Open();
        public void CloseBook() => sticker.Close();
        public bool BookOpen => sticker.IsOpen;

        /// <summary>Scale the picture buttons so they fit narrow phones and short landscape screens.</summary>
        void FitLayout()
        {
            float cw = canvasRt.rect.width * (safe.anchorMax.x - safe.anchorMin.x);
            float ch = canvasRt.rect.height * (safe.anchorMax.y - safe.anchorMin.y);
            if (cw < 100f || ch < 100f) return;
            needLayout = false;
            float sw = Mathf.Min(76f, (cw - 32f - 6 * 10f) / 7f);
            dockLayout.spacing = 10; dockRt.sizeDelta = new Vector2(7 * sw + 6 * 10, sw * 1.3f);
            dockRt.anchoredPosition = new Vector2(0, 22);
            for (int i = 0; i < 7; i++) swatchRt[i].sizeDelta = new Vector2(sw, sw);

            float sndS = Mathf.Min(84f, sw * 1.1f);
            sndRt.sizeDelta = new Vector2(sndS, sndS); sndRt.anchoredPosition = new Vector2(-18, -18);
            sndIcon.rectTransform.sizeDelta = new Vector2(sndS * .62f, sndS * .62f);
            sticker.Fit(cw, ch, sndS);
            parentUi.Fit(cw, ch);
            gearRt.sizeDelta = new Vector2(sndS, sndS); gearRt.anchoredPosition = new Vector2(18, -(18 + sndS + 10));
            ((RectTransform)gearRt.GetChild(0)).sizeDelta = new Vector2(sndS * .62f, sndS * .62f);

            float top = 18 + sndS + 10;
            float bs = Mathf.Clamp((ch - top - 24 - 7 * 8 - 10) / 7f, 36f, 64f);
            sideRt.anchoredPosition = new Vector2(-18, -top);
            sideRt.sizeDelta = new Vector2(bs, 7 * bs + 7 * 8 + 10);
            for (int i = 0; i < Trucks4; i++)
            {
                truckRt[i].sizeDelta = new Vector2(bs, bs);
                truckIcon[i].rectTransform.sizeDelta = new Vector2(bs * .8f, bs * .56f);
            }
            for (int i = 0; i < Costumes; i++) { costumeRt[i].sizeDelta = new Vector2(bs, bs); costumeBg[i].transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(bs * .62f, bs * .62f); }
            spacerRt[0].sizeDelta = new Vector2(bs, 10);

            float ss = Mathf.Clamp((cw - 2 * (sndS + 30) - (Slots - 1) * 12) / Slots, 30f, 76f);
            rowLayout.spacing = 12; rowRt.sizeDelta = new Vector2(Slots * ss + (Slots - 1) * 12, ss); rowRt.anchoredPosition = new Vector2(0, -18);
            for (int i = 0; i < Slots; i++) { slotRt[i].sizeDelta = new Vector2(ss, ss); carRt[i].sizeDelta = new Vector2(ss * .78f, ss * .52f); }

            // score panel: beside the gear in the top row when there is room before the counting slots, otherwise under the gear
            float left = 18 + sndS + 10, avail = cw / 2 - rowRt.sizeDelta.x / 2 - left - 10;
            float k = Mathf.Clamp(sndS / 84f, .7f, 1f);
            if (avail >= 120f) { scoreRt.anchoredPosition = new Vector2(left, -18); scoreRt.localScale = Vector3.one * Mathf.Min(k, avail / 184f); }
            else { scoreRt.anchoredPosition = new Vector2(18, -(18 + 2 * sndS + 20)); scoreRt.localScale = Vector3.one * k; }
        }

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            r.anchorMin = min; r.anchorMax = max;
            r.pivot = new Vector2(min.x == max.x ? min.x : .5f, min.y == max.y ? (min.y > .9f ? 1f : min.y < .1f ? 0f : .5f) : .5f);
            r.anchoredPosition = pos; r.sizeDelta = size;
        }
        static Image Img(RectTransform rt, Sprite s, Color c)
        {
            var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; return i;
        }
    }
}
