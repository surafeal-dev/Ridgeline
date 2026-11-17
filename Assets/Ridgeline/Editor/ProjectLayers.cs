using UnityEditor;
using UnityEngine;

namespace Ridgeline.EditorTools
{
    /// <summary>
    /// Makes sure the physics layers the game code relies on exist:
    /// 6 = Ground, 7 = Vehicle, 8 = Pickup. Runs on every editor load; idempotent.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectLayers
    {
        public const int Ground = 6;
        public const int Vehicle = 7;
        public const int Pickup = 8;

        static ProjectLayers() => EditorApplication.delayCall += Ensure;

        [MenuItem("Ridgeline/Ensure Layers")]
        public static void Ensure()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            bool changed = false;
            changed |= SetLayer(layers, Ground, "Ground");
            changed |= SetLayer(layers, Vehicle, "Vehicle");
            changed |= SetLayer(layers, Pickup, "Pickup");

            if (changed)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Ridgeline] Layers set: 6 Ground, 7 Vehicle, 8 Pickup.");
            }

            // Pickups only need to touch the vehicle; vehicle parts never hit each other's pickups twice.
            Physics2D.IgnoreLayerCollision(Pickup, Pickup, true);
            Physics2D.IgnoreLayerCollision(Pickup, Ground, true);
            // Only one vehicle should exist, but never let an old and new one collide during a respawn.
            Physics2D.IgnoreLayerCollision(Vehicle, Vehicle, true);
        }

        static bool SetLayer(SerializedProperty layers, int index, string name)
        {
            var p = layers.GetArrayElementAtIndex(index);
            if (p.stringValue == name) return false;
            if (!string.IsNullOrEmpty(p.stringValue))
            {
                Debug.LogWarning($"[Ridgeline] Layer {index} is '{p.stringValue}', expected '{name}'. Leaving it.");
                return false;
            }
            p.stringValue = name;
            return true;
        }
    }
}
