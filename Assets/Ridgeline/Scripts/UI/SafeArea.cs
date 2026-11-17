using UnityEngine;

namespace Ridgeline.UI
{
    /// <summary>Keeps a RectTransform inside Screen.safeArea (notches, rounded corners). Re-checks on resize.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _last;
        Vector2Int _lastScreen;

        void Awake() => _rt = (RectTransform)transform;

        void Update()
        {
            var sa = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (sa == _last && screen == _lastScreen) return;
            _last = sa;
            _lastScreen = screen;
            if (screen.x <= 0 || screen.y <= 0) return; // 0-sized viewport: keep the previous layout

            _rt.anchorMin = new Vector2(sa.xMin / screen.x, sa.yMin / screen.y);
            _rt.anchorMax = new Vector2(sa.xMax / screen.x, sa.yMax / screen.y);
            _rt.offsetMin = _rt.offsetMax = Vector2.zero;
        }
    }
}
