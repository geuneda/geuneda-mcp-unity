using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using McpUnity.Services;
using McpUnity.Unity;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for clearing all console logs in Unity
    /// </summary>
    public class ClearConsoleLogsTool : McpToolBase
    {
        private readonly IConsoleLogsService _consoleLogsService;

        public ClearConsoleLogsTool(IConsoleLogsService consoleLogsService)
        {
            Name = "clear_console_logs";
            Description = "Clears all console logs in Unity Editor";
            _consoleLogsService = consoleLogsService;
        }

        /// <summary>
        /// Execute the ClearConsoleLogs tool
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            try
            {
                // Clear the Unity Console window using internal LogEntries API
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType != null)
                {
                    var clearMethod = logEntriesType.GetMethod("Clear",
                        BindingFlags.Public | BindingFlags.Static);
                    clearMethod?.Invoke(null, null);
                }

                // Clear the stored MCP logs
                _consoleLogsService.ClearLogs();

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Console logs cleared successfully"
                };
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to clear console logs: {ex.Message}",
                    "clear_logs_error"
                );
            }
        }
    }
}
