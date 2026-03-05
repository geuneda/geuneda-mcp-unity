using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for performing physics raycasts in the scene
    /// </summary>
    public class PhysicsRaycastTool : McpToolBase
    {
        public PhysicsRaycastTool()
        {
            Name = "physics_raycast";
            Description = "Performs a physics raycast in the scene and returns hit information";
        }

        public override JObject Execute(JObject parameters)
        {
            var originObj = parameters["origin"] as JObject;
            var dirObj = parameters["direction"] as JObject;

            if (originObj == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'origin' not provided",
                    "validation_error"
                );
            }

            if (dirObj == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'direction' not provided",
                    "validation_error"
                );
            }

            Vector3 origin = new Vector3(
                originObj["x"]?.ToObject<float>() ?? 0f,
                originObj["y"]?.ToObject<float>() ?? 0f,
                originObj["z"]?.ToObject<float>() ?? 0f
            );

            Vector3 direction = new Vector3(
                dirObj["x"]?.ToObject<float>() ?? 0f,
                dirObj["y"]?.ToObject<float>() ?? 0f,
                dirObj["z"]?.ToObject<float>() ?? 0f
            ).normalized;

            float maxDistance = parameters["maxDistance"]?.ToObject<float>() ?? Mathf.Infinity;
            int layerMask = parameters["layerMask"]?.ToObject<int>() ?? -1;

            McpLogger.LogInfo($"Executing PhysicsRaycastTool: origin={origin}, direction={direction}, maxDistance={maxDistance}");

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);

            if (didHit)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["hit"] = true,
                    ["message"] = $"Raycast hit: {hit.collider.gameObject.name} at distance {hit.distance:F3}",
                    ["point"] = new JObject { ["x"] = hit.point.x, ["y"] = hit.point.y, ["z"] = hit.point.z },
                    ["normal"] = new JObject { ["x"] = hit.normal.x, ["y"] = hit.normal.y, ["z"] = hit.normal.z },
                    ["distance"] = hit.distance,
                    ["colliderName"] = hit.collider.name,
                    ["gameObjectName"] = hit.collider.gameObject.name,
                    ["gameObjectPath"] = GetGameObjectPath(hit.collider.gameObject)
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["hit"] = false,
                ["message"] = "Raycast did not hit any collider"
            };
        }

        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }

    /// <summary>
    /// Tool for getting current physics settings
    /// </summary>
    public class GetPhysicsSettingsTool : McpToolBase
    {
        public GetPhysicsSettingsTool()
        {
            Name = "get_physics_settings";
            Description = "Gets the current physics settings including gravity, solver iterations, and thresholds";
        }

        public override JObject Execute(JObject parameters)
        {
            McpLogger.LogInfo("Executing GetPhysicsSettingsTool");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Physics settings retrieved",
                ["gravity"] = new JObject
                {
                    ["x"] = Physics.gravity.x,
                    ["y"] = Physics.gravity.y,
                    ["z"] = Physics.gravity.z
                },
                ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                ["bounceThreshold"] = Physics.bounceThreshold,
                ["defaultContactOffset"] = Physics.defaultContactOffset,
                ["sleepThreshold"] = Physics.sleepThreshold,
                ["autoSimulation"] = Physics.simulationMode.ToString()
            };
        }
    }
}
