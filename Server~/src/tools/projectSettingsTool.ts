import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// GET PROJECT SETTINGS TOOL
// ============================================================================

const getProjectSettingsToolName = 'get_project_settings';
const getProjectSettingsToolDescription = 'Gets Unity project settings for a specific category (player, quality, physics, time, build)';
const getProjectSettingsParamsSchema = z.object({
  category: z.enum(['player', 'quality', 'physics', 'time', 'build']).describe('The settings category to retrieve')
});

/**
 * Registers the Get Project Settings tool with the MCP server
 */
export function registerGetProjectSettingsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getProjectSettingsToolName}`);

  server.tool(
    getProjectSettingsToolName,
    getProjectSettingsToolDescription,
    getProjectSettingsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getProjectSettingsToolName}`, params);
        const result = await getProjectSettingsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getProjectSettingsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getProjectSettingsToolName}`, error);
        throw error;
      }
    }
  );
}

async function getProjectSettingsHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.category) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'category' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: getProjectSettingsToolName,
    params: {
      category: params.category
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get project settings'
    );
  }

  return {
    content: [{
      type: 'text',
      text: response.message || `Retrieved ${params.category} settings`
    }, {
      type: 'text',
      text: JSON.stringify(response.settings, null, 2)
    }]
  };
}

// ============================================================================
// SET PROJECT SETTINGS TOOL
// ============================================================================

const setProjectSettingsToolName = 'set_project_settings';
const setProjectSettingsToolDescription = 'Modifies Unity project settings for a specific category (player, quality, physics, time)';
const setProjectSettingsParamsSchema = z.object({
  category: z.enum(['player', 'quality', 'physics', 'time']).describe('The settings category to modify'),
  settings: z.record(z.any()).describe('Key-value pairs of settings to modify (e.g., {"companyName": "MyCompany", "productName": "MyGame"})')
});

/**
 * Registers the Set Project Settings tool with the MCP server
 */
export function registerSetProjectSettingsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${setProjectSettingsToolName}`);

  server.tool(
    setProjectSettingsToolName,
    setProjectSettingsToolDescription,
    setProjectSettingsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${setProjectSettingsToolName}`, params);
        const result = await setProjectSettingsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${setProjectSettingsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${setProjectSettingsToolName}`, error);
        throw error;
      }
    }
  );
}

async function setProjectSettingsHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.category) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'category' must be provided"
    );
  }

  if (!params.settings || Object.keys(params.settings).length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'settings' must be provided and contain at least one setting"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: setProjectSettingsToolName,
    params: {
      category: params.category,
      settings: params.settings
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to set project settings'
    );
  }

  return {
    content: [{
      type: 'text',
      text: response.message || `Updated ${params.category} settings`
    }]
  };
}

// ============================================================================
// GET BUILD SCENES TOOL
// ============================================================================

const getBuildScenesToolName = 'get_build_scenes';
const getBuildScenesToolDescription = 'Gets the list of scenes in the Unity Build Settings';
const getBuildScenesParamsSchema = z.object({});

/**
 * Registers the Get Build Scenes tool with the MCP server
 */
export function registerGetBuildScenesTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getBuildScenesToolName}`);

  server.tool(
    getBuildScenesToolName,
    getBuildScenesToolDescription,
    getBuildScenesParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getBuildScenesToolName}`, params);
        const result = await getBuildScenesHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getBuildScenesToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getBuildScenesToolName}`, error);
        throw error;
      }
    }
  );
}

async function getBuildScenesHandler(mcpUnity: McpUnity, _params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: getBuildScenesToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get build scenes'
    );
  }

  return {
    content: [{
      type: 'text',
      text: response.message || 'Retrieved build scenes'
    }, {
      type: 'text',
      text: JSON.stringify(response.scenes, null, 2)
    }]
  };
}

// ============================================================================
// SET BUILD SCENES TOOL
// ============================================================================

const setBuildScenesToolName = 'set_build_scenes';
const setBuildScenesToolDescription = 'Sets the list of scenes in the Unity Build Settings';
const setBuildScenesParamsSchema = z.object({
  scenes: z.array(z.object({
    path: z.string().describe('The asset path of the scene (e.g., "Assets/Scenes/MainScene.unity")'),
    enabled: z.boolean().optional().default(true).describe('Whether the scene is enabled in the build')
  })).describe('Array of scenes to set in Build Settings')
});

/**
 * Registers the Set Build Scenes tool with the MCP server
 */
export function registerSetBuildScenesTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${setBuildScenesToolName}`);

  server.tool(
    setBuildScenesToolName,
    setBuildScenesToolDescription,
    setBuildScenesParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${setBuildScenesToolName}`, params);
        const result = await setBuildScenesHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${setBuildScenesToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${setBuildScenesToolName}`, error);
        throw error;
      }
    }
  );
}

async function setBuildScenesHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.scenes || !Array.isArray(params.scenes) || params.scenes.length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'scenes' must be a non-empty array"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: setBuildScenesToolName,
    params: {
      scenes: params.scenes
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to set build scenes'
    );
  }

  return {
    content: [{
      type: 'text',
      text: response.message || `Updated build scenes`
    }]
  };
}
