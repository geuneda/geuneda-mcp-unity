import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// SEARCH ASSETS TOOL
// ============================================================================

const searchAssetsToolName = 'search_assets';
const searchAssetsToolDescription = 'Searches for assets in the Unity project using filters like type, labels, and folder path';
const searchAssetsParamsSchema = z.object({
  searchQuery: z.string().optional().describe('Search query string to match asset names'),
  type: z.string().optional().describe('Asset type filter (e.g., "Texture2D", "Material", "Prefab", "MonoScript")'),
  labels: z.array(z.string()).optional().describe('Array of asset labels to filter by'),
  folder: z.string().optional().describe('Folder path to restrict search (e.g., "Assets/Prefabs")'),
  maxResults: z.number().int().min(1).optional().default(100).describe('Maximum number of results to return (default: 100)')
});

/**
 * Registers the Search Assets tool with the MCP server
 */
export function registerSearchAssetsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${searchAssetsToolName}`);

  server.tool(
    searchAssetsToolName,
    searchAssetsToolDescription,
    searchAssetsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${searchAssetsToolName}`, params);
        const result = await searchAssetsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${searchAssetsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${searchAssetsToolName}`, error);
        throw error;
      }
    }
  );
}

async function searchAssetsHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: searchAssetsToolName,
    params: {
      searchQuery: params.searchQuery,
      type: params.type,
      labels: params.labels,
      folder: params.folder,
      maxResults: params.maxResults ?? 100
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to search assets'
    );
  }

  // Format readable output
  let text = `Found ${response.returnedCount} of ${response.totalFound} asset(s)\n\n`;

  if (response.assets && Array.isArray(response.assets)) {
    for (const asset of response.assets) {
      text += `  ${asset.name} (${asset.type}) - ${asset.path}\n`;
    }
  }

  return {
    content: [{
      type: 'text',
      text: text
    }],
    data: {
      totalFound: response.totalFound,
      returnedCount: response.returnedCount,
      assets: response.assets
    }
  };
}

// ============================================================================
// GET ASSET DEPENDENCIES TOOL
// ============================================================================

const getAssetDependenciesToolName = 'get_asset_dependencies';
const getAssetDependenciesToolDescription = 'Gets the dependencies of an asset at the specified path, optionally including recursive dependencies';
const getAssetDependenciesParamsSchema = z.object({
  assetPath: z.string().describe('The asset path (e.g., "Assets/Scenes/MainScene.unity")'),
  recursive: z.boolean().optional().default(true).describe('Whether to include recursive (transitive) dependencies (default: true)')
});

/**
 * Registers the Get Asset Dependencies tool with the MCP server
 */
export function registerGetAssetDependenciesTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getAssetDependenciesToolName}`);

  server.tool(
    getAssetDependenciesToolName,
    getAssetDependenciesToolDescription,
    getAssetDependenciesParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getAssetDependenciesToolName}`, params);
        const result = await getAssetDependenciesHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getAssetDependenciesToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getAssetDependenciesToolName}`, error);
        throw error;
      }
    }
  );
}

async function getAssetDependenciesHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.assetPath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'assetPath' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: getAssetDependenciesToolName,
    params: {
      assetPath: params.assetPath,
      recursive: params.recursive ?? true
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get asset dependencies'
    );
  }

  // Format readable output
  let text = `Dependencies for '${response.assetPath}' (recursive: ${response.recursive})\n`;
  text += `Total: ${response.dependencyCount} dependency(ies)\n\n`;

  if (response.dependencies && Array.isArray(response.dependencies)) {
    for (const dep of response.dependencies) {
      text += `  ${dep.name} (${dep.type}) - ${dep.path}\n`;
    }
  }

  return {
    content: [{
      type: 'text',
      text: text
    }],
    data: {
      assetPath: response.assetPath,
      recursive: response.recursive,
      dependencyCount: response.dependencyCount,
      dependencies: response.dependencies
    }
  };
}

// ============================================================================
// REIMPORT ASSET TOOL
// ============================================================================

const reimportAssetToolName = 'reimport_asset';
const reimportAssetToolDescription = 'Reimports an asset at the specified path, optionally forcing a full reimport';
const reimportAssetParamsSchema = z.object({
  assetPath: z.string().describe('The asset path to reimport (e.g., "Assets/Textures/MyTexture.png")'),
  forceUpdate: z.boolean().optional().default(false).describe('Whether to force a full reimport (default: false)')
});

/**
 * Registers the Reimport Asset tool with the MCP server
 */
export function registerReimportAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${reimportAssetToolName}`);

  server.tool(
    reimportAssetToolName,
    reimportAssetToolDescription,
    reimportAssetParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${reimportAssetToolName}`, params);
        const result = await reimportAssetHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${reimportAssetToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${reimportAssetToolName}`, error);
        throw error;
      }
    }
  );
}

async function reimportAssetHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.assetPath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'assetPath' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: reimportAssetToolName,
    params: {
      assetPath: params.assetPath,
      forceUpdate: params.forceUpdate ?? false
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to reimport asset'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully reimported asset '${params.assetPath}'`
    }]
  };
}

/**
 * Registers all asset search tools
 */
export function registerAssetSearchTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerSearchAssetsTool(server, mcpUnity, logger);
  registerGetAssetDependenciesTool(server, mcpUnity, logger);
  registerReimportAssetTool(server, mcpUnity, logger);
}
