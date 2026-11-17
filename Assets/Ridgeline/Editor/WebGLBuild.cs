using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ridgeline.EditorTools
{
    /// <summary>
    /// Builds the YouTube Playables WebGL bundle to Builds/WebGL and checks it against the
    /// certification limits (file names, per-file size, total size, file count).
    /// Menu: Ridgeline > Build WebGL, or headless via -executeMethod Ridgeline.EditorTools.WebGLBuild.Build
    /// </summary>
    public static class WebGLBuild
    {
        const string OutDir = "Builds/WebGL";
        const long MiB = 1024 * 1024;

        [MenuItem("Ridgeline/Build WebGL")]
        public static void Build()
        {
            PlayablesProjectSetup.Apply();
            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Ridgeline] WebGL build failed: {report.summary.result}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Check();
        }

        [MenuItem("Ridgeline/Check WebGL Bundle")]
        public static void Check()
        {
            var files = Directory.GetFiles(OutDir, "*", SearchOption.AllDirectories);
            long total = 0;
            bool ok = true;
            var nameRule = new Regex("^[A-Za-z0-9_.-]+$");
            foreach (var f in files.OrderByDescending(f => new FileInfo(f).Length))
            {
                long len = new FileInfo(f).Length;
                total += len;
                string rel = f.Substring(OutDir.Length + 1).Replace('\\', '/');
                string flag = len >= 30 * MiB ? "  <-- OVER 30 MiB HARD LIMIT" : (len > 512 * 1024 ? "  (over 512 KiB recommendation)" : "");
                if (len >= 30 * MiB) ok = false;
                foreach (var part in rel.Split('/'))
                    if (!nameRule.IsMatch(part)) { flag += "  <-- BAD FILE NAME"; ok = false; }
                Debug.Log($"[Ridgeline] {len / 1024.0,10:F0} KiB  {rel}{flag}");
            }
            if (files.Length > 8000) ok = false;
            if (total >= 250 * MiB) ok = false;
            Debug.Log($"[Ridgeline] Bundle: {files.Length} files, {total / (double)MiB:F2} MiB uncompressed " +
                      $"(YouTube measures the initial bundle after its own HTTP compression; limit 30 MiB, target 15 MiB). " +
                      (ok ? "Static checks PASSED." : "Static checks FAILED."));
        }
    }
}
