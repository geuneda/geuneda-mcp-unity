import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// build_project Tool
// ============================================================================

const buildProjectToolName = 'build_project';
const buildProjectToolDescription = 'Builds the Unity project for a specified target platform. Supports Windows, macOS, Android, iOS, and WebGL.';
const buildProjectParamsSchema = z.object({
  target: z.enum(['StandaloneWindows64', 'StandaloneOSX', 'Android', 'iOS', 'WebGL']).describe('The build target platform'),
  outputPath: z.string().optional().describe('The output path for the build. Defaults to "Builds/<target>/Build<ext>"'),
  developmentBuild: z.boolean().optional().default(false).describe('Whether to create a development build (default: false)'),
  scenes: z.array(z.string()).optional().describe('Array of scene paths to include. Defaults to scenes enabled in Build Settings')
});

/**
 * Registers the build_project tool with the MCP server
 */
export function registerBuildProjectTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${buildProjectToolName}`);

  server.tool(
    buildProjectToolName,
    buildProjectToolDescription,
    buildProjectParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${buildProjectToolName}`, params);
        const result = await buildProjectHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${buildProjectToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${buildProjectToolName}`, error);
        throw error;
      }
    }
  );
}

async function buildProjectHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.target) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'target' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: buildProjectToolName,
    params: {
      target: params.target,
      outputPath: params.outputPath,
      developmentBuild: params.developmentBuild ?? false,
      scenes: params.scenes
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to build project'
    );
  }

  let text = response.message || 'Build completed';
  text += `\nResult: ${response.buildResult}`;
  text += `\nOutput: ${response.outputPath}`;
  text += `\nDuration: ${response.duration}s`;
  text += `\nErrors: ${response.totalErrors}, Warnings: ${response.totalWarnings}`;

  if (response.errors && response.errors.length > 0) {
    text += '\n\nErrors:\n' + response.errors.join('\n');
  }
  if (response.warnings && response.warnings.length > 0) {
    text += '\n\nWarnings:\n' + response.warnings.join('\n');
  }

  return {
    content: [{
      type: 'text',
      text
    }]
  };
}

// ============================================================================
// get_build_settings Tool
// ============================================================================

const getBuildSettingsToolName = 'get_build_settings';
const getBuildSettingsToolDescription = 'Gets the current build settings including target platform, scenes, and configuration';
const getBuildSettingsParamsSchema = z.object({});

/**
 * Registers the get_build_settings tool with the MCP server
 */
export function registerGetBuildSettingsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getBuildSettingsToolName}`);

  server.tool(
    getBuildSettingsToolName,
    getBuildSettingsToolDescription,
    getBuildSettingsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getBuildSettingsToolName}`, params);
        const result = await getBuildSettingsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getBuildSettingsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getBuildSettingsToolName}`, error);
        throw error;
      }
    }
  );
}

async function getBuildSettingsHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: getBuildSettingsToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get build settings'
    );
  }

  let text = `Active Build Target: ${response.activeBuildTarget}\n`;
  text += `Build Target Group: ${response.selectedBuildTargetGroup}\n`;
  text += `Development: ${response.development}\n`;
  text += `Scripting Backend: ${response.scriptingBackend}\n`;
  text += `IL2CPP Configuration: ${response.il2CppCompilerConfiguration}\n\n`;
  text += `Scenes in Build Settings:\n`;

  if (response.scenes && Array.isArray(response.scenes)) {
    for (const scene of response.scenes) {
      text += `  ${scene.enabled ? '[x]' : '[ ]'} ${scene.path}\n`;
    }
  }

  return {
    content: [{
      type: 'text',
      text
    }],
    data: {
      activeBuildTarget: response.activeBuildTarget,
      selectedBuildTargetGroup: response.selectedBuildTargetGroup,
      scenes: response.scenes,
      development: response.development,
      scriptingBackend: response.scriptingBackend,
      il2CppCompilerConfiguration: response.il2CppCompilerConfiguration
    }
  };
}

// ============================================================================
// Combined Registration
// ============================================================================

/**
 * Registers all build tools with the MCP server
 */
export function registerBuildTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerBuildProjectTool(server, mcpUnity, logger);
  registerGetBuildSettingsTool(server, mcpUnity, logger);
}
