import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// physics_raycast Tool
// ============================================================================

const physicsRaycastToolName = 'physics_raycast';
const physicsRaycastToolDescription = 'Performs a physics raycast in the scene and returns hit information including the hit point, normal, distance, and the GameObject that was hit.';
const physicsRaycastParamsSchema = z.object({
  origin: z.object({
    x: z.number().describe('X component of the ray origin'),
    y: z.number().describe('Y component of the ray origin'),
    z: z.number().describe('Z component of the ray origin')
  }).describe('The starting point of the ray in world coordinates'),
  direction: z.object({
    x: z.number().describe('X component of the ray direction'),
    y: z.number().describe('Y component of the ray direction'),
    z: z.number().describe('Z component of the ray direction')
  }).describe('The direction of the ray (will be normalized)'),
  maxDistance: z.number().optional().describe('Maximum distance the ray should travel. Defaults to infinity'),
  layerMask: z.number().int().optional().describe('Layer mask to selectively ignore colliders. Defaults to -1 (all layers)')
});

/**
 * Registers the physics_raycast tool with the MCP server
 */
export function registerPhysicsRaycastTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${physicsRaycastToolName}`);

  server.tool(
    physicsRaycastToolName,
    physicsRaycastToolDescription,
    physicsRaycastParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${physicsRaycastToolName}`, params);
        const result = await physicsRaycastHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${physicsRaycastToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${physicsRaycastToolName}`, error);
        throw error;
      }
    }
  );
}

async function physicsRaycastHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.origin) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'origin' must be provided"
    );
  }

  if (!params.direction) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'direction' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: physicsRaycastToolName,
    params: {
      origin: params.origin,
      direction: params.direction,
      maxDistance: params.maxDistance,
      layerMask: params.layerMask
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to perform physics raycast'
    );
  }

  let text = response.message || '';

  if (response.hit) {
    text += `\nHit Point: (${response.point.x.toFixed(3)}, ${response.point.y.toFixed(3)}, ${response.point.z.toFixed(3)})`;
    text += `\nHit Normal: (${response.normal.x.toFixed(3)}, ${response.normal.y.toFixed(3)}, ${response.normal.z.toFixed(3)})`;
    text += `\nDistance: ${response.distance.toFixed(3)}`;
    text += `\nGameObject: ${response.gameObjectName}`;
    text += `\nPath: ${response.gameObjectPath}`;
  }

  return {
    content: [{
      type: 'text',
      text
    }],
    ...(response.hit ? {
      data: {
        hit: true,
        point: response.point,
        normal: response.normal,
        distance: response.distance,
        colliderName: response.colliderName,
        gameObjectName: response.gameObjectName,
        gameObjectPath: response.gameObjectPath
      }
    } : {
      data: {
        hit: false
      }
    })
  };
}

// ============================================================================
// get_physics_settings Tool
// ============================================================================

const getPhysicsSettingsToolName = 'get_physics_settings';
const getPhysicsSettingsToolDescription = 'Gets the current physics settings including gravity, solver iterations, and thresholds';
const getPhysicsSettingsParamsSchema = z.object({});

/**
 * Registers the get_physics_settings tool with the MCP server
 */
export function registerGetPhysicsSettingsTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getPhysicsSettingsToolName}`);

  server.tool(
    getPhysicsSettingsToolName,
    getPhysicsSettingsToolDescription,
    getPhysicsSettingsParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getPhysicsSettingsToolName}`, params);
        const result = await getPhysicsSettingsHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getPhysicsSettingsToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getPhysicsSettingsToolName}`, error);
        throw error;
      }
    }
  );
}

async function getPhysicsSettingsHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: getPhysicsSettingsToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get physics settings'
    );
  }

  let text = `Gravity: (${response.gravity.x}, ${response.gravity.y}, ${response.gravity.z})\n`;
  text += `Default Solver Iterations: ${response.defaultSolverIterations}\n`;
  text += `Default Solver Velocity Iterations: ${response.defaultSolverVelocityIterations}\n`;
  text += `Bounce Threshold: ${response.bounceThreshold}\n`;
  text += `Default Contact Offset: ${response.defaultContactOffset}\n`;
  text += `Sleep Threshold: ${response.sleepThreshold}\n`;
  text += `Simulation Mode: ${response.autoSimulation}`;

  return {
    content: [{
      type: 'text',
      text
    }],
    data: {
      gravity: response.gravity,
      defaultSolverIterations: response.defaultSolverIterations,
      defaultSolverVelocityIterations: response.defaultSolverVelocityIterations,
      bounceThreshold: response.bounceThreshold,
      defaultContactOffset: response.defaultContactOffset,
      sleepThreshold: response.sleepThreshold,
      autoSimulation: response.autoSimulation
    }
  };
}

// ============================================================================
// Combined Registration
// ============================================================================

/**
 * Registers all physics tools with the MCP server
 */
export function registerPhysicsTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerPhysicsRaycastTool(server, mcpUnity, logger);
  registerGetPhysicsSettingsTool(server, mcpUnity, logger);
}
