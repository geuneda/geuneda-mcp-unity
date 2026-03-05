using System;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool to perform an undo operation in the Unity Editor.
    /// </summary>
    public class UndoTool : McpToolBase
    {
        public UndoTool()
        {
            Name = "undo";
            Description = "Performs an undo operation in the Unity Editor.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            Undo.PerformUndo();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Undo performed successfully",
                ["currentGroup"] = Undo.GetCurrentGroupName()
            };
        }
    }

    /// <summary>
    /// Tool to perform a redo operation in the Unity Editor.
    /// </summary>
    public class RedoTool : McpToolBase
    {
        public RedoTool()
        {
            Name = "redo";
            Description = "Performs a redo operation in the Unity Editor.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            Undo.PerformRedo();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Redo performed successfully",
                ["currentGroup"] = Undo.GetCurrentGroupName()
            };
        }
    }

    /// <summary>
    /// Tool to get the current undo history state in the Unity Editor.
    /// </summary>
    public class GetUndoHistoryTool : McpToolBase
    {
        public GetUndoHistoryTool()
        {
            Name = "get_undo_history";
            Description = "Gets the current undo history state, including the current group name.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            string currentGroupName = Undo.GetCurrentGroupName();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = string.IsNullOrEmpty(currentGroupName)
                    ? "No undo operations available"
                    : $"Current undo group: {currentGroupName}",
                ["currentGroup"] = currentGroupName
            };
        }
    }
}
