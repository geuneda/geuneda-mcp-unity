using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for building the Unity project
    /// </summary>
    public class BuildProjectTool : McpToolBase
    {
        public BuildProjectTool()
        {
            Name = "build_project";
            Description = "Builds the Unity project for a specified target platform";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            string targetStr = parameters["target"]?.ToObject<string>();
            string outputPath = parameters["outputPath"]?.ToObject<string>();
            bool developmentBuild = parameters["developmentBuild"]?.ToObject<bool>() ?? false;
            var scenesToken = parameters["scenes"];

            if (string.IsNullOrEmpty(targetStr))
            {
                tcs.TrySetResult(McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'target' not provided",
                    "validation_error"
                ));
                return;
            }

            BuildTarget target;
            if (!TryParseBuildTarget(targetStr, out target))
            {
                tcs.TrySetResult(McpUnitySocketHandler.CreateErrorResponse(
                    $"Invalid build target: {targetStr}. Supported: StandaloneWindows64, StandaloneOSX, Android, iOS, WebGL",
                    "validation_error"
                ));
                return;
            }

            // Default output path
            if (string.IsNullOrEmpty(outputPath))
            {
                string ext = GetBuildExtension(target);
                outputPath = $"Builds/{targetStr}/Build{ext}";
            }

            // Ensure output directory exists
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Get scenes
            string[] buildScenes;
            if (scenesToken != null && scenesToken.Type == JTokenType.Array)
            {
                buildScenes = scenesToken.ToObject<string[]>();
            }
            else
            {
                buildScenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
            }

            McpLogger.LogInfo($"Building project: target={targetStr}, outputPath={outputPath}, development={developmentBuild}");

            var options = new BuildPlayerOptions
            {
                scenes = buildScenes,
                locationPathName = outputPath,
                target = target,
                options = developmentBuild ? BuildOptions.Development : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            var errors = new JArray();
            var warnings = new JArray();
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        errors.Add(msg.content);
                    else if (msg.type == LogType.Warning)
                        warnings.Add(msg.content);
                }
            }

            bool success = report.summary.result == BuildResult.Succeeded;

            tcs.TrySetResult(new JObject
            {
                ["success"] = success,
                ["type"] = "text",
                ["message"] = success
                    ? $"Build succeeded: {outputPath} (duration: {report.summary.totalTime})"
                    : $"Build failed with {report.summary.totalErrors} error(s)",
                ["buildResult"] = report.summary.result.ToString(),
                ["outputPath"] = outputPath,
                ["totalErrors"] = report.summary.totalErrors,
                ["totalWarnings"] = report.summary.totalWarnings,
                ["errors"] = errors,
                ["warnings"] = warnings,
                ["duration"] = report.summary.totalTime.TotalSeconds
            });
        }

        private static bool TryParseBuildTarget(string str, out BuildTarget target)
        {
            switch (str)
            {
                case "StandaloneWindows64": target = BuildTarget.StandaloneWindows64; return true;
                case "StandaloneOSX": target = BuildTarget.StandaloneOSX; return true;
                case "Android": target = BuildTarget.Android; return true;
                case "iOS": target = BuildTarget.iOS; return true;
                case "WebGL": target = BuildTarget.WebGL; return true;
                default: target = default; return false;
            }
        }

        private static string GetBuildExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64: return ".exe";
                case BuildTarget.StandaloneOSX: return ".app";
                case BuildTarget.Android: return ".apk";
                default: return "";
            }
        }
    }

    /// <summary>
    /// Tool for getting current build settings information
    /// </summary>
    public class GetBuildSettingsTool : McpToolBase
    {
        public GetBuildSettingsTool()
        {
            Name = "get_build_settings";
            Description = "Gets the current build settings including target platform, scenes, and configuration";
        }

        public override JObject Execute(JObject parameters)
        {
            McpLogger.LogInfo("Executing GetBuildSettingsTool");

            var scenes = EditorBuildSettings.scenes;
            var scenesArray = new JArray();
            foreach (var scene in scenes)
            {
                scenesArray.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["guid"] = scene.guid.ToString()
                });
            }

            var scriptingBackend = PlayerSettings.GetScriptingBackend(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Build settings retrieved",
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["selectedBuildTargetGroup"] = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                ["scenes"] = scenesArray,
                ["development"] = EditorUserBuildSettings.development,
                ["scriptingBackend"] = scriptingBackend.ToString(),
                ["il2CppCompilerConfiguration"] = PlayerSettings.GetIl2CppCompilerConfiguration(
                    EditorUserBuildSettings.selectedBuildTargetGroup
                ).ToString()
            };
        }
    }
}
