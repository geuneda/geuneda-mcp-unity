using System.Collections.Generic;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for searching assets in the project
    /// </summary>
    public class SearchAssetsTool : McpToolBase
    {
        public SearchAssetsTool()
        {
            Name = "search_assets";
            Description = "Searches for assets in the project using filters like type, labels, and folder";
        }

        public override JObject Execute(JObject parameters)
        {
            string searchQuery = parameters["searchQuery"]?.ToObject<string>();
            string type = parameters["type"]?.ToObject<string>();
            JArray labelsArray = parameters["labels"] as JArray;
            string folder = parameters["folder"]?.ToObject<string>();
            int maxResults = parameters["maxResults"]?.ToObject<int?>() ?? 100;

            // Build the search filter
            string filter = "";
            if (!string.IsNullOrEmpty(type))
            {
                filter += $"t:{type} ";
            }
            if (labelsArray != null)
            {
                foreach (JToken label in labelsArray)
                {
                    string labelStr = label.ToObject<string>();
                    if (!string.IsNullOrEmpty(labelStr))
                    {
                        filter += $"l:{labelStr} ";
                    }
                }
            }
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filter += searchQuery;
            }

            filter = filter.Trim();

            string[] guids;
            if (!string.IsNullOrEmpty(folder))
            {
                guids = AssetDatabase.FindAssets(filter, new[] { folder });
            }
            else
            {
                guids = AssetDatabase.FindAssets(filter);
            }

            // Convert GUIDs to asset info (limit by maxResults)
            JArray results = new JArray();
            int count = 0;
            foreach (string guid in guids)
            {
                if (count >= maxResults)
                    break;

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                results.Add(new JObject
                {
                    ["name"] = assetName,
                    ["path"] = assetPath,
                    ["type"] = assetType?.Name ?? "Unknown",
                    ["guid"] = guid
                });

                count++;
            }

            McpLogger.LogInfo($"[MCP Unity] Search assets: found {results.Count} results for filter '{filter}'");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} asset(s) matching the search criteria",
                ["totalFound"] = guids.Length,
                ["returnedCount"] = results.Count,
                ["assets"] = results
            };
        }
    }

    /// <summary>
    /// Tool for getting asset dependencies
    /// </summary>
    public class GetAssetDependenciesTool : McpToolBase
    {
        public GetAssetDependenciesTool()
        {
            Name = "get_asset_dependencies";
            Description = "Gets the dependencies of an asset at the specified path";
        }

        public override JObject Execute(JObject parameters)
        {
            string assetPath = parameters["assetPath"]?.ToObject<string>();
            bool recursive = parameters["recursive"]?.ToObject<bool?>() ?? true;

            if (string.IsNullOrEmpty(assetPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'assetPath' not provided",
                    "validation_error"
                );
            }

            // Verify the asset exists
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Asset not found at path '{assetPath}'",
                    "not_found_error"
                );
            }

            string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive);

            JArray depArray = new JArray();
            foreach (string depPath in dependencies)
            {
                // Skip self
                if (depPath == assetPath)
                    continue;

                System.Type depType = AssetDatabase.GetMainAssetTypeAtPath(depPath);
                depArray.Add(new JObject
                {
                    ["path"] = depPath,
                    ["type"] = depType?.Name ?? "Unknown",
                    ["name"] = System.IO.Path.GetFileNameWithoutExtension(depPath)
                });
            }

            McpLogger.LogInfo($"[MCP Unity] Get dependencies for '{assetPath}': found {depArray.Count} dependencies");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Asset '{assetPath}' has {depArray.Count} dependency(ies)",
                ["assetPath"] = assetPath,
                ["recursive"] = recursive,
                ["dependencyCount"] = depArray.Count,
                ["dependencies"] = depArray
            };
        }
    }

    /// <summary>
    /// Tool for reimporting an asset
    /// </summary>
    public class ReimportAssetTool : McpToolBase
    {
        public ReimportAssetTool()
        {
            Name = "reimport_asset";
            Description = "Reimports an asset at the specified path, optionally forcing a full reimport";
        }

        public override JObject Execute(JObject parameters)
        {
            string assetPath = parameters["assetPath"]?.ToObject<string>();
            bool forceUpdate = parameters["forceUpdate"]?.ToObject<bool?>() ?? false;

            if (string.IsNullOrEmpty(assetPath))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'assetPath' not provided",
                    "validation_error"
                );
            }

            // Verify the asset exists
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Asset not found at path '{assetPath}'",
                    "not_found_error"
                );
            }

            ImportAssetOptions options = forceUpdate
                ? ImportAssetOptions.ForceUpdate
                : ImportAssetOptions.Default;

            AssetDatabase.ImportAsset(assetPath, options);

            McpLogger.LogInfo($"[MCP Unity] Reimported asset '{assetPath}' (forceUpdate: {forceUpdate})");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Successfully reimported asset '{assetPath}'",
                ["assetPath"] = assetPath,
                ["forceUpdate"] = forceUpdate
            };
        }
    }
}
