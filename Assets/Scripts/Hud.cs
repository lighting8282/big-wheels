using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>Full-screen tap catcher: any tap that no button swallows makes the truck jump.</summary>
    public class TapCatcher : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData e) { Game.I?.Act(); }
    }

    /// <summary>Picture-only UI: 5 counting slots, 7 colour swatches, sound toggle and a big play button.</summary>
    public class Hud
    {
        readonly RectTransform safe, playRt;
        readonly Image[] slotBg = new Image[5], slotCar = new Image[5], swatch = new Image[7];
        readonly RectTransform[] slotRt = new RectTransform[5], swatchRt = new RectTransform[7];
        readonly Image sndIcon;
        readonly GameObject play;
        readonly RectTransform canvasRt, rowRt, dockRt, sndRt;
        readonly RectTransform[] carRt = new RectTransform[5];
        readonly HorizontalLayoutGroup rowLayout, dockLayout;
        bool needLayout = true;
        int lastColor = -1;
        Vector2 lastScreen;

        public Hud(Game game, Camera cam)
        {
            var cgo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRt = cgo.GetComponent<RectTransform>();
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f; canvas.sortingOrder = 100;
            var cs = cgo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1000, 700); cs.matchWidthOrHeight = 0f;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var tap = Rt("TapCatcher", cgo.transform); Stretch(tap);
            var tapImg = tap.gameObject.AddComponent<Image>(); tapImg.color = new Color(0, 0, 0, 0);
            tap.gameObject.AddComponent<TapCatcher>();

            safe = Rt("SafeArea", cgo.transform); Stretch(safe);

            // counting slots (top centre)
            var row = Rt("Slots", safe); Anchor(row, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(5 * 84 + 4 * 14, 84));
            rowRt = row; var hl = rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 14; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = false; hl.childControlHeight = false;
            for (int i = 0; i < 5; i++)
            {
                slotRt[i] = Rt("Slot" + i, row); slotRt[i].sizeDelta = new Vector2(84, 84);
                slotBg[i] = Img(slotRt[i], Art.UiRing, Color.white); slotBg[i].raycastTarget = false;
                var car = Rt("Car", slotRt[i]); Anchor(car, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(66, 44));
                carRt[i] = car; slotCar[i] = Img(car, null, Color.white); slotCar[i].preserveAspect = true; slotCar[i].raycastTarget = false; slotCar[i].enabled = false;
            }

            // colour swatches (bottom centre)
            var dock = Rt("Dock", safe); Anchor(dock, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(7 * 96 + 6 * 12, 120));
            dockRt = dock; var dl = dockLayout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.spacing = 12; dl.childAlignment = TextAnchor.MiddleCenter; dl.childControlWidth = false; dl.childControlHeight = false;
            for (int i = 0; i < 7; i++)
            {
                int idx = i;
                swatchRt[i] = Rt("Swatch" + i, dock); swatchRt[i].sizeDelta = new Vector2(96, 96);
                swatch[i] = Img(swatchRt[i], Art.SwatchNormal[i], Color.white);
                swatchRt[i].gameObject.AddComponent<Button>().onClick.AddListener(() => game.SetColor(idx));
            }

            // sound toggle (top right)
            var snd = Rt("Sound", safe); Anchor(snd, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(96, 96)); snd.pivot = new Vector2(1, 1); sndRt = snd;
            Img(snd, Art.UiRingFilled, Color.white);
            snd.gameObject.AddComponent<Button>().onClick.AddListener(() => sndIcon.sprite = game.ToggleMute() ? Art.SpeakerOff : Art.SpeakerOn);
            var ic = Rt("Icon", snd); Anchor(ic, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
            sndIcon = Img(ic, Art.SpeakerOn, Color.white); sndIcon.raycastTarget = false;

            // big play button (centre)
            playRt = Rt("Play", safe); Anchor(playRt, new Vector2(.5f, .55f), new Vector2(.5f, .55f), Vector2.zero, new Vector2(300, 300));
            Img(playRt, Art.UiRingFilled, Color.white);
            playRt.gameObject.AddComponent<Button>().onClick.AddListener(game.StartGame);
            var tri = Rt("Triangle", playRt); Anchor(tri, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(8, 0), new Vector2(170, 170));
            Img(tri, Art.PlayTriangle, Color.white).raycastTarget = false;
            play = playRt.gameObject;
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
            play.SetActive(!g.Started);
            if (!g.Started) playRt.localScale = Vector3.one * (1f + .08f * Mathf.Sin(g.Clock * 5f));

            for (int i = 0; i < 5; i++)
            {
                bool filled = i < g.Slots;
                slotBg[i].sprite = filled ? Art.UiRingFilled : Art.UiRing;
                slotCar[i].enabled = filled;
                if (filled) slotCar[i].sprite = Art.Car[g.SlotCols[i]];
                float pop = g.SlotPop[i];
                float sc2 = 1f + .35f * Mathf.Sin(pop * Mathf.PI) + (g.Celeb > 0 ? .08f * Mathf.Sin(g.Clock * 14f + i) : 0f);
                slotRt[i].localScale = Vector3.one * sc2;
            }
            if (lastColor != g.ColorIndex)
            {
                lastColor = g.ColorIndex;
                for (int i = 0; i < 7; i++)
                {
                    bool sel = i == lastColor;
                    swatch[i].sprite = sel ? Art.SwatchSelected[i] : Art.SwatchNormal[i];
                    swatchRt[i].localScale = Vector3.one * (sel ? 1.2f : 1f);
                    swatchRt[i].anchoredPosition = new Vector2(swatchRt[i].anchoredPosition.x, 0);
                }
            }
        }

        /// <summary>Scale the picture buttons so they fit narrow phones and stay modest on wide screens.</summary>
        void FitLayout()
        {
            float cw = canvasRt.rect.width * (safe.anchorMax.x - safe.anchorMin.x);
            if (cw < 100f) return;
            needLayout = false;
            float sw = Mathf.Min(76f, (cw - 32f - 6 * 10f) / 7f);
            dockLayout.spacing = 10; dockRt.sizeDelta = new Vector2(7 * sw + 6 * 10, sw * 1.3f);
            dockRt.anchoredPosition = new Vector2(0, 22);
            for (int i = 0; i < 7; i++) swatchRt[i].sizeDelta = new Vector2(sw, sw);
            float sndS = Mathf.Min(84f, sw * 1.1f);
            sndRt.sizeDelta = new Vector2(sndS, sndS); sndRt.anchoredPosition = new Vector2(-18, -18);
            sndIcon.rectTransform.sizeDelta = new Vector2(sndS * .62f, sndS * .62f);
            float ss = Mathf.Clamp((cw - 2 * (sndS + 30) - 4 * 12) / 5f, 30f, 70f);
            rowLayout.spacing = 12; rowRt.sizeDelta = new Vector2(5 * ss + 4 * 12, ss); rowRt.anchoredPosition = new Vector2(0, -18);
            for (int i = 0; i < 5; i++) { slotRt[i].sizeDelta = new Vector2(ss, ss); carRt[i].sizeDelta = new Vector2(ss * .78f, ss * .52f); }
        }

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            r.anchorMin = min; r.anchorMax = max; r.pivot = new Vector2(min.x == max.x ? min.x : .5f, min.y == max.y ? (min.y > .9f ? 1f : min.y < .1f ? 0f : .5f) : .5f);
            r.anchoredPosition = pos; r.sizeDelta = size;
        }
        static Image Img(RectTransform rt, Sprite s, Color c)
        {
            var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; return i;
        }
    }
}
