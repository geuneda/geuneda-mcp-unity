import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// get_memory_snapshot Tool
// ============================================================================

const getMemorySnapshotToolName = 'get_memory_snapshot';
const getMemorySnapshotToolDescription = 'Retrieves a snapshot of Unity memory usage including total allocated, reserved, and unused memory, Mono heap usage, graphics driver memory, and optionally a detailed breakdown by asset type.';
const getMemorySnapshotParamsSchema = z.object({
  detailed: z.boolean().optional().describe('When true, includes a per-asset-type breakdown table (Textures, Meshes, Materials, etc.)')
});

export function registerGetMemorySnapshotTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getMemorySnapshotToolName}`);

  server.tool(
    getMemorySnapshotToolName,
    getMemorySnapshotToolDescription,
    getMemorySnapshotParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getMemorySnapshotToolName}`, params);
        const result = await getMemorySnapshotHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getMemorySnapshotToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getMemorySnapshotToolName}`, error);
        throw error;
      }
    }
  );
}

async function getMemorySnapshotHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { detailed = false } = params;

  const response = await mcpUnity.sendRequest({
    method: getMemorySnapshotToolName,
    params: { detailed }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get memory snapshot'
    );
  }

  let text = '--- Memory Snapshot ---\n';
  text += `Total Allocated: ${response.totalAllocatedMB} MB\n`;
  text += `Total Reserved:  ${response.totalReservedMB} MB\n`;
  text += `Total Unused:    ${response.totalUnusedMB} MB\n`;
  text += `\n`;
  text += `Mono Used:       ${response.monoUsedMB} MB\n`;
  text += `Mono Heap:       ${response.monoHeapMB} MB\n`;
  text += `\n`;
  text += `Graphics Driver: ${response.graphicsDriverMB} MB\n`;

  if (detailed && response.assetBreakdown) {
    text += `\n--- Asset Breakdown ---\n`;
    text += `${'Type'.padEnd(20)} ${'Count'.padStart(8)} ${'Size (MB)'.padStart(12)}\n`;
    text += `${'-'.repeat(20)} ${'-'.repeat(8)} ${'-'.repeat(12)}\n`;

    const breakdown = response.assetBreakdown;
    const categories = ['textures', 'meshes', 'materials', 'audioClips', 'animationClips', 'shaders'];
    for (const cat of categories) {
      if (breakdown[cat]) {
        const entry = breakdown[cat];
        const name = (entry.typeName || cat).padEnd(20);
        const count = String(entry.count).padStart(8);
        const size = String(entry.totalMB).padStart(12);
        text += `${name} ${count} ${size}\n`;
      }
    }
  }

  return {
    content: [{ type: 'text', text }],
    data: {
      totalAllocatedMB: response.totalAllocatedMB,
      totalReservedMB: response.totalReservedMB,
      totalUnusedMB: response.totalUnusedMB,
      monoUsedMB: response.monoUsedMB,
      monoHeapMB: response.monoHeapMB,
      graphicsDriverMB: response.graphicsDriverMB,
      ...(detailed && response.assetBreakdown ? { assetBreakdown: response.assetBreakdown } : {})
    }
  };
}

// ============================================================================
// get_rendering_stats Tool
// ============================================================================

const getRenderingStatsToolName = 'get_rendering_stats';
const getRenderingStatsToolDescription = 'Retrieves current rendering statistics from Unity including draw calls, batches, set-pass calls, triangle and vertex counts, and other GPU pipeline metrics.';
const getRenderingStatsParamsSchema = z.object({});

export function registerGetRenderingStatsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getRenderingStatsToolName}`);

  server.tool(
    getRenderingStatsToolName,
    getRenderingStatsToolDescription,
    getRenderingStatsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getRenderingStatsToolName}`, params);
        const result = await getRenderingStatsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getRenderingStatsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getRenderingStatsToolName}`, error);
        throw error;
      }
    }
  );
}

