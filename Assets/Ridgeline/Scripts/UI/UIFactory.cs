using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ridgeline.UI
{
    /// <summary>Small helpers for building uGUI from code with a consistent look.</summary>
    public static class UIFactory
    {
        public static readonly Color Panel = new Color(0.08f, 0.11f, 0.16f, 0.88f);
        public static readonly Color PanelLight = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Accent = new Color(0.95f, 0.65f, 0.25f);
        public static readonly Color AccentDark = new Color(0.62f, 0.38f, 0.10f);
        public static readonly Color Good = new Color(0.40f, 0.78f, 0.36f);
        public static readonly Color Bad = new Color(0.90f, 0.30f, 0.25f);
        public static readonly Color TextMain = new Color(0.97f, 0.94f, 0.88f);
        public static readonly Color TextDim = new Color(0.97f, 0.94f, 0.88f, 0.65f);
        public static readonly Color Disabled = new Color(0.4f, 0.4f, 0.42f);

        static Font s_Font;
        /// <summary>Game font (CC0 Kenney Future) when set; Unity's built-in font otherwise.</summary>
        public static Font CustomFont;
        public static Font Font => CustomFont != null ? CustomFont
            : (s_Font != null ? s_Font : (s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            return img;
        }

        public static Text Text(string name, Transform parent, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            t.resizeTextMaxSize = size;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, int fontSize, Color bg, Action onClick, out Text labelText)
        {
            var img = Image(name, parent, bg, Art.PlaceholderArt.Square);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            labelText = Text("Label", img.transform, label, fontSize, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(labelText.rectTransform, 6f);
            return btn;
        }

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, RectOffset padding, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, RectOffset padding, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return h;
        }

        public static LayoutElement Layout(Component c, float minH = -1, float prefH = -1, float flexW = -1, float prefW = -1, float minW = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (minH >= 0) le.minHeight = minH;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (minW >= 0) le.minWidth = minW;
            return le;
        }

        /// <summary>Blocks clicks to whatever is behind a modal.</summary>
        public static Image Blocker(Transform parent, float alpha = 0.55f)
        {
            var img = Image("Blocker", parent, new Color(0, 0, 0, alpha));
            Stretch(img.rectTransform);
            img.raycastTarget = true;
            return img;
        }
    }

    /// <summary>
    /// Minimal horizontal slider (drag or tap). Avoids the verbose default Slider prefab setup.
    /// </summary>
    public class SimpleSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public RectTransform track;
        public RectTransform fill;
        public RectTransform handle;
        public event Action<float> Changed;
        public float Value { get; private set; }

        public void SetValueWithoutNotify(float v)
        {
            Value = Mathf.Clamp01(v);
            Layout();
        }

        public void OnPointerDown(PointerEventData e) => SetFromPointer(e);
        public void OnDrag(PointerEventData e) => SetFromPointer(e);

        void SetFromPointer(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var local)) return;
            var r = track.rect;
            Value = Mathf.Clamp01((local.x - r.xMin) / Mathf.Max(1f, r.width));
            Layout();
            Changed?.Invoke(Value);
        }

        void Layout()
        {
            if (fill) fill.anchorMax = new Vector2(Value, 1f);
            if (handle) handle.anchorMin = handle.anchorMax = new Vector2(Value, 0.5f);
        }

        public static SimpleSlider Create(Transform parent, float height)
        {
            var root = UIFactory.Image("Slider", parent, new Color(0, 0, 0, 0));
            UIFactory.Layout(root, minH: height, prefH: height, flexW: 1);
            var track = UIFactory.Image("Track", root.transform, new Color(1, 1, 1, 0.15f), Art.PlaceholderArt.Square);
            track.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            track.rectTransform.offsetMin = track.rectTransform.offsetMax = Vector2.zero;
            var fill = UIFactory.Image("Fill", track.transform, UIFactory.Accent, Art.PlaceholderArt.Square);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.raycastTarget = false;
            var handle = UIFactory.Image("Handle", track.transform, UIFactory.TextMain, Art.PlaceholderArt.Circle);
            handle.rectTransform.sizeDelta = new Vector2(height, height);
            handle.raycastTarget = false;

            var s = root.gameObject.AddComponent<SimpleSlider>();
            s.track = track.rectTransform;
            s.fill = fill.rectTransform;
            s.handle = handle.rectTransform;
            root.raycastTarget = true;
            return s;
        }
    }
}
