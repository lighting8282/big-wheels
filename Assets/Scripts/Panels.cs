using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>Tiny helpers for building the pause and customise panels in code.</summary>
    static class Kit
    {
        static Font font;
        public static Font F => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        public static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public static Image Img(RectTransform rt, Sprite s, Color c) { var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; return i; }

        /// <summary>Places a rect from the top-left corner of its parent.</summary>
        public static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }

        public static Text Label(Transform parent, string s, int size, FontStyle style, TextAnchor align, Color col)
        {
            var rt = Rt("Text", parent); Stretch(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F; t.text = s; t.fontSize = size; t.fontStyle = style; t.alignment = align; t.color = col; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static RectTransform Btn(Transform parent, string label, Color col, float x, float y, float w, float h, int size, UnityEngine.Events.UnityAction click, out Text lbl)
        {
            var rt = Rt("Button", parent); Place(rt, x, y, w, h);
            var img = Img(rt, Art3.Card, col); img.type = Image.Type.Sliced;
            rt.gameObject.AddComponent<Button>().onClick.AddListener(click);
            lbl = Label(rt, label, size, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            return rt;
        }

        public static RectTransform Dim(Transform canvasRoot, string name, out RectTransform card, float w, float h)
        {
            var dim = Rt(name, canvasRoot); Stretch(dim); Img(dim, null, new Color(0, 0, 0, .62f));
            card = Rt("Card", dim); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.sizeDelta = new Vector2(w, h);
            var ci = Img(card, Art3.Card, new Color(.11f, .12f, .16f, .97f)); ci.type = Image.Type.Sliced;
            return dim;
        }
    }

    /// <summary>A pedal on screen: reports whether it is being held.</summary>
    public class PedalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action<bool> OnState;
        public void OnPointerDown(PointerEventData e) { OnState?.Invoke(true); }
        public void OnPointerUp(PointerEventData e) { OnState?.Invoke(false); }
        public void OnPointerExit(PointerEventData e) { OnState?.Invoke(false); }
    }

    /// <summary>The pause screen: resume, customise (in the cab), settings, and a switch between the two views.</summary>
    public class PauseUi
    {
        readonly Game game; readonly GameObject root, customBtn; readonly Text viewLbl; readonly RectTransform card, settingsRt, viewRt;
        bool open;
        public bool IsOpen => open;

        public PauseUi(Game game, Transform canvasRoot, System.Action settings, System.Action customise)
        {
            this.game = game;
            var dim = Kit.Dim(canvasRoot, "PauseScreen", out var cd, 400, 450);
            root = dim.gameObject; card = cd;
            var title = Kit.Label(card, "PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1); title.rectTransform.pivot = new Vector2(.5f, 1);
            title.rectTransform.offsetMin = new Vector2(0, -96); title.rectTransform.offsetMax = new Vector2(0, -22);
            Text l;
            Kit.Btn(card, "Resume", Palette.Hex("#2f8a4a"), 40, 110, 320, 66, 32, Close, out l);
            customBtn = Kit.Btn(card, "Customize", Palette.Hex("#2b4f8a"), 40, 192, 320, 62, 28, () => customise(), out l).gameObject;
            settingsRt = Kit.Btn(card, "Settings", Palette.Hex("#4a4f5c"), 40, 270, 320, 62, 28, () => settings(), out l);
            viewRt = Kit.Btn(card, "", Palette.Hex("#6b4a8a"), 40, 348, 320, 62, 26, () => { game.ToggleView(); Refresh(); }, out viewLbl);
            root.SetActive(false);
        }

        void Refresh()
        {
            viewLbl.text = game.Cockpit ? "Switch to side view" : "Switch to cockpit view";
            bool cb = game.Cockpit;
            customBtn.SetActive(cb);
            Kit.Place(settingsRt, 40, cb ? 270 : 192, 320, 62); Kit.Place(viewRt, 40, cb ? 348 : 270, 320, 62);
            card.sizeDelta = new Vector2(400, cb ? 450 : 370);
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

    /// <summary>The cosmetics panel: paint, suit, wheel, hanging charm and dash sticker, each with arrows. Choices are saved.</summary>
    public class CustomizeUi
    {
        readonly Game game; readonly GameObject root; readonly Text[] val = new Text[Game.CosRowCount];
        bool open;
        public bool IsOpen => open;

        public CustomizeUi(Game game, Transform canvasRoot)
        {
            this.game = game;
            var dim = Kit.Dim(canvasRoot, "CustomizeScreen", out var card, 620, 560);
            root = dim.gameObject;
            var title = Kit.Label(card, "CUSTOMIZE", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1); title.rectTransform.pivot = new Vector2(.5f, 1);
            title.rectTransform.offsetMin = new Vector2(0, -90); title.rectTransform.offsetMax = new Vector2(0, -18);
            for (int i = 0; i < Game.CosRowCount; i++)
            {
                int row = i; float y = 100 + i * 72;
                var name = Kit.Rt("Row", card); Kit.Place(name, 28, y, 200, 56);
                Kit.Label(name, Game.CosRows[i], 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1, 1, 1, .85f));
                Text l;
                Kit.Btn(card, "<", Palette.Hex("#3a4150"), 250, y, 56, 56, 34, () => { game.CycleCos(row, -1); Refresh(); }, out l);
                var v = Kit.Rt("Value", card); Kit.Place(v, 310, y, 190, 56);
                val[i] = Kit.Label(v, "", 26, FontStyle.Bold, TextAnchor.MiddleCenter, Palette.Hex("#ffd23f"));
                Kit.Btn(card, ">", Palette.Hex("#3a4150"), 504, y, 56, 56, 34, () => { game.CycleCos(row, 1); Refresh(); }, out l);
            }
            Kit.Btn(card, "Done", Palette.Hex("#2f8a4a"), 150, 472, 320, 60, 30, Close, out var dl);
            root.SetActive(false);
        }

        void Refresh() { for (int i = 0; i < val.Length; i++) val[i].text = game.CosName(i); }

        public void Open()
        {
            if (open) return;
            open = true; game.Hold(true); Refresh(); root.SetActive(true);
        }

        public void Close()
        {
            if (!open) return;
            open = false; game.Hold(false); root.SetActive(false);
        }
    }
}