async function getRenderingStatsHandler(mcpUnity: McpUnity, _params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: getRenderingStatsToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get rendering stats'
    );
  }

  let text = '--- Rendering Stats ---\n';
  text += `${'Metric'.padEnd(28)} ${'Value'.padStart(14)}\n`;
  text += `${'-'.repeat(28)} ${'-'.repeat(14)}\n`;
  text += `${'Draw Calls'.padEnd(28)} ${String(response.drawCalls).padStart(14)}\n`;
  text += `${'Batches'.padEnd(28)} ${String(response.batches).padStart(14)}\n`;
  text += `${'Set Pass Calls'.padEnd(28)} ${String(response.setPassCalls).padStart(14)}\n`;
  text += `${'Triangles'.padEnd(28)} ${String(response.triangles).padStart(14)}\n`;
  text += `${'Vertices'.padEnd(28)} ${String(response.vertices).padStart(14)}\n`;
  text += `${'Shadow Casters'.padEnd(28)} ${String(response.shadowCasters).padStart(14)}\n`;
  text += `${'Render Texture Changes'.padEnd(28)} ${String(response.renderTextureChanges).padStart(14)}\n`;
  text += `${'Screen Res'.padEnd(28)} ${String(response.screenRes).padStart(14)}\n`;
  text += `${'Screen Bytes'.padEnd(28)} ${String(response.screenBytes).padStart(14)}\n`;

  if (!response.isPlayMode) {
    text += `\nNote: Editor is not in Play Mode. Rendering stats may not reflect runtime performance.`;
  }

  return {
    content: [{ type: 'text', text }],
    data: {
      isPlayMode: response.isPlayMode,
      drawCalls: response.drawCalls,
      batches: response.batches,
      setPassCalls: response.setPassCalls,
      triangles: response.triangles,
      vertices: response.vertices,
      shadowCasters: response.shadowCasters,
      renderTextureChanges: response.renderTextureChanges,
      screenRes: response.screenRes,
      screenBytes: response.screenBytes
    }
  };
}

// ============================================================================
// get_frame_timing Tool
// ============================================================================

const getFrameTimingToolName = 'get_frame_timing';
const getFrameTimingToolDescription = 'Retrieves frame timing data from the Unity Profiler including CPU and GPU frame times, current FPS, target frame rate, and vSync settings.';
const getFrameTimingParamsSchema = z.object({
  frameCount: z.number().int().min(1).max(120).optional().describe('Number of recent frames to retrieve timing for (1-120). Defaults to 10')
});

export function registerGetFrameTimingTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getFrameTimingToolName}`);

  server.tool(
    getFrameTimingToolName,
    getFrameTimingToolDescription,
    getFrameTimingParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getFrameTimingToolName}`, params);
        const result = await getFrameTimingHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getFrameTimingToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getFrameTimingToolName}`, error);
        throw error;
      }
    }
  );
}

async function getFrameTimingHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { frameCount = 10 } = params;

  const response = await mcpUnity.sendRequest({
    method: getFrameTimingToolName,
    params: { frameCount }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get frame timing data'
    );
  }

  const fmt = (ms: number): string => Number(ms).toFixed(2);

  // Compute min/max from timings array
  let minCpu = Infinity, maxCpu = 0, minGpu = Infinity, maxGpu = 0;
  if (response.timings && Array.isArray(response.timings)) {
    for (const t of response.timings) {
      if (t.cpuTimeMs < minCpu) minCpu = t.cpuTimeMs;
      if (t.cpuTimeMs > maxCpu) maxCpu = t.cpuTimeMs;
      if (t.gpuTimeMs < minGpu) minGpu = t.gpuTimeMs;
      if (t.gpuTimeMs > maxGpu) maxGpu = t.gpuTimeMs;
    }
  }
  if (minCpu === Infinity) minCpu = 0;
  if (minGpu === Infinity) minGpu = 0;

  let text = `--- Frame Timing (${response.framesRetrieved} frames) ---\n\n`;
  text += `Current FPS:    ${fmt(response.currentFps)}\n`;
  text += `Target FPS:     ${response.targetFps}\n`;
  text += `VSync Count:    ${response.vSyncCount}\n\n`;
  text += `${''.padEnd(12)} ${'Avg (ms)'.padStart(12)} ${'Min (ms)'.padStart(12)} ${'Max (ms)'.padStart(12)}\n`;
  text += `${'-'.repeat(12)} ${'-'.repeat(12)} ${'-'.repeat(12)} ${'-'.repeat(12)}\n`;
  text += `${'CPU Time'.padEnd(12)} ${fmt(response.averageCpuTimeMs).padStart(12)} ${fmt(minCpu).padStart(12)} ${fmt(maxCpu).padStart(12)}\n`;
  text += `${'GPU Time'.padEnd(12)} ${fmt(response.averageGpuTimeMs).padStart(12)} ${fmt(minGpu).padStart(12)} ${fmt(maxGpu).padStart(12)}\n`;

  return {
    content: [{ type: 'text', text }],
    data: {
      currentFps: response.currentFps,
      targetFps: response.targetFps,
      vSyncCount: response.vSyncCount,
      framesRetrieved: response.framesRetrieved,
      averageCpuTimeMs: response.averageCpuTimeMs,
      averageGpuTimeMs: response.averageGpuTimeMs,
      minCpuTimeMs: minCpu,
      maxCpuTimeMs: maxCpu,
      minGpuTimeMs: minGpu,
      maxGpuTimeMs: maxGpu,
      timings: response.timings
    }
  };
}

// ============================================================================
// get_profiler_data Tool
// ============================================================================

const getProfilerDataToolName = 'get_profiler_data';
const getProfilerDataToolDescription = 'Retrieves per-frame CPU profiler sample data from the Unity Profiler. Returns the top CPU-consuming functions for each frame, optionally filtered by profiler categories.';
const getProfilerDataParamsSchema = z.object({
  frameCount: z.number().int().min(1).max(300).optional().describe('Number of recent frames to retrieve (1-300). Defaults to 1'),
  categories: z.array(z.string()).optional().describe('Profiler category names to filter by (e.g. ["Rendering", "Scripts", "Physics"]). Defaults to all categories')
});

export function registerGetProfilerDataTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getProfilerDataToolName}`);

  server.tool(
    getProfilerDataToolName,
    getProfilerDataToolDescription,
    getProfilerDataParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getProfilerDataToolName}`, params);
        const result = await getProfilerDataHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getProfilerDataToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getProfilerDataToolName}`, error);
        throw error;
      }
    }
  );
}

