using System;
using System.IO;
using System.Linq;
using System.Reflection;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating C# scripts from templates
    /// </summary>
    public class CreateScriptTool : McpToolBase
    {
        public CreateScriptTool()
        {
            Name = "create_script";
            Description = "Creates a new C# script file from a template (MonoBehaviour, ScriptableObject, or plain class)";
        }

        public override JObject Execute(JObject parameters)
        {
            string scriptName = parameters["scriptName"]?.ToObject<string>();
            string namespaceName = parameters["namespaceName"]?.ToObject<string>();
            string folderPath = parameters["folderPath"]?.ToObject<string>() ?? "Assets/Scripts";
            string scriptType = parameters["scriptType"]?.ToObject<string>() ?? "MonoBehaviour";

            if (string.IsNullOrEmpty(scriptName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'scriptName' not provided",
                    "validation_error"
                );
            }

            string template;
            switch (scriptType)
            {
                case "ScriptableObject":
                    template = GenerateScriptableObjectTemplate(scriptName, namespaceName);
                    break;
                case "plain":
                    template = GeneratePlainClassTemplate(scriptName, namespaceName);
                    break;
                default:
                    template = GenerateMonoBehaviourTemplate(scriptName, namespaceName);
                    break;
            }

            // Ensure folder exists
            string absoluteFolderPath = Path.Combine(Application.dataPath.Replace("Assets", ""), folderPath);
            if (!Directory.Exists(absoluteFolderPath))
            {
                Directory.CreateDirectory(absoluteFolderPath);
            }

            string fullPath = Path.Combine(absoluteFolderPath, scriptName + ".cs");

            if (File.Exists(fullPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script file already exists at '{folderPath}/{scriptName}.cs'",
                    "validation_error"
                );
            }

            File.WriteAllText(fullPath, template);
            AssetDatabase.Refresh();

            McpLogger.LogInfo($"[MCP Unity] Created script '{scriptName}' at '{folderPath}/{scriptName}.cs'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully created {scriptType} script '{scriptName}' at '{folderPath}/{scriptName}.cs'",
                ["scriptPath"] = $"{folderPath}/{scriptName}.cs",
                ["scriptType"] = scriptType
            };
        }

        private string GenerateMonoBehaviourTemplate(string className, string namespaceName)
        {
            string body = $@"using UnityEngine;

";
            if (!string.IsNullOrEmpty(namespaceName))
            {
                body += $@"namespace {namespaceName}
{{
    public class {className} : MonoBehaviour
    {{
        void Start()
        {{
        }}

        void Update()
        {{
        }}
    }}
}}
";
            }
            else
            {
                body += $@"public class {className} : MonoBehaviour
{{
    void Start()
    {{
    }}

    void Update()
    {{
    }}
}}
";
            }
            return body;
        }

        private string GenerateScriptableObjectTemplate(string className, string namespaceName)
        {
            string body = $@"using UnityEngine;

";
            if (!string.IsNullOrEmpty(namespaceName))
            {
                body += $@"namespace {namespaceName}
{{
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""{namespaceName}/{className}"")]
    public class {className} : ScriptableObject
    {{
    }}
}}
";
            }
            else
            {
                body += $@"[CreateAssetMenu(fileName = ""{className}"", menuName = ""ScriptableObjects/{className}"")]
public class {className} : ScriptableObject
{{
}}
";
            }
            return body;
        }

        private string GeneratePlainClassTemplate(string className, string namespaceName)
        {
            string body = $@"using System;

";
            if (!string.IsNullOrEmpty(namespaceName))
            {
                body += $@"namespace {namespaceName}
{{
    public class {className}
    {{
    }}
}}
";
            }
            else
            {
                body += $@"public class {className}
{{
}}
";
            }
            return body;
        }
    }

    /// <summary>
    /// Tool for attaching a script component to a GameObject
    /// </summary>
    public class AttachScriptTool : McpToolBase
    {
        public AttachScriptTool()
        {
            Name = "attach_script";
            Description = "Attaches a script component to a GameObject by finding the MonoScript asset";
        }

        public override JObject Execute(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            string scriptName = parameters["scriptName"]?.ToObject<string>();

            if (!instanceId.HasValue && string.IsNullOrEmpty(objectPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'instanceId' or 'objectPath' must be provided",
                    "validation_error"
                );
            }

            if (string.IsNullOrEmpty(scriptName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'scriptName' not provided",
                    "validation_error"
                );
            }

            // Find the GameObject
            GameObject gameObject = null;
            if (instanceId.HasValue)
            {
                gameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
            }
            else if (!string.IsNullOrEmpty(objectPath))
            {
                gameObject = GameObject.Find(objectPath);
            }

            if (gameObject == null)
            {
                string identifier = instanceId.HasValue ? $"ID {instanceId.Value}" : $"path '{objectPath}'";
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject with {identifier} not found",
                    "not_found_error"
                );
            }

            // Find the MonoScript asset
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
            MonoScript script = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    script = ms;
                    break;
                }
            }

            if (script == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script '{scriptName}' not found in the project",
                    "not_found_error"
                );
            }

            Type scriptClass = script.GetClass();
            if (scriptClass == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script '{scriptName}' does not define a valid class (may have compile errors)",
                    "validation_error"
                );
            }

            if (!typeof(Component).IsAssignableFrom(scriptClass))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script '{scriptName}' is not a Component and cannot be attached to a GameObject",
                    "validation_error"
                );
            }

            Undo.AddComponent(gameObject, scriptClass);

            McpLogger.LogInfo($"[MCP Unity] Attached script '{scriptName}' to '{gameObject.name}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully attached script '{scriptName}' to '{gameObject.name}'",
                ["gameObjectName"] = gameObject.name,
                ["scriptName"] = scriptName
            };
        }
    }

    /// <summary>
    /// Tool for getting script information via reflection
    /// </summary>
    public class GetScriptInfoTool : McpToolBase
    {
        public GetScriptInfoTool()
        {
            Name = "get_script_info";
            Description = "Gets information about a script including its serialized fields and public methods";
        }

        public override JObject Execute(JObject parameters)
        {
            string scriptName = parameters["scriptName"]?.ToObject<string>();

            if (string.IsNullOrEmpty(scriptName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'scriptName' not provided",
                    "validation_error"
                );
            }

            // Find the MonoScript asset
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
            MonoScript script = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.name == scriptName)
                {
                    script = ms;
                    break;
                }
            }

            if (script == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script '{scriptName}' not found in the project",
                    "not_found_error"
                );
            }

            Type type = script.GetClass();
            if (type == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Script '{scriptName}' does not define a valid class (may have compile errors)",
                    "validation_error"
                );
            }

            string scriptPath = AssetDatabase.GetAssetPath(script);

            // Get serializable fields (public or [SerializeField])
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Select(f => new JObject
                {
                    ["name"] = f.Name,
                    ["type"] = f.FieldType.Name,
                    ["isPublic"] = f.IsPublic
                });

            // Get public methods (declared only, exclude inherited from Object)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => new JObject
                {
                    ["name"] = m.Name,
                    ["returnType"] = m.ReturnType.Name,
                    ["parameters"] = new JArray(
                        m.GetParameters().Select(p => new JObject
                        {
                            ["name"] = p.Name,
                            ["type"] = p.ParameterType.Name
                        })
                    )
                });

            McpLogger.LogInfo($"[MCP Unity] Retrieved info for script '{scriptName}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Script info for '{scriptName}'",
                ["scriptName"] = scriptName,
                ["scriptPath"] = scriptPath,
                ["className"] = type.FullName,
                ["baseClass"] = type.BaseType?.Name,
                ["fields"] = new JArray(fields),
                ["methods"] = new JArray(methods)
            };
        }
    }
}
