using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DodgeRunner.EditorTools
{
    // CLI から WebGL / macOS をビルドする。出力先は -buildOutput（unity build -o）で受け取る。
    public static class Builder
    {
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Build/Mac/DodgeRunner.app");

        static void Build(BuildTarget target, string defaultOutput)
        {
            var output = ReadArg("-buildOutput") ?? defaultOutput;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in build settings. Run SceneBuilder.Build first.");

            PlayerSettings.productName = "Fable 5.1 Dodge Runner";
            var isWebGL = target == BuildTarget.WebGL;
            if (isWebGL)
            {
                // GitHub Pages は .gz に Content-Encoding を付けないため、JS 側で解凍する fallback を有効にする
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"{{\"event\":\"build_done\",\"target\":\"{target}\",\"result\":\"{summary.result}\",\"size\":{summary.totalSize},\"output\":\"{output}\"}}");
            var failed = summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded;
            if (failed) throw new Exception($"Build failed: {summary.result}");
        }

        static string ReadArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }
}