async function getProfilerDataHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { frameCount = 1, categories } = params;

  const response = await mcpUnity.sendRequest({
    method: getProfilerDataToolName,
    params: {
      frameCount,
      ...(categories ? { categories } : {})
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get profiler data'
    );
  }

  let text = `--- Profiler Data ---\n`;
  text += `Profiler Enabled: ${response.profilerEnabled}\n`;
  text += `Frames: ${response.frames?.length || 0}\n`;

  if (response.frames && Array.isArray(response.frames)) {
    for (const frame of response.frames) {
      text += `\nFrame ${frame.frameIndex}:\n`;
      text += `  ${'Function'.padEnd(48)} ${'Total (ms)'.padStart(12)} ${'Self (ms)'.padStart(12)} ${'Calls'.padStart(8)} ${'GC (B)'.padStart(10)}\n`;
      text += `  ${'-'.repeat(48)} ${'-'.repeat(12)} ${'-'.repeat(12)} ${'-'.repeat(8)} ${'-'.repeat(10)}\n`;

      if (frame.samples && Array.isArray(frame.samples)) {
        for (const sample of frame.samples) {
          const name = String(sample.name).length > 48
            ? String(sample.name).substring(0, 45) + '...'
            : String(sample.name).padEnd(48);
          const total = Number(sample.totalMs).toFixed(2).padStart(12);
          const self = Number(sample.selfMs).toFixed(2).padStart(12);
          const calls = String(sample.calls).padStart(8);
          const gc = String(Math.round(sample.gcAllocBytes)).padStart(10);
          text += `  ${name} ${total} ${self} ${calls} ${gc}\n`;
        }
      }
    }
  }

  return {
    content: [{ type: 'text', text }],
    data: {
      profilerEnabled: response.profilerEnabled,
      frames: response.frames
    }
  };
}

// ============================================================================
// get_profiler_report Tool
// ============================================================================

const getProfilerReportToolName = 'get_profiler_report';
const getProfilerReportToolDescription = 'Generates a comprehensive profiler report combining memory, rendering, frame timing, and CPU sample data into a single structured summary. Each section can be individually toggled.';
const getProfilerReportParamsSchema = z.object({
  includeMemory: z.boolean().optional().describe('Include memory usage section. Defaults to true'),
  includeRendering: z.boolean().optional().describe('Include rendering stats section. Defaults to true'),
  includeFrameTiming: z.boolean().optional().describe('Include frame timing section. Defaults to true'),
  includeTopCpuSamples: z.boolean().optional().describe('Include top CPU samples section. Defaults to true'),
  topSampleCount: z.number().int().min(1).max(50).optional().describe('Number of top CPU samples to include (1-50). Defaults to 10')
});

