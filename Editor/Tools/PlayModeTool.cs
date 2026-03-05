using System;
using System.Threading.Tasks;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool to enter Play Mode in the Unity Editor.
    /// Uses EditorApplication.playModeStateChanged to detect completion.
    /// </summary>
    public class EnterPlayModeTool : McpToolBase
    {
        public EnterPlayModeTool()
        {
            Name = "enter_play_mode";
            Description = "Enters Play Mode in the Unity Editor.";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            if (EditorApplication.isPlaying)
            {
                tcs.SetResult(new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Already in Play Mode"
                });
                return;
            }

            Action<PlayModeStateChange> handler = null;
            handler = (PlayModeStateChange state) =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    EditorApplication.playModeStateChanged -= handler;
                    tcs.TrySetResult(new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = "Successfully entered Play Mode"
                    });
                }
            };

            EditorApplication.playModeStateChanged += handler;
            McpLogger.LogInfo("Entering Play Mode");
            EditorApplication.isPlaying = true;
        }
    }

    /// <summary>
    /// Tool to exit Play Mode in the Unity Editor.
    /// Uses EditorApplication.playModeStateChanged to detect completion.
    /// </summary>
    public class ExitPlayModeTool : McpToolBase
    {
        public ExitPlayModeTool()
        {
            Name = "exit_play_mode";
            Description = "Exits Play Mode in the Unity Editor.";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            if (!EditorApplication.isPlaying)
            {
                tcs.SetResult(new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Already in Edit Mode"
                });
                return;
            }

            Action<PlayModeStateChange> handler = null;
            handler = (PlayModeStateChange state) =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.playModeStateChanged -= handler;
                    tcs.TrySetResult(new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = "Successfully exited Play Mode"
                    });
                }
            };

            EditorApplication.playModeStateChanged += handler;
            McpLogger.LogInfo("Exiting Play Mode");
            EditorApplication.isPlaying = false;
        }
    }

    /// <summary>
    /// Tool to pause or unpause the Unity Editor during Play Mode.
    /// Can toggle or set an explicit paused state.
    /// </summary>
    public class PauseEditorTool : McpToolBase
    {
        public PauseEditorTool()
        {
            Name = "pause_editor";
            Description = "Pauses or unpauses the Unity Editor. Can toggle or set an explicit paused state.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Cannot pause: Editor is not in Play Mode",
                    "invalid_state_error"
                );
            }

            bool? paused = parameters["paused"]?.ToObject<bool?>();

            if (paused.HasValue)
            {
                EditorApplication.isPaused = paused.Value;
            }
            else
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = EditorApplication.isPaused
                    ? "Editor is now paused"
                    : "Editor is now unpaused",
                ["isPaused"] = EditorApplication.isPaused
            };
        }
    }

    /// <summary>
    /// Tool to advance a single frame while the Editor is paused in Play Mode.
    /// </summary>
    public class StepFrameTool : McpToolBase
    {
        public StepFrameTool()
        {
            Name = "step_frame";
            Description = "Advances a single frame while the Editor is paused in Play Mode.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Cannot step frame: Editor is not in Play Mode",
                    "invalid_state_error"
                );
            }

            EditorApplication.Step();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Successfully stepped one frame"
            };
        }
    }
}
