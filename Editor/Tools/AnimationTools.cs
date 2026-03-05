using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Utility class for Animation tool operations
    /// </summary>
    internal static class AnimationToolUtils
    {
        /// <summary>
        /// Result of finding a GameObject
        /// </summary>
        public struct FindResult
        {
            public GameObject GameObject;
            public JObject Error;
        }

        /// <summary>
        /// Find a GameObject by instanceId or objectPath from parameters
        /// </summary>
        public static FindResult FindGameObject(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();

            GameObject gameObject = null;
            string identifierInfo = "";

            if (instanceId.HasValue)
            {
                gameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                identifierInfo = $"instance ID {instanceId.Value}";
            }
            else if (!string.IsNullOrEmpty(objectPath))
            {
                gameObject = GameObject.Find(objectPath);
                identifierInfo = $"path '{objectPath}'";
            }
            else
            {
                return new FindResult
                {
                    Error = McpUnitySocketHandler.CreateErrorResponse(
                        "Either 'instanceId' or 'objectPath' must be provided",
                        "validation_error"
                    )
                };
            }

            if (gameObject == null)
            {
                return new FindResult
                {
                    Error = McpUnitySocketHandler.CreateErrorResponse(
                        $"GameObject not found with {identifierInfo}",
                        "not_found_error"
                    )
                };
            }

            return new FindResult { GameObject = gameObject };
        }
    }

    /// <summary>
    /// Tool for getting Animator component information from a GameObject
    /// </summary>
    public class GetAnimatorInfoTool : McpToolBase
    {
        public GetAnimatorInfoTool()
        {
            Name = "get_animator_info";
            Description = "Gets detailed information about an Animator component including parameters, layers, and states.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            var findResult = AnimationToolUtils.FindGameObject(parameters);
            if (findResult.Error != null)
                return findResult.Error;

            GameObject go = findResult.GameObject;
            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject '{go.name}' does not have an Animator component",
                    "component_error"
                );
            }

            var controller = animator.runtimeAnimatorController;
            var animController = controller as AnimatorController;

            // Parameters
            var parametersArray = new JArray();
            if (animController != null)
            {
                foreach (var param in animController.parameters)
                {
                    parametersArray.Add(new JObject
                    {
                        ["name"] = param.name,
                        ["type"] = param.type.ToString(),
                        ["defaultFloat"] = param.defaultFloat,
                        ["defaultInt"] = param.defaultInt,
                        ["defaultBool"] = param.defaultBool
                    });
                }
            }

            // Layers and States
            var layers = new JArray();
            if (animController != null)
            {
                foreach (var layer in animController.layers)
                {
                    var states = new JArray();
                    foreach (var childState in layer.stateMachine.states)
                    {
                        states.Add(new JObject
                        {
                            ["name"] = childState.state.name,
                            ["speed"] = childState.state.speed,
                            ["motion"] = childState.state.motion?.name ?? "None"
                        });
                    }
                    layers.Add(new JObject
                    {
                        ["name"] = layer.name,
                        ["defaultWeight"] = layer.defaultWeight,
                        ["states"] = states
                    });
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Animator info for '{go.name}'",
                ["parameters"] = parametersArray,
                ["layers"] = layers,
                ["controllerName"] = controller?.name ?? "None"
            };
        }
    }

    /// <summary>
    /// Tool for setting Animator parameters on a GameObject
    /// </summary>
    public class SetAnimatorParameterTool : McpToolBase
    {
        public SetAnimatorParameterTool()
        {
            Name = "set_animator_parameter";
            Description = "Sets an Animator parameter value. Supports Float, Int, Bool, and Trigger types with auto-detection.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            var findResult = AnimationToolUtils.FindGameObject(parameters);
            if (findResult.Error != null)
                return findResult.Error;

            GameObject go = findResult.GameObject;
            var animator = go.GetComponent<Animator>();
            if (animator == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject '{go.name}' does not have an Animator component",
                    "component_error"
                );
            }

            string parameterName = parameters["parameterName"]?.ToObject<string>();
            if (string.IsNullOrEmpty(parameterName))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'parameterName' not provided",
                    "validation_error"
                );
            }

            JToken valueToken = parameters["value"];
            string parameterTypeStr = parameters["parameterType"]?.ToObject<string>();

            // Auto-detect parameter type from AnimatorController
            AnimatorControllerParameterType paramType;
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller != null && string.IsNullOrEmpty(parameterTypeStr))
            {
                bool found = false;
                foreach (var param in controller.parameters)
                {
                    if (param.name == parameterName)
                    {
                        paramType = param.type;
                        found = true;
                        return ApplyParameter(animator, parameterName, paramType, valueToken, go.name);
                    }
                }
                if (!found)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Parameter '{parameterName}' not found in Animator controller",
                        "not_found_error"
                    );
                }
            }

            // Use explicit parameterType if provided
            if (!string.IsNullOrEmpty(parameterTypeStr))
            {
                if (!Enum.TryParse<AnimatorControllerParameterType>(parameterTypeStr, true, out paramType))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Invalid parameter type '{parameterTypeStr}'. Must be Float, Int, Bool, or Trigger",
                        "validation_error"
                    );
                }
                return ApplyParameter(animator, parameterName, paramType, valueToken, go.name);
            }

            return McpUnitySocketHandler.CreateErrorResponse(
                "Could not determine parameter type. Provide 'parameterType' or ensure the Animator has an AnimatorController",
                "validation_error"
            );
        }

        private JObject ApplyParameter(Animator animator, string parameterName, AnimatorControllerParameterType paramType, JToken valueToken, string goName)
        {
            Undo.RecordObject(animator, $"Set Animator Parameter {parameterName}");

            switch (paramType)
            {
                case AnimatorControllerParameterType.Float:
                    float floatVal = valueToken?.ToObject<float>() ?? 0f;
                    animator.SetFloat(parameterName, floatVal);
                    break;
                case AnimatorControllerParameterType.Int:
                    int intVal = valueToken?.ToObject<int>() ?? 0;
                    animator.SetInteger(parameterName, intVal);
                    break;
                case AnimatorControllerParameterType.Bool:
                    bool boolVal = valueToken?.ToObject<bool>() ?? false;
                    animator.SetBool(parameterName, boolVal);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(parameterName);
                    break;
            }

            EditorUtility.SetDirty(animator);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Set parameter '{parameterName}' ({paramType}) on '{goName}'"
            };
        }
    }
}
