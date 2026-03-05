import * as z from 'zod';
import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// get_animator_info Tool
// ============================================================================

const getAnimatorInfoToolName = 'get_animator_info';
const getAnimatorInfoToolDescription = 'Gets detailed information about an Animator component including parameters, layers, and states.';
const getAnimatorInfoParamsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy (alternative to instanceId)')
});

/**
 * Registers the get_animator_info tool with the MCP server
 */
export function registerGetAnimatorInfoTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getAnimatorInfoToolName}`);

  server.tool(
    getAnimatorInfoToolName,
    getAnimatorInfoToolDescription,
    getAnimatorInfoParamsSchema.shape,
    async (params: z.infer<typeof getAnimatorInfoParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${getAnimatorInfoToolName}`, params);
        const result = await getAnimatorInfoHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getAnimatorInfoToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getAnimatorInfoToolName}`, error);
        throw error;
      }
    }
  );
}

async function getAnimatorInfoHandler(mcpUnity: McpUnity, params: z.infer<typeof getAnimatorInfoParamsSchema>): Promise<CallToolResult> {
  validateGameObjectIdentifier(params);

  const response = await mcpUnity.sendRequest({
    method: getAnimatorInfoToolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get animator info'
    );
  }

  // Format the response
  let text = `Animator Info for controller: ${response.controllerName}\n\n`;

  text += `Parameters:\n`;
  if (response.parameters && Array.isArray(response.parameters)) {
    for (const param of response.parameters) {
      text += `  - ${param.name} (${param.type})`;
      if (param.type === 'Float') text += ` default: ${param.defaultFloat}`;
      else if (param.type === 'Int') text += ` default: ${param.defaultInt}`;
      else if (param.type === 'Bool') text += ` default: ${param.defaultBool}`;
      text += '\n';
    }
  }

  text += `\nLayers:\n`;
  if (response.layers && Array.isArray(response.layers)) {
    for (const layer of response.layers) {
      text += `  ${layer.name} (weight: ${layer.defaultWeight})\n`;
      if (layer.states && Array.isArray(layer.states)) {
        for (const state of layer.states) {
          text += `    - ${state.name} (speed: ${state.speed}, motion: ${state.motion})\n`;
        }
      }
    }
  }

  return {
    content: [{
      type: 'text',
      text: text
    }],
    data: {
      controllerName: response.controllerName,
      parameters: response.parameters,
      layers: response.layers
    }
  };
}

// ============================================================================
// set_animator_parameter Tool
// ============================================================================

const setAnimatorParameterToolName = 'set_animator_parameter';
const setAnimatorParameterToolDescription = 'Sets an Animator parameter value. Supports Float, Int, Bool, and Trigger types with auto-detection from the AnimatorController.';
const setAnimatorParameterParamsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy (alternative to instanceId)'),
  parameterName: z.string().describe('The name of the Animator parameter to set'),
  value: z.any().optional().describe('The value to set (number for Float/Int, boolean for Bool, not needed for Trigger)'),
  parameterType: z.enum(['Float', 'Int', 'Bool', 'Trigger']).optional().describe('The parameter type. Auto-detected from the AnimatorController if not specified')
});

/**
 * Registers the set_animator_parameter tool with the MCP server
 */
export function registerSetAnimatorParameterTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${setAnimatorParameterToolName}`);

  server.tool(
    setAnimatorParameterToolName,
    setAnimatorParameterToolDescription,
    setAnimatorParameterParamsSchema.shape,
    async (params: z.infer<typeof setAnimatorParameterParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${setAnimatorParameterToolName}`, params);
        const result = await setAnimatorParameterHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${setAnimatorParameterToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${setAnimatorParameterToolName}`, error);
        throw error;
      }
    }
  );
}

async function setAnimatorParameterHandler(mcpUnity: McpUnity, params: z.infer<typeof setAnimatorParameterParamsSchema>): Promise<CallToolResult> {
  validateGameObjectIdentifier(params);

  if (!params.parameterName || params.parameterName.trim() === '') {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'parameterName' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: setAnimatorParameterToolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      parameterName: params.parameterName,
      value: params.value,
      parameterType: params.parameterType
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to set animator parameter'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Animator parameter set successfully'
    }]
  };
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Validates that either instanceId or objectPath is provided
 */
function validateGameObjectIdentifier(params: { instanceId?: number; objectPath?: string }) {
  if ((params.instanceId === undefined || params.instanceId === null) &&
      (!params.objectPath || params.objectPath.trim() === '')) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'instanceId' or 'objectPath' must be provided"
    );
  }
}

/**
 * Registers all animation tools with the MCP server
 */
export function registerAnimationTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerGetAnimatorInfoTool(server, mcpUnity, logger);
  registerSetAnimatorParameterTool(server, mcpUnity, logger);
}
