using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ridgeline.InputControl
{
    /// <summary>
    /// On-screen pedal. Works with touch (multi-touch: both pedals at once) and mouse,
    /// via EventSystem pointer events. Each pointer is tracked by id; sliding off releases.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class PedalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Pedal { Gas, Brake }

        public Pedal pedal = Pedal.Gas;
        [Tooltip("Scale applied while held, for visual feedback.")]
        public float pressedScale = 0.92f;

        readonly HashSet<int> _pointers = new HashSet<int>();
        Vector3 _baseScale = Vector3.one;

        void Awake() => _baseScale = transform.localScale;

        void OnDisable()
        {
            _pointers.Clear();
            Apply();
        }

        public void OnPointerDown(PointerEventData e)
        {
            _pointers.Add(e.pointerId);
            Apply();
        }

        public void OnPointerUp(PointerEventData e)
        {
            _pointers.Remove(e.pointerId);
            Apply();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (_pointers.Remove(e.pointerId)) Apply();
        }

        // Safety net: if a release event is ever missed (pointer lifted outside the window, focus lost,
        // pause overlay), let go as soon as no mouse button or finger is actually down.
        void Update()
        {
            if (_pointers.Count == 0 || AnyPointerDown()) return;
            _pointers.Clear();
            Apply();
        }

        static bool AnyPointerDown()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return true;
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null)
                foreach (var t in touch.touches)
                    if (t.press.isPressed) return true;
            var pen = UnityEngine.InputSystem.Pen.current;
            return pen != null && pen.tip.isPressed;
        }

        void Apply()
        {
            bool held = _pointers.Count > 0;
            if (pedal == Pedal.Gas) PedalInput.SetGasHeld(held);
            else PedalInput.SetBrakeHeld(held);
            transform.localScale = held ? _baseScale * pressedScale : _baseScale;
        }
    }
}
