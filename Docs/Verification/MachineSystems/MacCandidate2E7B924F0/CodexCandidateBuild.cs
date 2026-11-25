using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class CodexCandidateBuild
{
    [Serializable] public class Message { public string type; public string content; }
    [Serializable] public class Report
    {
        public string project, commit, result, output;
        public int errors, warnings;
        public long bytes;
        public double seconds;
        public Message[] messages;
    }
    public static void Run()
    {
        const string expected = "/Users/steven/coo-verification/mac-2e7b924f0-20261125/Assets";
        const string output = "/Users/steven/coo-verification/builds/mac-2e7b924f0-20261125/CavesOfOoo.app";
        const string receipt = "/private/tmp/coo-mac-2e7b924f0-20261125-export/build-report.json";
        if (Application.dataPath != expected || !Application.isBatchMode || Directory.Exists(output))
            throw new InvalidOperationException("Refusing wrong build project, mode or existing package");
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Main/SampleScene.unity" },
            target = BuildTarget.StandaloneOSX,
            locationPathName = output,
            options = BuildOptions.DetailedBuildReport
        });
        var summary = result.summary;
        var report = new Report {
            project = Application.dataPath,
            commit = "2e7b924f078f07426ccdd8d9c3e30f8a471c21fd",
            result = summary.result.ToString(), output = output,
            errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
            bytes = (long)summary.totalSize, seconds = summary.totalTime.TotalSeconds,
            messages = result.steps.SelectMany(s => s.messages).Select(m => new Message { type = m.type.ToString(), content = m.content }).ToArray()
        };
        File.WriteAllText(receipt, JsonUtility.ToJson(report, true));
        EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 2);
    }
}
