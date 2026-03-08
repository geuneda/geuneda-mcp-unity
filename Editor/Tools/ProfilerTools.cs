using System;
using System.Collections.Generic;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    internal static class ProfilerHelpers
    {
        internal static double BytesToMB(long bytes)
        {
            return Math.Round(bytes / (1024.0 * 1024.0), 2);
        }
    }

    internal struct ProfilerSampleData
    {
        public string Name;
        public float TotalMs;
        public float SelfMs;
        public int Calls;
        public float GcAllocBytes;
    }

    /// <summary>
    /// Tool for capturing a memory snapshot from the Unity Profiler.
    /// Returns total allocated, reserved, unused, Mono, and graphics driver memory.
    /// When detailed mode is enabled, includes per-asset-type memory breakdown.
    /// </summary>
    public class GetMemorySnapshotTool : McpToolBase
    {
        public GetMemorySnapshotTool()
        {
            Name = "get_memory_snapshot";
            Description = "Captures a memory snapshot from the Unity Profiler including total allocated, reserved, unused, Mono heap, and graphics driver memory. Optionally includes per-asset-type breakdown.";
        }

        public override JObject Execute(JObject parameters)
        {
            try
            {
                bool detailed = parameters?["detailed"]?.ToObject<bool>() ?? false;

                McpLogger.LogInfo($"Executing GetMemorySnapshotTool: detailed={detailed}");

                long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
                long totalReserved = Profiler.GetTotalReservedMemoryLong();
                long totalUnused = Profiler.GetTotalUnusedReservedMemoryLong();
                long monoUsed = Profiler.GetMonoUsedSizeLong();
                long monoHeap = Profiler.GetMonoHeapSizeLong();
                long graphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();

                var result = new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Memory snapshot captured",
                    ["totalAllocatedMB"] = ProfilerHelpers.BytesToMB(totalAllocated),
                    ["totalReservedMB"] = ProfilerHelpers.BytesToMB(totalReserved),
                    ["totalUnusedMB"] = ProfilerHelpers.BytesToMB(totalUnused),
                    ["monoUsedMB"] = ProfilerHelpers.BytesToMB(monoUsed),
                    ["monoHeapMB"] = ProfilerHelpers.BytesToMB(monoHeap),
                    ["graphicsDriverMB"] = ProfilerHelpers.BytesToMB(graphicsDriver)
                };

                if (detailed)
                {
                    var assetBreakdown = new JObject();
                    assetBreakdown["textures"] = GetAssetMemoryBreakdown<Texture2D>("Texture2D");
                    assetBreakdown["meshes"] = GetAssetMemoryBreakdown<Mesh>("Mesh");
                    assetBreakdown["materials"] = GetAssetMemoryBreakdown<Material>("Material");
                    assetBreakdown["audioClips"] = GetAssetMemoryBreakdown<AudioClip>("AudioClip");
                    assetBreakdown["animationClips"] = GetAssetMemoryBreakdown<AnimationClip>("AnimationClip");
                    assetBreakdown["shaders"] = GetAssetMemoryBreakdown<Shader>("Shader");
                    result["assetBreakdown"] = assetBreakdown;
                }

                return result;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GetMemorySnapshotTool: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error capturing memory snapshot: {ex.Message}",
                    "profiler_error"
                );
            }
        }

        private static JObject GetAssetMemoryBreakdown<T>(string typeName) where T : UnityEngine.Object
        {
            var objects = Resources.FindObjectsOfTypeAll<T>();
            long totalBytes = 0;
            foreach (var obj in objects)
            {
                totalBytes += Profiler.GetRuntimeMemorySizeLong(obj);
            }

            return new JObject
            {
                ["count"] = objects.Length,
                ["totalMB"] = ProfilerHelpers.BytesToMB(totalBytes),
                ["typeName"] = typeName
            };
        }
    }

    /// <summary>
    /// Tool for retrieving rendering statistics from the Unity Editor.
    /// Returns draw calls, batches, triangles, vertices, and other rendering metrics.
    /// Note: rendering stats are only populated during Play Mode.
    /// </summary>
    public class GetRenderingStatsTool : McpToolBase
    {
        public GetRenderingStatsTool()
        {
            Name = "get_rendering_stats";
            Description = "Gets current rendering statistics from UnityEditor.UnityStats including draw calls, batches, triangles, vertices, and more. Data is most meaningful during Play Mode.";
        }

        public override JObject Execute(JObject parameters)
        {
            try
            {
                McpLogger.LogInfo("Executing GetRenderingStatsTool");

                bool isPlayMode = EditorApplication.isPlaying;

                var result = new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = isPlayMode
                        ? "Rendering stats retrieved"
                        : "Rendering stats retrieved (not in Play Mode, values may be zero)",
                    ["isPlayMode"] = isPlayMode,
                    ["batches"] = UnityStats.batches,
                    ["setPassCalls"] = UnityStats.setPassCalls,
                    ["triangles"] = UnityStats.triangles,
                    ["vertices"] = UnityStats.vertices,
                    ["shadowCasters"] = UnityStats.shadowCasters,
                    ["screenRes"] = UnityStats.screenRes,
                    ["screenBytes"] = UnityStats.screenBytes
                };

#pragma warning disable CS0618
                try
                {
                    result["drawCalls"] = UnityStats.drawCalls;
                    result["renderTextureChanges"] = UnityStats.renderTextureChanges;
                }
                catch
                {
                    result["drawCalls"] = -1;
                    result["renderTextureChanges"] = -1;
                }
#pragma warning restore CS0618

                return result;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GetRenderingStatsTool: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error getting rendering stats: {ex.Message}",
                    "profiler_error"
                );
            }
        }
    }

    /// <summary>
    /// Tool for retrieving frame timing information.
    /// Uses FrameTimingManager to capture CPU and GPU frame timings,
    /// and reports current FPS, target frame rate, and vSync settings.
    /// </summary>
    public class GetFrameTimingTool : McpToolBase
    {
        public GetFrameTimingTool()
        {
            Name = "get_frame_timing";
            Description = "Gets frame timing data including CPU/GPU times, current FPS, target frame rate, and vSync settings. Uses FrameTimingManager for detailed per-frame timings.";
        }

        public override JObject Execute(JObject parameters)
        {
            try
            {
                int frameCount = parameters?["frameCount"]?.ToObject<int>() ?? 10;
                if (frameCount < 1) frameCount = 1;
                if (frameCount > 120) frameCount = 120;

                McpLogger.LogInfo($"Executing GetFrameTimingTool: frameCount={frameCount}");

                bool isPlayMode = EditorApplication.isPlaying;

                FrameTimingManager.CaptureFrameTimings();

                var timingsArray = new FrameTiming[frameCount];
                uint retrieved = FrameTimingManager.GetLatestTimings((uint)frameCount, timingsArray);

                float currentFps = isPlayMode && Time.unscaledDeltaTime > 0f
                    ? 1f / Time.unscaledDeltaTime
                    : 0f;

                int vSyncCount = QualitySettings.vSyncCount;
                int targetFrameRate = Application.targetFrameRate;

                var timingsJson = new JArray();
                double cpuTotal = 0;
                double gpuTotal = 0;
                int validCount = 0;

                for (int i = 0; i < (int)retrieved; i++)
                {
                    var timing = timingsArray[i];
                    double cpuMs = timing.cpuFrameTime;
                    double gpuMs = timing.gpuFrameTime;

                    timingsJson.Add(new JObject
                    {
                        ["cpuTimeMs"] = Math.Round(cpuMs, 3),
                        ["gpuTimeMs"] = Math.Round(gpuMs, 3)
                    });

                    cpuTotal += cpuMs;
                    gpuTotal += gpuMs;
                    validCount++;
                }

                double avgCpuMs = validCount > 0 ? Math.Round(cpuTotal / validCount, 3) : 0;
                double avgGpuMs = validCount > 0 ? Math.Round(gpuTotal / validCount, 3) : 0;

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = isPlayMode
                        ? $"Frame timing data retrieved ({retrieved} frames captured)"
                        : $"Frame timing data retrieved ({retrieved} frames captured, not in Play Mode)",
                    ["isPlayMode"] = isPlayMode,
                    ["currentFps"] = Math.Round(currentFps, 2),
                    ["targetFps"] = targetFrameRate,
                    ["vSyncCount"] = vSyncCount,
                    ["framesRetrieved"] = (int)retrieved,
                    ["averageCpuTimeMs"] = avgCpuMs,
                    ["averageGpuTimeMs"] = avgGpuMs,
                    ["timings"] = timingsJson
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GetFrameTimingTool: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Error getting frame timing: {ex.Message}",
                    "profiler_error"
                );
            }
        }
    }

    /// <summary>
    /// Tool for retrieving profiler frame data with CPU sample hierarchy.
    /// Accesses ProfilerDriver to read recorded frames and extracts the top
    /// CPU-consuming samples per frame.
    /// </summary>
    public class GetProfilerDataTool : McpToolBase
    {
        public GetProfilerDataTool()
        {
            Name = "get_profiler_data";
            Description = "Gets profiler frame data including top CPU samples per frame from the Unity Profiler. Requires the Profiler to be recording. Returns sample names, total/self times, call counts, and GC allocations.";
        }

        public override JObject Execute(JObject parameters)
        {
            int frameCount = parameters?["frameCount"]?.ToObject<int>() ?? 1;
            if (frameCount < 1) frameCount = 1;
            if (frameCount > 300) frameCount = 300;

            var categoriesParam = parameters?["categories"] as JArray;
            HashSet<string> categoryFilter = null;
            if (categoriesParam != null && categoriesParam.Count > 0)
            {
                categoryFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var cat in categoriesParam)
                {
                    string catStr = cat?.ToObject<string>();
                    if (!string.IsNullOrEmpty(catStr))
                    {
                        categoryFilter.Add(catStr);
                    }
                }
            }

            McpLogger.LogInfo($"Executing GetProfilerDataTool: frameCount={frameCount}");

            bool profilerEnabled = Profiler.enabled;

            var framesJson = new JArray();

            try
            {
                int lastFrame = ProfilerDriver.lastFrameIndex;
                int firstFrame = ProfilerDriver.firstFrameIndex;

                if (lastFrame < 0 || firstFrame < 0 || lastFrame < firstFrame)
                {
                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = "No profiler data available. Ensure the Profiler is enabled and recording.",
                        ["profilerEnabled"] = profilerEnabled,
                        ["frames"] = new JArray()
                    };
                }

                int startFrame = Math.Max(firstFrame, lastFrame - frameCount + 1);

                for (int frameIndex = startFrame; frameIndex <= lastFrame; frameIndex++)
                {
                    var frameData = GetFrameSamples(frameIndex, categoryFilter);
                    framesJson.Add(frameData);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GetProfilerDataTool: Error accessing profiler data: {ex.Message}");
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Warning: Could not access profiler data. {ex.Message}",
                    ["profilerEnabled"] = profilerEnabled,
                    ["frames"] = new JArray()
                };
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Profiler data retrieved for {framesJson.Count} frame(s)",
                ["profilerEnabled"] = profilerEnabled,
                ["frames"] = framesJson
            };
        }

        private static JObject GetFrameSamples(int frameIndex, HashSet<string> categoryFilter)
        {
            var samplesJson = new JArray();

            using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex,
                0,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnDontSort))
            {
                if (frameData == null || !frameData.valid)
                {
                    return new JObject
                    {
                        ["frameIndex"] = frameIndex,
                        ["samples"] = samplesJson
                    };
                }

                int rootId = frameData.GetRootItemID();
                var children = new List<int>();
                frameData.GetItemChildren(rootId, children);

                var sampleList = new List<ProfilerSampleData>();

                foreach (int childId in children)
                {
                    string name = frameData.GetItemName(childId);
                    float totalMs = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnTotalTime);
                    float selfMs = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnSelfTime);
                    int calls = (int)frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnCalls);
                    float gcAlloc = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnGcMemory);

                    if (categoryFilter != null)
                    {
                        try
                        {
                            string categoryName = frameData.GetItemCategoryName(childId);
                            if (!categoryFilter.Contains(categoryName))
                            {
                                continue;
                            }
                        }
                        catch
                        {
                            // GetItemCategoryName may not be available in all Unity versions
                        }
                    }

                    sampleList.Add(new ProfilerSampleData
                    {
                        Name = name,
                        TotalMs = totalMs,
                        SelfMs = selfMs,
                        Calls = calls,
                        GcAllocBytes = gcAlloc
                    });
                }

                sampleList.Sort((a, b) => b.TotalMs.CompareTo(a.TotalMs));
                int count = Math.Min(20, sampleList.Count);

                for (int i = 0; i < count; i++)
                {
                    var sample = sampleList[i];
                    samplesJson.Add(new JObject
                    {
                        ["name"] = sample.Name,
                        ["totalMs"] = Math.Round(sample.TotalMs, 3),
                        ["selfMs"] = Math.Round(sample.SelfMs, 3),
                        ["calls"] = sample.Calls,
                        ["gcAllocBytes"] = Math.Round(sample.GcAllocBytes, 0)
                    });
                }
            }

            return new JObject
            {
                ["frameIndex"] = frameIndex,
                ["samples"] = samplesJson
            };
        }
    }

    /// <summary>
    /// Tool for generating a comprehensive profiler report that combines
    /// memory, rendering, frame timing, and CPU sample data into a single response.
    /// Internally reuses logic from the other profiler tools.
    /// </summary>
    public class GetProfilerReportTool : McpToolBase
    {
        public GetProfilerReportTool()
        {
            Name = "get_profiler_report";
            Description = "Generates a comprehensive profiler report combining memory snapshot, rendering stats, frame timing, and top CPU samples into a single response.";
        }

        public override JObject Execute(JObject parameters)
        {
            bool includeMemory = parameters?["includeMemory"]?.ToObject<bool>() ?? true;
            bool includeRendering = parameters?["includeRendering"]?.ToObject<bool>() ?? true;
            bool includeFrameTiming = parameters?["includeFrameTiming"]?.ToObject<bool>() ?? true;
            bool includeTopCpuSamples = parameters?["includeTopCpuSamples"]?.ToObject<bool>() ?? true;
            int topSampleCount = parameters?["topSampleCount"]?.ToObject<int>() ?? 10;
            if (topSampleCount < 1) topSampleCount = 1;
            if (topSampleCount > 50) topSampleCount = 50;

            McpLogger.LogInfo($"Executing GetProfilerReportTool: memory={includeMemory}, rendering={includeRendering}, frameTiming={includeFrameTiming}, cpuSamples={includeTopCpuSamples}");

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Profiler report generated"
            };

            if (includeMemory)
            {
                result["memory"] = BuildMemorySection();
            }

            if (includeRendering)
            {
                result["rendering"] = BuildRenderingSection();
            }

            if (includeFrameTiming)
            {
                result["frameTiming"] = BuildFrameTimingSection();
            }

            if (includeTopCpuSamples)
            {
                result["topCpuSamples"] = BuildTopCpuSamplesSection(topSampleCount);
            }

            return result;
        }

        private static JObject BuildMemorySection()
        {
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = Profiler.GetTotalReservedMemoryLong();
            long totalUnused = Profiler.GetTotalUnusedReservedMemoryLong();
            long monoUsed = Profiler.GetMonoUsedSizeLong();
            long monoHeap = Profiler.GetMonoHeapSizeLong();
            long graphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();

            return new JObject
            {
                ["totalAllocatedMB"] = ProfilerHelpers.BytesToMB(totalAllocated),
                ["totalReservedMB"] = ProfilerHelpers.BytesToMB(totalReserved),
                ["totalUnusedMB"] = ProfilerHelpers.BytesToMB(totalUnused),
                ["monoUsedMB"] = ProfilerHelpers.BytesToMB(monoUsed),
                ["monoHeapMB"] = ProfilerHelpers.BytesToMB(monoHeap),
                ["graphicsDriverMB"] = ProfilerHelpers.BytesToMB(graphicsDriver)
            };
        }

        private static JObject BuildRenderingSection()
        {
            var result = new JObject
            {
                ["isPlayMode"] = EditorApplication.isPlaying,
                ["batches"] = UnityStats.batches,
                ["setPassCalls"] = UnityStats.setPassCalls,
                ["triangles"] = UnityStats.triangles,
                ["vertices"] = UnityStats.vertices,
                ["shadowCasters"] = UnityStats.shadowCasters,
                ["screenRes"] = UnityStats.screenRes,
                ["screenBytes"] = UnityStats.screenBytes
            };

#pragma warning disable CS0618
            try
            {
                result["drawCalls"] = UnityStats.drawCalls;
                result["renderTextureChanges"] = UnityStats.renderTextureChanges;
            }
            catch
            {
                result["drawCalls"] = -1;
                result["renderTextureChanges"] = -1;
            }
#pragma warning restore CS0618

            return result;
        }

        private static JObject BuildFrameTimingSection()
        {
            bool isPlayMode = EditorApplication.isPlaying;

            FrameTimingManager.CaptureFrameTimings();

            var timingsArray = new FrameTiming[10];
            uint retrieved = FrameTimingManager.GetLatestTimings(10, timingsArray);

            float currentFps = isPlayMode && Time.unscaledDeltaTime > 0f
                ? 1f / Time.unscaledDeltaTime
                : 0f;

            double cpuTotal = 0;
            double gpuTotal = 0;
            int validCount = 0;

            for (int i = 0; i < (int)retrieved; i++)
            {
                cpuTotal += timingsArray[i].cpuFrameTime;
                gpuTotal += timingsArray[i].gpuFrameTime;
                validCount++;
            }

            double avgCpuMs = validCount > 0 ? Math.Round(cpuTotal / validCount, 3) : 0;
            double avgGpuMs = validCount > 0 ? Math.Round(gpuTotal / validCount, 3) : 0;

            return new JObject
            {
                ["isPlayMode"] = isPlayMode,
                ["currentFps"] = Math.Round(currentFps, 2),
                ["targetFps"] = Application.targetFrameRate,
                ["vSyncCount"] = QualitySettings.vSyncCount,
                ["framesRetrieved"] = (int)retrieved,
                ["averageCpuTimeMs"] = avgCpuMs,
                ["averageGpuTimeMs"] = avgGpuMs
            };
        }

        private static JObject BuildTopCpuSamplesSection(int topSampleCount)
        {
            var samplesJson = new JArray();

            try
            {
                int lastFrame = ProfilerDriver.lastFrameIndex;
                int firstFrame = ProfilerDriver.firstFrameIndex;

                if (lastFrame < 0 || firstFrame < 0 || lastFrame < firstFrame)
                {
                    return new JObject
                    {
                        ["warning"] = "No profiler data available. Ensure the Profiler is enabled and recording.",
                        ["profilerEnabled"] = Profiler.enabled,
                        ["samples"] = new JArray()
                    };
                }

                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
                    lastFrame,
                    0,
                    HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnDontSort))
                {
                    if (frameData != null && frameData.valid)
                    {
                        int rootId = frameData.GetRootItemID();
                        var children = new List<int>();
                        frameData.GetItemChildren(rootId, children);

                        var sampleList = new List<ProfilerSampleData>();

                        foreach (int childId in children)
                        {
                            string name = frameData.GetItemName(childId);
                            float totalMs = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnTotalTime);
                            float selfMs = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnSelfTime);
                            int calls = (int)frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnCalls);
                            float gcAlloc = frameData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnGcMemory);

                            sampleList.Add(new ProfilerSampleData
                            {
                                Name = name,
                                TotalMs = totalMs,
                                SelfMs = selfMs,
                                Calls = calls,
                                GcAllocBytes = gcAlloc
                            });
                        }

                        sampleList.Sort((a, b) => b.TotalMs.CompareTo(a.TotalMs));
                        int count = Math.Min(topSampleCount, sampleList.Count);

                        for (int i = 0; i < count; i++)
                        {
                            var sample = sampleList[i];
                            samplesJson.Add(new JObject
                            {
                                ["name"] = sample.Name,
                                ["totalMs"] = Math.Round(sample.TotalMs, 3),
                                ["selfMs"] = Math.Round(sample.SelfMs, 3),
                                ["calls"] = sample.Calls,
                                ["gcAllocBytes"] = Math.Round(sample.GcAllocBytes, 0)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GetProfilerReportTool: Error accessing CPU sample data: {ex.Message}");
                return new JObject
                {
                    ["warning"] = $"Could not access profiler data: {ex.Message}",
                    ["profilerEnabled"] = Profiler.enabled,
                    ["samples"] = new JArray()
                };
            }

            return new JObject
            {
                ["profilerEnabled"] = Profiler.enabled,
                ["frameIndex"] = ProfilerDriver.lastFrameIndex,
                ["samples"] = samplesJson
            };
        }
    }
}
