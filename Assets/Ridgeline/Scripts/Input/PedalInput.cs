using UnityEngine;
using UnityEngine.InputSystem;

namespace Ridgeline.InputControl
{
    /// <summary>
    /// Single source of truth for the two pedals. On-screen pedals (touch + mouse) call
    /// <see cref="SetGasHeld"/>/<see cref="SetBrakeHeld"/>; keyboard is read here.
    /// Keyboard: Right/D = gas, Left/A = brake. Esc is never consumed here.
    /// </summary>
    public static class PedalInput
    {
        static bool s_GasUI, s_BrakeUI;

        public static void SetGasHeld(bool held) => s_GasUI = held;
        public static void SetBrakeHeld(bool held) => s_BrakeUI = held;

        public static void ReleaseAll()
        {
            s_GasUI = false;
            s_BrakeUI = false;
        }

        public static bool Gas => s_GasUI || KeyHeld(Key.RightArrow) || KeyHeld(Key.D) || KeyHeld(Key.UpArrow) || KeyHeld(Key.W);
        public static bool Brake => s_BrakeUI || KeyHeld(Key.LeftArrow) || KeyHeld(Key.A) || KeyHeld(Key.DownArrow) || KeyHeld(Key.S);

        static bool KeyHeld(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }
    }
}
