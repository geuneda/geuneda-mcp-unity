using System;
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
    /// Tool for invoking a public method on a GameObject's Component at runtime.
    /// Useful for triggering button clicks, calling test helpers, and invoking UI event handlers.
    /// </summary>
    public class InvokeMethodTool : McpToolBase
    {
        public InvokeMethodTool()
        {
            Name = "invoke_method";
            Description = "Invokes a public method on a Component attached to a GameObject. Supports passing arguments and returns the method's return value.";
        }

        /// <summary>
        /// Execute the InvokeMethod tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            // Extract parameters
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            string componentType = parameters["componentType"]?.ToObject<string>();
            string methodName = parameters["methodName"]?.ToObject<string>();
            JArray arguments = parameters["arguments"] as JArray;

            // Validate parameters - require either instanceId or objectPath
            if (!instanceId.HasValue && string.IsNullOrEmpty(objectPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'instanceId' or 'objectPath' must be provided",
                    "validation_error"
                );
            }

            if (string.IsNullOrEmpty(componentType))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'componentType' not provided",
                    "validation_error"
                );
            }

            if (string.IsNullOrEmpty(methodName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'methodName' not provided",
                    "validation_error"
                );
            }

            // Find the GameObject by instance ID or path
            GameObject gameObject = null;
            string identifier = "unknown";

            if (instanceId.HasValue)
            {
                gameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                identifier = $"ID {instanceId.Value}";
            }
            else
            {
                gameObject = GameObject.Find(objectPath);
                identifier = $"path '{objectPath}'";

                if (gameObject == null)
                {
                    gameObject = FindGameObjectByPath(objectPath);
                }
            }

            if (gameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject with path '{objectPath}' or instance ID {instanceId} not found",
                    "not_found_error"
                );
            }

            McpLogger.LogInfo($"[MCP Unity] Invoking method '{methodName}' on component '{componentType}' of GameObject '{gameObject.name}' (found by {identifier})");

            // Find the Component by type name
            Component component = FindComponentByTypeName(gameObject, componentType);
            if (component == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Component '{componentType}' not found on GameObject '{gameObject.name}'",
                    "component_error"
                );
            }

            // Find the method using reflection
            Type type = component.GetType();
            MethodInfo method = FindMethod(type, methodName);

            if (method == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Method '{methodName}' not found on component '{componentType}'",
                    "method_error"
                );
            }

            // Prepare arguments
            ParameterInfo[] methodParams = method.GetParameters();
            object[] invokeArgs = PrepareArguments(methodParams, arguments, out string argError);

            if (argError != null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(argError, "argument_error");
            }

            // Invoke the method
            try
            {
                object result = method.Invoke(component, invokeArgs);

                JObject response = new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Successfully invoked '{methodName}' on component '{componentType}' of GameObject '{gameObject.name}'"
                };

                if (method.ReturnType != typeof(void) && result != null)
                {
                    try
                    {
                        response["returnValue"] = JToken.FromObject(result);
                    }
                    catch (Exception)
                    {
                        // If the return value cannot be serialized, convert to string
                        response["returnValue"] = result.ToString();
                    }
                }
                else
                {
                    response["returnValue"] = null;
                }

                return response;
            }
            catch (TargetInvocationException ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Method invocation failed: {ex.InnerException?.Message ?? ex.Message}",
                    "invocation_error"
                );
            }
            catch (Exception ex)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error invoking method: {ex.Message}",
                    "invocation_error"
                );
            }
        }

        /// <summary>
        /// Find a GameObject by its hierarchy path
        /// </summary>
        /// <param name="path">The path to the GameObject (e.g. "Canvas/Panel/Button")</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        private GameObject FindGameObjectByPath(string path)
        {
            string[] pathParts = path.Split('/');
            GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            if (pathParts.Length == 0)
            {
                return null;
            }

            foreach (GameObject rootObj in rootGameObjects)
            {
                if (rootObj.name == pathParts[0])
                {
                    GameObject current = rootObj;

                    for (int i = 1; i < pathParts.Length; i++)
                    {
                        Transform child = current.transform.Find(pathParts[i]);
                        if (child == null)
                        {
                            return null;
                        }

                        current = child.gameObject;
                    }

                    return current;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a Component on a GameObject by its type name (short name or full name)
        /// </summary>
        /// <param name="gameObject">The GameObject to search on</param>
        /// <param name="typeName">The type name to match</param>
        /// <returns>The matching Component, or null if not found</returns>
        private Component FindComponentByTypeName(GameObject gameObject, string typeName)
        {
            Component[] components = gameObject.GetComponents<Component>();
            return components.FirstOrDefault(c =>
                c != null &&
                (c.GetType().Name == typeName || c.GetType().FullName == typeName));
        }

        /// <summary>
        /// Find a method by name on the given type.
        /// Searches public instance methods first, then non-public instance methods.
        /// </summary>
        /// <param name="type">The type to search</param>
        /// <param name="methodName">The method name to find</param>
        /// <returns>The MethodInfo if found, null otherwise</returns>
        private MethodInfo FindMethod(Type type, string methodName)
        {
            // Search public instance methods first
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                return method;
            }

            // Fall back to non-public instance methods
            method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method;
        }

        /// <summary>
        /// Prepare invocation arguments by converting JArray elements to the expected parameter types
        /// </summary>
        /// <param name="methodParams">The method's parameter info array</param>
        /// <param name="arguments">The JArray of arguments from the request</param>
        /// <param name="error">Output error message if argument preparation fails</param>
        /// <returns>The prepared arguments array, or null if an error occurred</returns>
        private object[] PrepareArguments(ParameterInfo[] methodParams, JArray arguments, out string error)
        {
            error = null;

            if (arguments == null || arguments.Count == 0)
            {
                // Check if the method requires parameters that have no defaults
                int requiredParamCount = methodParams.Count(p => !p.HasDefaultValue);
                if (requiredParamCount > 0)
                {
                    error = $"Method requires {requiredParamCount} argument(s) but none were provided";
                    return null;
                }

                // Fill in default values for optional parameters
                if (methodParams.Length > 0)
                {
                    object[] defaultArgs = new object[methodParams.Length];
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        defaultArgs[i] = methodParams[i].DefaultValue;
                    }
                    return defaultArgs;
                }

                return Array.Empty<object>();
            }

            if (arguments.Count > methodParams.Length)
            {
                error = $"Too many arguments provided: expected {methodParams.Length}, got {arguments.Count}";
                return null;
            }

            object[] invokeArgs = new object[methodParams.Length];
            for (int i = 0; i < methodParams.Length; i++)
            {
                if (i < arguments.Count)
                {
                    try
                    {
                        invokeArgs[i] = arguments[i].ToObject(methodParams[i].ParameterType);
                    }
                    catch (Exception ex)
                    {
                        error = $"Failed to convert argument {i} to type '{methodParams[i].ParameterType.Name}': {ex.Message}";
                        return null;
                    }
                }
                else if (methodParams[i].HasDefaultValue)
                {
                    invokeArgs[i] = methodParams[i].DefaultValue;
                }
                else
                {
                    error = $"Missing required argument at index {i} (parameter '{methodParams[i].Name}')";
                    return null;
                }
            }

            return invokeArgs;
        }
    }
}
