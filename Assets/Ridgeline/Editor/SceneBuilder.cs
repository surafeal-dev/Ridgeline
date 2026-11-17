using System.IO;
using Ridgeline.Audio;
using Ridgeline.CameraControl;
using Ridgeline.Game;
using Ridgeline.Platform;
using Ridgeline.UI;
using Ridgeline.Vehicle;
using Ridgeline.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YTGameSDK;

namespace Ridgeline.EditorTools
{
    /// <summary>
    /// Creates (or re-creates) the MVP content and the single game scene:
    ///  - Materials/SpriteUnlit.mat (URP 2D unlit sprite shader, also used for the vertex-coloured terrain)
    ///  - Data/Jeep.asset (VehicleConfig) and Data/Meadow.asset (StageConfig) - only if missing
    ///  - Scenes/Main.unity, set as the only scene in the build
    /// Menu: Ridgeline > Build Main Scene, or headless via -executeMethod.
    /// </summary>
    public static class SceneBuilder
    {
        const string Root = "Assets/Ridgeline";
        const string MatPath = Root + "/Materials/SpriteUnlit.mat";
        const string JeepPath = Root + "/Data/Jeep.asset";
        const string MeadowPath = Root + "/Data/Meadow.asset";
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Ridgeline/Build Main Scene")]
        public static void Build()
        {
            ProjectLayers.Ensure();
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Data");

            var mat = LoadOrCreateMaterial();
            var jeep = LoadOrCreate<VehicleConfig>(JeepPath, c => { c.id = "jeep"; c.displayName = "Jeep"; });
            var meadow = LoadOrCreate<StageConfig>(MeadowPath, s => { s.id = "meadow"; s.displayName = "Meadow"; });
            var artPath = AssetDatabase.GetAssetPath(ArtSetBuilder.Build());

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Re-load after NewScene: opening a scene can unload assets that nothing referenced yet,
            // leaving earlier C# references dead.
            var art = AssetDatabase.LoadAssetAtPath<Ridgeline.Art.ArtSet>(artPath);
            mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            jeep = AssetDatabase.LoadAssetAtPath<VehicleConfig>(JeepPath);
            meadow = AssetDatabase.LoadAssetAtPath<StageConfig>(MeadowPath);

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = art != null ? art.skyColor : meadow.skyBottom;
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<FollowCamera>();

            // Far scenery (parallax). Not parented to the camera: it positions itself each frame.
            ParallaxBackground background = null;
            if (art != null && art.farBackgroundMaterial != null)
            {
                var bgGo = new GameObject("Background", typeof(MeshFilter), typeof(MeshRenderer));
                bgGo.GetComponent<MeshRenderer>().sharedMaterial = art.farBackgroundMaterial;
                background = bgGo.AddComponent<ParallaxBackground>();
                background.cam = cam;
            }

            // YouTube SDK wrapper: the GameObject MUST be named "YTGameWrapper".
            var ytGo = new GameObject("YTGameWrapper");
            // Single-scene game: keep the wrapper scene-owned so reloads never leave a stale copy behind.
            ytGo.AddComponent<YTGameWrapper>().DontDestroyOnSceneChange = false;
            var bridge = ytGo.AddComponent<PlayablesBridge>();

            // World
            var terrainGo = new GameObject("Terrain");
            var terrain = terrainGo.AddComponent<TerrainGenerator>();
            terrain.stage = meadow;
            terrain.groundMaterial = mat;
            terrain.spriteMaterial = mat;
            terrain.art = art;

            // Game
            var gameGo = new GameObject("Game");
            var gm = gameGo.AddComponent<GameManager>();
            var run = gameGo.AddComponent<RunManager>();
            var audio = gameGo.AddComponent<AudioService>();
            var ui = gameGo.AddComponent<GameUI>();
            gm.vehicleConfig = jeep;
            gm.stageConfig = meadow;
            gm.spriteMaterial = mat;
            gm.groundMaterial = mat;
            gm.bridge = bridge;
            gm.followCamera = follow;
            gm.background = background;
            gm.art = art;
            gm.terrain = terrain;
            gm.run = run;
            gm.audioService = audio;
            gm.ui = ui;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Ridgeline] Main scene built: " + ScenePath);
        }

        static Material LoadOrCreateMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");
            mat = new Material(shader) { name = "SpriteUnlit" };
            AssetDatabase.CreateAsset(mat, MatPath);
            return mat;
        }

        static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
