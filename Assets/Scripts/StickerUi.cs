using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BigWheels
{
    /// <summary>
    /// Picture-only sticker book UI: a book button, a "you got a sticker" pop-up that flies to the button,
    /// and a full-screen page of 25 sticker slots (grey silhouettes until earned; tap an earned one to hear it).
    /// </summary>
    public class StickerUi
    {
        const int N = Stickers.Count;
        readonly Game game;
        readonly RectTransform safe, bookBtn, rewardRt, bookRoot, gridRt, closeRt, headerRt;
        readonly GameObject bookBg, rewardGo;
        readonly Image rewardBg, rewardIcon;
        readonly RectTransform[] cellRt = new RectTransform[N];
        readonly Image[] cellBg = new Image[N], cellIcon = new Image[N];
        readonly float[] bounce = new float[N];
        readonly GridLayoutGroup grid;
        readonly Queue<int> queue = new();
        int state;                   // 0 idle, 1 pop, 2 fly
        float stateT, cw, ch, btn = 84f;
        bool open;
        public bool IsOpen => open;

        public StickerUi(Game game, Transform canvasRoot, RectTransform safe)
        {
            this.game = game; this.safe = safe;

            // book button (top left)
            bookBtn = Rt("BookButton", safe); Anchor(bookBtn, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(84, 84), new Vector2(0, 1));
            Img(bookBtn, Art.UiRingFilled, Color.white);
            bookBtn.gameObject.AddComponent<Button>().onClick.AddListener(Open);
            var bi = Rt("Icon", bookBtn); Anchor(bi, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(56, 50), new Vector2(.5f, .5f));
            var bimg = Img(bi, Art3.IconBook, Color.white); bimg.preserveAspect = true; bimg.raycastTarget = false;

            // reward pop-up (centre of the safe area, ignores taps)
            rewardRt = Rt("Reward", safe); Anchor(rewardRt, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(280, 280), new Vector2(.5f, .5f));
            rewardBg = Img(rewardRt, Art.UiRingFilled, Color.white); rewardBg.raycastTarget = false;
            var ri = Rt("Icon", rewardRt); Anchor(ri, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(190, 190), new Vector2(.5f, .5f));
            rewardIcon = Img(ri, null, Color.white); rewardIcon.preserveAspect = true; rewardIcon.raycastTarget = false;
            rewardGo = rewardRt.gameObject; rewardGo.SetActive(false);

            // book page (above everything)
            var bg = Rt("BookBackground", canvasRoot); Stretch(bg);
            Img(bg, null, Palette.Hex("#fff3d6"));
            bookBg = bg.gameObject; bookBg.SetActive(false);

            bookRoot = Rt("BookPage", canvasRoot); Stretch(bookRoot);
            bookRoot.gameObject.SetActive(false);

            headerRt = Rt("Header", bookRoot); Anchor(headerRt, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(110, 96), new Vector2(.5f, 1));
            var hi = Img(headerRt, Art3.IconBook, Color.white); hi.preserveAspect = true; hi.raycastTarget = false;

            closeRt = Rt("Close", bookRoot); Anchor(closeRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(96, 96), new Vector2(1, 1));
            Img(closeRt, Art.UiRingFilled, Color.white);
            closeRt.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var ci = Rt("Icon", closeRt); Anchor(ci, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(52, 52), new Vector2(.5f, .5f));
            Img(ci, Art3.IconClose, Color.white).raycastTarget = false;

            gridRt = Rt("Grid", bookRoot); Anchor(gridRt, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -130), new Vector2(600, 400), new Vector2(.5f, 1));
            grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(12, 12); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 7;
            grid.childAlignment = TextAnchor.UpperCenter; grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            for (int i = 0; i < N; i++)
            {
                int id = i;
                cellRt[i] = Rt("Sticker" + i, gridRt);
                cellBg[i] = Img(cellRt[i], Art.UiRingFilled, Color.white);
                cellRt[i].gameObject.AddComponent<Button>().onClick.AddListener(() => TapCell(id));
                var ic = Rt("Icon", cellRt[i]);
                ic.anchorMin = new Vector2(.17f, .17f); ic.anchorMax = new Vector2(.83f, .83f); ic.offsetMin = ic.offsetMax = Vector2.zero;
                cellIcon[i] = Img(ic, null, Color.white); cellIcon[i].preserveAspect = true; cellIcon[i].raycastTarget = false;
            }
        }

        /// <summary>Called by the HUD whenever the layout is recomputed.</summary>
        public void Fit(float cw, float ch, float topBtn)
        {
            this.cw = cw; this.ch = ch; btn = topBtn;
            bookBtn.sizeDelta = new Vector2(btn, btn); bookBtn.anchoredPosition = new Vector2(18, -18);
            ((RectTransform)bookBtn.GetChild(0)).sizeDelta = new Vector2(btn * .66f, btn * .6f);
            if (open) FitBook();
        }

        void FitBook()
        {
            if (cw < 100f) return;
            int cols = cw > ch ? 8 : 4, rows = Mathf.CeilToInt(N / (float)cols);
            float header = 120f;
            float cell = Mathf.Min(200f, (cw - 48f - (cols - 1) * 12f) / cols, (ch - header - 30f - (rows - 1) * 12f) / rows);
            cell = Mathf.Max(40f, cell);
            grid.constraintCount = cols; grid.cellSize = new Vector2(cell, cell);
            gridRt.sizeDelta = new Vector2(cols * cell + (cols - 1) * 12f, rows * cell + (rows - 1) * 12f);
            gridRt.anchoredPosition = new Vector2(0, -header);
            float cb = Mathf.Min(96f, btn * 1.15f);
            closeRt.sizeDelta = new Vector2(cb, cb); ((RectTransform)closeRt.GetChild(0)).sizeDelta = new Vector2(cb * .55f, cb * .55f);
        }

        public void Show(int id) { queue.Enqueue(id); }

        public void Open()
        {
            if (open) return;
            open = true; game.Hold(true); Stickers.MarkSeen();
            bookBg.SetActive(true); bookRoot.gameObject.SetActive(true);
            SyncAnchors(); FitBook(); UpdateCells();
            Sfx.Play("swoosh");
        }

        public void Close()
        {
            if (!open) return;
            open = false; game.Hold(false);
            bookBg.SetActive(false); bookRoot.gameObject.SetActive(false);
            Sfx.Play("swoosh", .6f);
        }

        void SyncAnchors() { bookRoot.anchorMin = safe.anchorMin; bookRoot.anchorMax = safe.anchorMax; bookRoot.offsetMin = bookRoot.offsetMax = Vector2.zero; }

        void UpdateCells()
        {
            for (int i = 0; i < N; i++)
            {
                var d = Stickers.Get(i); bool have = Stickers.Have(i);
                cellBg[i].color = have ? d.Bg : new Color(.83f, .85f, .9f);
                cellIcon[i].sprite = d.Icon;
                cellIcon[i].color = have ? Color.white : new Color(0, 0, 0, .25f);
                cellIcon[i].rectTransform.localRotation = Quaternion.Euler(0, 0, d.Rot);
            }
        }

        void TapCell(int i)
        {
            bounce[i] = 1f;
            if (!Stickers.Have(i)) { Sfx.Play("boing", .3f); return; }
            string s = Stickers.Get(i).Sfx;
            if (s == "pop") Sfx.Pop(Random.Range(0, 8)); else Sfx.Play(s);
        }

        public void Refresh(float time)
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            bookBtn.localScale = Vector3.one * (Stickers.Unseen != 0 ? 1f + .08f * Mathf.Sin(time * 6f) : 1f);

            if (open)
            {
                SyncAnchors();
                for (int i = 0; i < N; i++)
                {
                    if (bounce[i] > 0) bounce[i] = Mathf.Max(0, bounce[i] - dt * 2.5f);
                    cellRt[i].localScale = Vector3.one * (1f + .25f * Mathf.Sin(bounce[i] * Mathf.PI));
                }
                return;
            }

            switch (state)
            {
                case 0:
                    if (queue.Count > 0)
                    {
                        int id = queue.Dequeue(); var d = Stickers.Get(id);
                        rewardBg.color = d.Bg; rewardIcon.sprite = d.Icon;
                        rewardIcon.rectTransform.localRotation = Quaternion.Euler(0, 0, d.Rot);
                        rewardGo.SetActive(true); rewardRt.localScale = Vector3.zero; state = 1; stateT = 0;
                    }
                    break;
                case 1:
                    {
                        stateT += dt;
                        const float dur = 1.0f;
                        float x = Mathf.Clamp01(stateT / .35f) - 1f, back = 1f + 2.70158f * x * x * x + 1.70158f * x * x;
                        rewardRt.localScale = Vector3.one * back;
                        rewardRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(stateT * 12f) * 7f * (1f - stateT / dur));
                        rewardRt.anchoredPosition = new Vector2(0, 40f);
                        if (stateT >= dur) { state = 2; stateT = 0; }
                        break;
                    }
                case 2:
                    {
                        stateT += dt;
                        float p = Mathf.Clamp01(stateT / .55f), e = p * p * (3 - 2 * p);
                        Vector2 target = new Vector2(-safe.rect.width / 2f + bookBtn.anchoredPosition.x + btn / 2f, safe.rect.height / 2f + bookBtn.anchoredPosition.y - btn / 2f);
                        rewardRt.anchoredPosition = Vector2.Lerp(new Vector2(0, 40f), target, e);
                        rewardRt.localScale = Vector3.one * Mathf.Lerp(1f, btn / 280f, e);
                        rewardRt.localRotation = Quaternion.Euler(0, 0, e * 360f);
                        if (p >= 1f) { rewardGo.SetActive(false); state = 0; Sfx.Blip(); }
                        break;
                    }
            }
        }

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            r.anchorMin = min; r.anchorMax = max; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
        }
        static Image Img(RectTransform rt, Sprite s, Color c)
        {
            var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; return i;
        }
    }
}