export function registerGetProfilerReportTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getProfilerReportToolName}`);

  server.tool(
    getProfilerReportToolName,
    getProfilerReportToolDescription,
    getProfilerReportParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getProfilerReportToolName}`, params);
        const result = await getProfilerReportHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getProfilerReportToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getProfilerReportToolName}`, error);
        throw error;
      }
    }
  );
}

async function getProfilerReportHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const {
    includeMemory = true,
    includeRendering = true,
    includeFrameTiming = true,
    includeTopCpuSamples = true,
    topSampleCount = 10
  } = params;

  const response = await mcpUnity.sendRequest({
    method: getProfilerReportToolName,
    params: {
      includeMemory,
      includeRendering,
      includeFrameTiming,
      includeTopCpuSamples,
      topSampleCount
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get profiler report'
    );
  }

  let text = '# Unity Profiler Report\n';

  // Memory section
  if (includeMemory && response.memory) {
    const mem = response.memory;
    text += `\n## Memory\n\n`;
    text += `Total Allocated: ${mem.totalAllocatedMB} MB\n`;
    text += `Total Reserved:  ${mem.totalReservedMB} MB\n`;
    text += `Total Unused:    ${mem.totalUnusedMB} MB\n`;
    text += `Mono Used:       ${mem.monoUsedMB} MB\n`;
    text += `Mono Heap:       ${mem.monoHeapMB} MB\n`;
    text += `Graphics Driver: ${mem.graphicsDriverMB} MB\n`;
  }

  // Rendering section
  if (includeRendering && response.rendering) {
    const ren = response.rendering;
    text += `\n## Rendering\n\n`;
    text += `Draw Calls:              ${ren.drawCalls ?? 'N/A'}\n`;
    text += `Batches:                 ${ren.batches ?? 'N/A'}\n`;
    text += `Set Pass Calls:          ${ren.setPassCalls ?? 'N/A'}\n`;
    text += `Triangles:               ${ren.triangles ?? 'N/A'}\n`;
    text += `Vertices:                ${ren.vertices ?? 'N/A'}\n`;
    text += `Shadow Casters:          ${ren.shadowCasters ?? 'N/A'}\n`;
    text += `Render Texture Changes:  ${ren.renderTextureChanges ?? 'N/A'}\n`;
    text += `Screen Res:              ${ren.screenRes ?? 'N/A'}\n`;
    text += `Screen Bytes:            ${ren.screenBytes ?? 'N/A'}\n`;

    if (!ren.isPlayMode) {
      text += `\nNote: Editor is not in Play Mode. Rendering stats may not reflect runtime performance.\n`;
    }
  }

  // Frame timing section
  if (includeFrameTiming && response.frameTiming) {
    const ft = response.frameTiming;
    text += `\n## Frame Timing\n\n`;
    text += `Current FPS:    ${Number(ft.currentFps).toFixed(1)}\n`;
    text += `Target FPS:     ${ft.targetFps}\n`;
    text += `VSync Count:    ${ft.vSyncCount}\n`;
    text += `Frames Sampled: ${ft.framesRetrieved}\n`;
    text += `Avg CPU Time:   ${Number(ft.averageCpuTimeMs).toFixed(2)} ms\n`;
    text += `Avg GPU Time:   ${Number(ft.averageGpuTimeMs).toFixed(2)} ms\n`;
  }

  // Top CPU samples section
  if (includeTopCpuSamples && response.topCpuSamples) {
    const section = response.topCpuSamples;
    text += `\n## Top CPU Samples`;

    if (section.warning) {
      text += `\n\nWarning: ${section.warning}\n`;
    } else if (section.samples && Array.isArray(section.samples)) {
      text += ` (Frame ${section.frameIndex})\n\n`;
      text += `${'#'.padStart(4)} ${'Function'.padEnd(48)} ${'Total (ms)'.padStart(12)} ${'Self (ms)'.padStart(12)} ${'Calls'.padStart(8)} ${'GC (B)'.padStart(10)}\n`;
      text += `${'-'.repeat(4)} ${'-'.repeat(48)} ${'-'.repeat(12)} ${'-'.repeat(12)} ${'-'.repeat(8)} ${'-'.repeat(10)}\n`;

      for (let i = 0; i < section.samples.length; i++) {
        const sample = section.samples[i];
        const rank = String(i + 1).padStart(4);
        const name = String(sample.name).length > 48
          ? String(sample.name).substring(0, 45) + '...'
          : String(sample.name).padEnd(48);
        const total = Number(sample.totalMs).toFixed(2).padStart(12);
        const self = Number(sample.selfMs).toFixed(2).padStart(12);
        const calls = String(sample.calls).padStart(8);
        const gc = String(Math.round(sample.gcAllocBytes ?? 0)).padStart(10);
        text += `${rank} ${name} ${total} ${self} ${calls} ${gc}\n`;
      }
    }
  }

  return {
    content: [{ type: 'text', text }],
    data: {
      ...(includeMemory && response.memory ? { memory: response.memory } : {}),
      ...(includeRendering && response.rendering ? { rendering: response.rendering } : {}),
      ...(includeFrameTiming && response.frameTiming ? { frameTiming: response.frameTiming } : {}),
      ...(includeTopCpuSamples && response.topCpuSamples ? { topCpuSamples: response.topCpuSamples } : {})
    }
  };
}

// ============================================================================
// Combined Registration
// ============================================================================

export function registerProfilerTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerGetMemorySnapshotTool(server, mcpUnity, logger);
  registerGetRenderingStatsTool(server, mcpUnity, logger);
  registerGetFrameTimingTool(server, mcpUnity, logger);
  registerGetProfilerDataTool(server, mcpUnity, logger);
  registerGetProfilerReportTool(server, mcpUnity, logger);
}
