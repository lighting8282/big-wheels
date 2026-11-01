using UnityEngine;
using UnityEngine.InputSystem;

namespace BigWheels
{
    // Pedals (gas, brake, cruise) and the cosmetics the driver can change from inside the cab.
    public partial class Game
    {
        // ---------- pedals: cruise by default, gas to speed up, brake to slow down ----------
        public bool PedalGas { get; private set; }
        public bool PedalBrake { get; private set; }
        float gasMul = 1f;
        public float GasMul => gasMul;
        public void SetPedal(bool gas, bool down) { if (gas) PedalGas = down; else PedalBrake = down; }
        public void ReleasePedals() { PedalGas = PedalBrake = false; }
        public void DebugPedal(bool gas, bool brake) { PedalGas = gas; PedalBrake = brake; }

        /// <summary>Side view: Right/D or the right trigger speeds up, Left/A or the left trigger slows down. The on-screen pedals do the same.</summary>
        float ReadSideGas()
        {
            float g = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) g += 1f;
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) g -= 1f;
            }
            var gp = Gamepad.current;
            if (gp != null) g += gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue();
            if (PedalGas) g += 1f;
            if (PedalBrake) g -= 1f;
            return Mathf.Clamp(g, -1f, 1f);
        }

        // ---------- pause ----------
        public void OpenPause() { if (Started) hud.OpenPause(); }
        public void ClosePause() => hud.ClosePause();
        public void OpenCustomize() => hud.OpenCustomize();
        public void CloseCustomize() => hud.CloseCustomize();

        /// <summary>Esc or the pad's Start button: close whatever is open, otherwise pause.</summary>
        void EscapePressed()
        {
            if (hud.BookOpen) CloseBook();
            else if (hud.CustomizeOpen) hud.CloseCustomize();
            else if (hud.ParentCardOpen) CloseParent();
            else if (hud.PauseOpen) hud.ClosePause();
            else if (Started && !hud.BreakOpen) hud.OpenPause();
        }

        // ---------- cosmetics (the cab) ----------
        public const int CosRowCount = 5;
        public static readonly string[] CosRows = { "Paint", "Suit", "Wheel", "Hanging", "Dash sticker" };
        static readonly string[][] CosNames =
        {
            new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink" },
            new[] { "Red", "Yellow", "Blue", "Green", "Purple", "White" },
            new[] { "Black", "Red", "Yellow", "Blue", "Green", "Pink" },
            new[] { "Dice", "Skull", "Pine tree", "None" },
            new[] { "Skull", "Flame", "Star", "None" }
        };
        public static readonly Color[] SuitTints = { new Color(.85f, .16f, .18f), new Color(.95f, .72f, .1f), new Color(.2f, .45f, .9f), new Color(.2f, .65f, .3f), new Color(.5f, .25f, .8f), new Color(.92f, .92f, .95f) };
        public static readonly Color[] WheelTints = { new Color(.28f, .30f, .34f), new Color(.8f, .16f, .18f), new Color(.85f, .65f, .1f), new Color(.2f, .42f, .85f), new Color(.2f, .6f, .3f), new Color(.8f, .25f, .55f) };

        public int Cos(int row) => row == 0 ? ColorIndex : Mathf.Clamp(Settings.GetCos(row), 0, CosNames[row].Length - 1);
        public string CosName(int row) => CosNames[row][Mathf.Clamp(Cos(row), 0, CosNames[row].Length - 1)];

        public void CycleCos(int row, int dir)
        {
            int n = CosNames[row].Length, v = (Cos(row) + dir + n) % n;
            if (row == 0) SetColor(v); else Settings.SetCos(row, v);
            Sfx.Play("sparkle", .5f);
            if (Cockpit) cBobV += 40f;
        }
    }
}
