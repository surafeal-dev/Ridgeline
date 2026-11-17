using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ridgeline.EditorTools
{
    /// <summary>
    /// Applies the YouTube Playables WebGL settings from PLAN.md (Phase 0 / Phase 6).
    /// Run from the menu (Ridgeline > Apply Playables Settings) or headless with
    /// -executeMethod Ridgeline.EditorTools.PlayablesProjectSetup.Apply
    /// </summary>
    public static class PlayablesProjectSetup
    {
        [MenuItem("Ridgeline/Apply Playables Settings")]
        public static void Apply()
        {
            ProjectLayers.Ensure();
            var webgl = NamedBuildTarget.WebGL;

            // Identity
            PlayerSettings.companyName = "Ridgeline";
            PlayerSettings.productName = "Ridgeline";
            PlayerSettings.bundleVersion = "0.1.0";

            // Template: our trimmed copy of Google's YTGameWrapperTemplate (SDK first, CSP nonce, 0-height safe).
            PlayerSettings.WebGL.template = "PROJECT:RidgelineYT";

            // YouTube compresses after upload; Unity gzip/brotli builds are NOT supported.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;

            // Don't let Unity cache the build in IndexedDB; cloud save is the only allowed storage for progress.
            PlayerSettings.WebGL.dataCaching = false;

            // Smaller code, no diagnostics overlay.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false; // keep simple alphanumeric file names

            // Memory: JS heap must stay < 512 MB (iOS Safari reloads above that).
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 384;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;

            // Background frame updates must keep ticking (FAQ: "Run in Background").
            PlayerSettings.runInBackground = true;

            // Size: strip as much as possible.
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(webgl, Il2CppCodeGeneration.OptimizeSize);

            // No splash: the page shows its own loading bar, and firstFrameReady fires on our boot screen.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[Ridgeline] YouTube Playables WebGL settings applied.");
        }
    }
}
