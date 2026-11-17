using System.IO;
using System.Linq;
using Ridgeline.Art;
using UnityEditor;
using UnityEngine;

namespace Ridgeline.EditorTools
{
    /// <summary>
    /// Import rules for Assets/Ridgeline/Art and the Ridgeline > Build Art Set menu, which
    ///  - draws the fuel can sprite (no CC0 one was found) if it is missing,
    ///  - turns the Jeep into an open-top (from the original in Art/Source~) so the bobblehead driver fits,
    ///  - creates the terrain/background materials,
    ///  - fills Data/ArtSet.asset.
    /// </summary>
    public class ArtImportRules : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Ridgeline/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var ti = (TextureImporter)assetImporter;
            bool tiled = assetPath.Contains("/Terrain/") || assetPath.Contains("/Background/");
            if (tiled)
            {
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapModeU = TextureWrapMode.Repeat;
                // Grass-top and background tiles repeat only sideways; clamping V stops the opposite edge bleeding in.
                ti.wrapModeV = assetPath.Contains("/Background/") || assetPath.Contains("grass_top") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                ti.filterMode = FilterMode.Bilinear;
            }
            else
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.alphaIsTransparency = true;
            }
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Ridgeline/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.5f;
            ai.defaultSampleSettings = s;
            ai.forceToMono = true;
        }
    }

    public static class ArtSetBuilder
    {
        const string Art = "Assets/Ridgeline/Art";
        const string SetPath = "Assets/Ridgeline/Data/ArtSet.asset";
        const string MatDir = Art + "/Materials";
        const string FuelPath = Art + "/Pickups/fuel_can.png";
        const string BodyPath = Art + "/Vehicle/jeep_body.png";

        [MenuItem("Ridgeline/Build Art Set")]
        public static ArtSet Build()
        {
            Directory.CreateDirectory(MatDir);
            if (!File.Exists(FuelPath)) DrawFuelCan(FuelPath);
            MakeOpenCab(Art + "/Source~/jeep_body_original.png", BodyPath);
            AssetDatabase.Refresh();

            var set = AssetDatabase.LoadAssetAtPath<ArtSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ArtSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            set.jeepBody = S("Vehicle/jeep_body.png");
            set.jeepTire = S("Vehicle/jeep_tire.png");
            set.driverHead = S("Driver/driver_head.png");
            set.driverHeadHurt = S("Driver/driver_headShock.png");
            set.driverBody = S("Driver/driver_body.png");
            set.driverArm = S("Driver/driver_arm.png");

            set.grassMaterial = Mat("Grass", T("Terrain/grass_top.png"));
            set.dirtMaterial = Mat("Dirt", T("Terrain/dirt.png"));
            var far = T("Background/meadow_far.png");
            set.farBackgroundMaterial = Mat("FarBackground", far);
            set.skyColor = TopColor(Art + "/Background/meadow_far.png", set.skyColor);

            set.coinBronze = S("Pickups/coin_bronze.png");
            set.coinSilver = S("Pickups/coin_silver.png");
            set.coinGold = S("Pickups/coin_gold.png");
            set.coinGem = S("Pickups/gem_blue.png");
            set.fuelCan = S("Pickups/fuel_can.png");

            set.trees = new[] { S("Decor/tree.png"), S("Decor/treeLong.png") }.Where(x => x).ToArray();
            set.bushes = new[] { S("Decor/bush1.png"), S("Decor/bush3.png"), S("Decor/small_bush.png") }.Where(x => x).ToArray();
            set.smallProps = new[] { S("Decor/small_rock.png"), S("Decor/small_grass.png") }.Where(x => x).ToArray();
            set.milestoneFlag = S("Decor/flag.png");

            set.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Ridgeline/Fonts/LilitaOne-Regular.ttf");
            set.hudCoin = S("UI/hud_coin.png");

            set.coin = A("coin"); set.fuel = A("fuel"); set.bonus = A("bonus");
            set.click = A("click"); set.crash = A("crash"); set.bump = A("bump");

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ridgeline] Art set built: " + SetPath);
            return set;
        }

        static Sprite S(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + rel);
        static Texture2D T(string rel) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/" + rel);
        static AudioClip A(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Ridgeline/Audio/" + name + ".ogg");

        static Material Mat(string name, Texture2D tex)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Color TopColor(string path, Color fallback)
        {
            var tex = Load(path);
            if (tex == null) return fallback;
            var c = tex.GetPixel(tex.width / 2, tex.height - 2);
            Object.DestroyImmediate(tex);
            return c;
        }

        static Texture2D Load(string path)
        {
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        /// <summary>
        /// Turns the CC0 truck's closed cab into an open-top jeep so the bobblehead driver fits:
        /// removes the roof, rear pillar and side window, keeps the windshield (made see-through).
        /// Always rebuilt from the untouched original in Art/Source~ (folders ending in ~ are not imported).
        /// </summary>
        static void MakeOpenCab(string sourcePath, string outPath)
        {
            var tex = Load(sourcePath);
            if (tex == null) { Debug.LogWarning("[Ridgeline] Missing " + sourcePath); return; }
            // Measured on the 400x144 original (x from the left, rows from the top).
            float sx = tex.width / 400f, sy = tex.height / 144f;
            int cutX0 = Mathf.RoundToInt(186 * sx), cutX1 = Mathf.RoundToInt(243 * sx);
            int beltRow = Mathf.RoundToInt(56 * sy); // door line: everything above it in the cab is removed
            int windX1 = Mathf.RoundToInt(300 * sx);
            for (int y = 0; y < tex.height; y++)
            {
                int rowFromTop = tex.height - 1 - y;
                if (rowFromTop >= beltRow) continue;
                for (int x = cutX0; x < windX1; x++)
                {
                    var c = tex.GetPixel(x, y);
                    if (x < cutX1) { tex.SetPixel(x, y, Color.clear); continue; }
                    bool glass = c.b > 0.75f && c.g > 0.6f && c.r < 0.85f && c.b > c.r + 0.1f;
                    bool shine = c.r > 0.9f && c.g > 0.9f && c.b > 0.9f;
                    if (c.a > 0.99f && (glass || shine)) tex.SetPixel(x, y, new Color(c.r, c.g, c.b, glass ? 0.45f : 0.7f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Flat-style jerry can (128x128) in the same look as the Kenney art: red body, dark outline, handle, spout.</summary>
        static void DrawFuelCan(string path)
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color32[N * N];
            var red = new Color32(226, 61, 54, 255);
            var redLight = new Color32(244, 104, 92, 255);
            var outline = new Color32(120, 30, 34, 255);
            var metal = new Color32(70, 74, 86, 255);

            float RoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
            {
                float dx = Mathf.Max(Mathf.Abs(x - cx) - (hw - r), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - cy) - (hh - r), 0f);
                return Mathf.Sqrt(dx * dx + dy * dy) - r; // <0 inside
            }

            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                Color32 c = new Color32(0, 0, 0, 0);
                // Spout (top right, tilted)
                float sx = (fx - 92f) * 0.8f + (fy - 112f) * 0.6f, sy = -(fx - 92f) * 0.6f + (fy - 112f) * 0.8f;
                float spout = RoundRect(sx, sy, 0f, 0f, 7f, 12f, 3f);
                if (spout < 0f) c = spout > -3f ? outline : metal;
                // Handle (top left): outer frame minus hole
                float hOuter = RoundRect(fx, fy, 50f, 108f, 26f, 14f, 8f);
                float hHole = RoundRect(fx, fy, 50f, 106f, 15f, 6f, 4f);
                if (hOuter < 0f && hHole > 0f) c = (hOuter > -3.5f || hHole < 3.5f) ? outline : red;
                // Body
                float body = RoundRect(fx, fy, 64f, 54f, 44f, 48f, 12f);
                if (body < 0f)
                {
                    c = body > -4f ? outline : red;
                    // Embossed X
                    float d1 = Mathf.Abs((fx - 64f) - (fy - 54f) * 0.85f) / 1.31f;
                    float d2 = Mathf.Abs((fx - 64f) + (fy - 54f) * 0.85f) / 1.31f;
                    float inner = RoundRect(fx, fy, 64f, 54f, 32f, 36f, 8f);
                    if (body < -4f && inner < 0f && (d1 < 4.5f || d2 < 4.5f)) c = redLight;
                    if (body < -4f && Mathf.Abs(inner) < 2f) c = outline;
                }
                px[y * N + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
