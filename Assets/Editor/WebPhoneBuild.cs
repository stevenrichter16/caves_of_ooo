using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Editor
{
    /// <summary>Run only in a disposable copy created by Tools/WebPort/prepare_project.py.</summary>
    public static class WebPhoneBuild
    {
        [Serializable] private sealed class Provenance { public string source; }
        [Serializable] private sealed class CacheManifest { public string[] assets; }

        public static void Build()
        {
            if (Application.unityVersion != "6000.3.4f1")
                throw new InvalidOperationException("This build is pinned to Unity 6000.3.4f1; found " + Application.unityVersion);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string marker = Path.Combine(project, ".coo-web-project.json");
            if (!File.Exists(marker)) throw new InvalidOperationException("Prepare a disposable Web project first; desktop settings are never changed here.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Install Web Build Support for Unity 6000.3.4f1.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new InvalidOperationException("Launch this copy with -buildTarget WebGL before -executeMethod.");
            var provenance = JsonUtility.FromJson<Provenance>(File.ReadAllText(marker));
            if (provenance == null || string.IsNullOrEmpty(provenance.source)) throw new InvalidDataException("Missing source identity.");
            string destination = Environment.GetEnvironmentVariable("COO_WEB_OUTPUT");
            if (string.IsNullOrEmpty(destination)) destination = Path.Combine(project, "Builds/Phone");
            if (Directory.Exists(destination) && Directory.GetFileSystemEntries(destination).Length > 0)
                throw new InvalidOperationException("Web output must be empty; existing builds are preserved.");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.template = "PROJECT:CavesPhone";
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 32;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.geometricMemoryGrowthStep = 0.2f;
            PlayerSettings.WebGL.memoryGeometricGrowthCap = 96;
            PlayerSettings.runInBackground = false;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Main/SampleScene.unity" }, locationPathName = destination,
                target = BuildTarget.WebGL, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result);
            File.WriteAllText(Path.Combine(destination, "build-info.json"), JsonUtility.ToJson(provenance));
            string worker = Path.Combine(destination, "service-worker.js");
            File.WriteAllText(worker, File.ReadAllText(worker).Replace("__COO_BUILD_ID__", provenance.source));
            File.Copy(marker, Path.Combine(destination, "build-provenance.json"), true);
            var assets = new System.Collections.Generic.List<string>();
            foreach (string directory in new[] { "Build", "StreamingAssets" })
            {
                string root = Path.Combine(destination, directory);
                if (!Directory.Exists(root)) continue;
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    if (!file.EndsWith(".meta", StringComparison.Ordinal))
                        assets.Add(Path.GetRelativePath(destination, file).Replace('\\', '/'));
            }
            assets.Sort(StringComparer.Ordinal);
            File.WriteAllText(Path.Combine(destination, "cache-manifest.json"), JsonUtility.ToJson(new CacheManifest { assets = assets.ToArray() }));
            Debug.Log("[Web] Built " + provenance.source + " at " + destination + ". Phone qualification is still required.");
        }
    }
}
