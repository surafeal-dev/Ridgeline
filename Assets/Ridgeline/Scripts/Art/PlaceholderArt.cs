using UnityEngine;

namespace Ridgeline.Art
{
    /// <summary>
    /// Tiny runtime-generated sprites (white square, circle, rounded box) used until real art exists.
    /// Each sprite is 1 world unit across; scale and tint them per use.
    /// </summary>
    public static class PlaceholderArt
    {
        static Sprite s_Square, s_Circle, s_Ring;

        // NOTE: use Unity's == null (not ??=). With domain reload disabled in the editor, statics survive
        // between play sessions while the sprites themselves are destroyed; ??= would return the dead object.
        public static Sprite Square => s_Square != null ? s_Square : (s_Square = Make("Square", 8, (x, y, n) => 1f));
        public static Sprite Circle => s_Circle != null ? s_Circle : (s_Circle = Make("Circle", 64, CircleAlpha));
        /// <summary>Circle with a darker ring (tyre look) when tinted.</summary>
        public static Sprite Ring => s_Ring != null ? s_Ring : (s_Ring = Make("Ring", 64, RingAlpha, ringShade: true));

        static float CircleAlpha(int x, int y, int n)
        {
            float c = (n - 1) * 0.5f;
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            return Mathf.Clamp01(c - d + 0.5f);
        }

        static float RingAlpha(int x, int y, int n) => CircleAlpha(x, y, n);

        static Sprite Make(string name, int n, System.Func<int, int, int, float> alpha, bool ringShade = false)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "Placeholder" + name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float a = alpha(x, y, n);
                byte v = 255;
                if (ringShade)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    v = d > 0.62f ? (byte)70 : (d > 0.45f ? (byte)200 : (byte)140);
                }
                px[y * n + x] = new Color32(v, v, v, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }
    }
}
