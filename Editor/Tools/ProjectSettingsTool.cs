using System;
using System.Collections.Generic;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for getting Unity project settings by category
    /// </summary>
    public class GetProjectSettingsTool : McpToolBase
    {
        public GetProjectSettingsTool()
        {
            Name = "get_project_settings";
            Description = "Gets Unity project settings for a specific category (player, quality, physics, time, build)";
        }

        public override JObject Execute(JObject parameters)
        {
            string category = parameters?["category"]?.ToObject<string>();

            if (string.IsNullOrEmpty(category))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'category' not provided",
                    "validation_error"
                );
            }

            McpLogger.LogInfo($"Executing GetProjectSettingsTool: category={category}");

            JObject settings;

            switch (category.ToLowerInvariant())
            {
                case "player":
                    settings = GetPlayerSettings();
                    break;
                case "quality":
                    settings = GetQualitySettings();
                    break;
                case "physics":
                    settings = GetPhysicsSettings();
                    break;
                case "time":
                    settings = GetTimeSettings();
                    break;
                case "build":
                    settings = GetBuildSettings();
                    break;
                default:
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Unknown category '{category}'. Supported: player, quality, physics, time, build",
                        "validation_error"
                    );
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Retrieved {category} settings",
                ["category"] = category,
                ["settings"] = settings
            };
        }

        private JObject GetPlayerSettings()
        {
            return new JObject
            {
                ["companyName"] = PlayerSettings.companyName,
                ["productName"] = PlayerSettings.productName,
                ["bundleVersion"] = PlayerSettings.bundleVersion,
                ["applicationIdentifier"] = PlayerSettings.applicationIdentifier,
                ["defaultIsFullScreen"] = PlayerSettings.defaultIsFullScreen,
                ["runInBackground"] = PlayerSettings.runInBackground,
                ["colorSpace"] = PlayerSettings.colorSpace.ToString(),
                ["apiCompatibilityLevel"] = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
                ["scriptingBackend"] = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString()
            };
        }

        private JObject GetQualitySettings()
        {
            var names = QualitySettings.names;
            var namesArray = new JArray();
            foreach (string name in names)
            {
                namesArray.Add(name);
            }

            return new JObject
            {
                ["qualityLevelNames"] = namesArray,
                ["currentQualityLevel"] = QualitySettings.GetQualityLevel(),
                ["currentQualityName"] = names.Length > QualitySettings.GetQualityLevel() ? names[QualitySettings.GetQualityLevel()] : "Unknown",
                ["vSyncCount"] = QualitySettings.vSyncCount,
                ["antiAliasing"] = QualitySettings.antiAliasing,
                ["shadowResolution"] = QualitySettings.shadowResolution.ToString(),
                ["shadowDistance"] = QualitySettings.shadowDistance,
                ["anisotropicFiltering"] = QualitySettings.anisotropicFiltering.ToString(),
                ["pixelLightCount"] = QualitySettings.pixelLightCount
            };
        }

        private JObject GetPhysicsSettings()
        {
            var gravity = Physics.gravity;
            return new JObject
            {
                ["gravity"] = new JObject
                {
                    ["x"] = gravity.x,
                    ["y"] = gravity.y,
                    ["z"] = gravity.z
                },
                ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                ["bounceThreshold"] = Physics.bounceThreshold,
                ["defaultContactOffset"] = Physics.defaultContactOffset,
                ["sleepThreshold"] = Physics.sleepThreshold,
                ["autoSimulation"] = Physics.simulationMode.ToString()
            };
        }

        private JObject GetTimeSettings()
        {
            return new JObject
            {
                ["fixedDeltaTime"] = Time.fixedDeltaTime,
                ["timeScale"] = Time.timeScale,
                ["maximumDeltaTime"] = Time.maximumDeltaTime,
                ["maximumParticleDeltaTime"] = Time.maximumParticleDeltaTime,
                ["captureDeltaTime"] = Time.captureDeltaTime
            };
        }

        private JObject GetBuildSettings()
        {
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

            return new JObject
            {
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["selectedBuildTargetGroup"] = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                ["scenes"] = scenesArray
            };
        }
    }

    /// <summary>
    /// Tool for modifying Unity project settings
    /// </summary>
    public class SetProjectSettingsTool : McpToolBase
    {
        public SetProjectSettingsTool()
        {
            Name = "set_project_settings";
            Description = "Modifies Unity project settings for a specific category (player, quality, physics, time)";
        }

        public override JObject Execute(JObject parameters)
        {
            string category = parameters?["category"]?.ToObject<string>();
            JObject settings = parameters?["settings"] as JObject;

            if (string.IsNullOrEmpty(category))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'category' not provided",
                    "validation_error"
                );
            }

            if (settings == null || settings.Count == 0)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'settings' not provided or empty",
                    "validation_error"
                );
            }

            McpLogger.LogInfo($"Executing SetProjectSettingsTool: category={category}");

            List<string> applied = new List<string>();
            List<string> warnings = new List<string>();

            switch (category.ToLowerInvariant())
            {
                case "player":
                    ApplyPlayerSettings(settings, applied, warnings);
                    break;
                case "quality":
                    ApplyQualitySettings(settings, applied, warnings);
                    break;
                case "physics":
                    ApplyPhysicsSettings(settings, applied, warnings);
                    break;
                case "time":
                    ApplyTimeSettings(settings, applied, warnings);
                    break;
                default:
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Unknown or read-only category '{category}'. Writable: player, quality, physics, time",
                        "validation_error"
                    );
            }

            string message = $"Applied {applied.Count} setting(s) to {category}";
            if (warnings.Count > 0)
            {
                message += $". Warnings: {string.Join(", ", warnings)}";
            }

            McpLogger.LogInfo(message);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = message,
                ["category"] = category,
                ["appliedSettings"] = new JArray(applied),
                ["warnings"] = new JArray(warnings)
            };
        }

        private void ApplyPlayerSettings(JObject settings, List<string> applied, List<string> warnings)
        {
            foreach (var prop in settings.Properties())
            {
                switch (prop.Name)
                {
                    case "companyName":
                        PlayerSettings.companyName = prop.Value.ToObject<string>();
                        applied.Add(prop.Name);
                        break;
                    case "productName":
                        PlayerSettings.productName = prop.Value.ToObject<string>();
                        applied.Add(prop.Name);
                        break;
                    case "bundleVersion":
                        PlayerSettings.bundleVersion = prop.Value.ToObject<string>();
                        applied.Add(prop.Name);
                        break;
                    case "runInBackground":
                        PlayerSettings.runInBackground = prop.Value.ToObject<bool>();
                        applied.Add(prop.Name);
                        break;
                    case "defaultIsFullScreen":
                        PlayerSettings.defaultIsFullScreen = prop.Value.ToObject<bool>();
                        applied.Add(prop.Name);
                        break;
                    default:
                        warnings.Add($"Unknown or unsupported player setting: '{prop.Name}'");
                        break;
                }
            }
        }

        private void ApplyQualitySettings(JObject settings, List<string> applied, List<string> warnings)
        {
            foreach (var prop in settings.Properties())
            {
                switch (prop.Name)
                {
                    case "qualityLevel":
                        QualitySettings.SetQualityLevel(prop.Value.ToObject<int>());
                        applied.Add(prop.Name);
                        break;
                    case "vSyncCount":
                        QualitySettings.vSyncCount = prop.Value.ToObject<int>();
                        applied.Add(prop.Name);
                        break;
                    case "antiAliasing":
                        QualitySettings.antiAliasing = prop.Value.ToObject<int>();
                        applied.Add(prop.Name);
                        break;
                    case "shadowDistance":
                        QualitySettings.shadowDistance = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "pixelLightCount":
                        QualitySettings.pixelLightCount = prop.Value.ToObject<int>();
                        applied.Add(prop.Name);
                        break;
                    default:
                        warnings.Add($"Unknown or unsupported quality setting: '{prop.Name}'");
                        break;
                }
            }
        }

        private void ApplyPhysicsSettings(JObject settings, List<string> applied, List<string> warnings)
        {
            foreach (var prop in settings.Properties())
            {
                switch (prop.Name)
                {
                    case "gravity":
                        var g = prop.Value as JObject;
                        if (g != null)
                        {
                            Physics.gravity = new Vector3(
                                g["x"]?.ToObject<float>() ?? 0f,
                                g["y"]?.ToObject<float>() ?? -9.81f,
                                g["z"]?.ToObject<float>() ?? 0f
                            );
                            applied.Add(prop.Name);
                        }
                        break;
                    case "defaultSolverIterations":
                        Physics.defaultSolverIterations = prop.Value.ToObject<int>();
                        applied.Add(prop.Name);
                        break;
                    case "defaultSolverVelocityIterations":
                        Physics.defaultSolverVelocityIterations = prop.Value.ToObject<int>();
                        applied.Add(prop.Name);
                        break;
                    case "bounceThreshold":
                        Physics.bounceThreshold = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "defaultContactOffset":
                        Physics.defaultContactOffset = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "sleepThreshold":
                        Physics.sleepThreshold = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    default:
                        warnings.Add($"Unknown or unsupported physics setting: '{prop.Name}'");
                        break;
                }
            }
        }

        private void ApplyTimeSettings(JObject settings, List<string> applied, List<string> warnings)
        {
            foreach (var prop in settings.Properties())
            {
                switch (prop.Name)
                {
                    case "fixedDeltaTime":
                        Time.fixedDeltaTime = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "timeScale":
                        Time.timeScale = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "maximumDeltaTime":
                        Time.maximumDeltaTime = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    case "captureDeltaTime":
                        Time.captureDeltaTime = prop.Value.ToObject<float>();
                        applied.Add(prop.Name);
                        break;
                    default:
                        warnings.Add($"Unknown or unsupported time setting: '{prop.Name}'");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Tool for getting build scenes from EditorBuildSettings
    /// </summary>
    public class GetBuildScenesTool : McpToolBase
    {
        public GetBuildScenesTool()
        {
            Name = "get_build_scenes";
            Description = "Gets the list of scenes in the Build Settings";
        }

        public override JObject Execute(JObject parameters)
        {
            McpLogger.LogInfo("Executing GetBuildScenesTool");

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

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {scenes.Length} scene(s) in Build Settings",
                ["scenes"] = scenesArray
            };
        }
    }

    /// <summary>
    /// Tool for setting build scenes in EditorBuildSettings
    /// </summary>
    public class SetBuildScenesTool : McpToolBase
    {
        public SetBuildScenesTool()
        {
            Name = "set_build_scenes";
            Description = "Sets the list of scenes in the Build Settings";
        }

        public override JObject Execute(JObject parameters)
        {
            var scenesParam = parameters?["scenes"] as JArray;

            if (scenesParam == null || scenesParam.Count == 0)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'scenes' not provided or empty",
                    "validation_error"
                );
            }

            McpLogger.LogInfo($"Executing SetBuildScenesTool: {scenesParam.Count} scene(s)");

            var buildScenes = new List<EditorBuildSettingsScene>();

            foreach (JObject sceneObj in scenesParam)
            {
                string path = sceneObj["path"]?.ToObject<string>();
                bool enabled = sceneObj["enabled"]?.ToObject<bool>() ?? true;

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                buildScenes.Add(new EditorBuildSettingsScene(path, enabled));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();

            McpLogger.LogInfo($"Set {buildScenes.Count} scene(s) in Build Settings");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully set {buildScenes.Count} scene(s) in Build Settings",
                ["sceneCount"] = buildScenes.Count
            };
        }
    }
}
