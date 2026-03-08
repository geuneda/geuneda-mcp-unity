import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'invoke_method';
const toolDescription = 'Invokes a public method on a Component attached to a GameObject. Supports passing arguments and returns the method\'s return value.';
const paramsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy (alternative to instanceId)'),
  componentType: z.string().describe('The name of the component type (e.g. "Button", "PlayerController")'),
  methodName: z.string().describe('The name of the method to invoke'),
  arguments: z.array(z.any()).optional().describe('Arguments to pass to the method')
});

/**
 * Creates and registers the Invoke Method tool with the MCP server
 * This tool allows invoking public methods on Components attached to GameObjects
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerInvokeMethodTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

  // Register this tool with the MCP server
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

/**
 * Handles invoking a method on a Component attached to a GameObject in Unity
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  // Validate parameters - require either instanceId or objectPath
  if ((params.instanceId === undefined || params.instanceId === null) &&
      (!params.objectPath || params.objectPath.trim() === '')) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'instanceId' or 'objectPath' must be provided"
    );
  }

  if (!params.componentType) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'componentType' must be provided"
    );
  }

  if (!params.methodName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'methodName' must be provided"
    );
  }

  // Send request to Unity
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      componentType: params.componentType,
      methodName: params.methodName,
      arguments: params.arguments
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to invoke method '${params.methodName}' on component '${params.componentType}'`
    );
  }

  // Build response text with return value info if present
  let resultText = response.message || `Successfully invoked '${params.methodName}' on component '${params.componentType}'`;
  if (response.returnValue !== undefined && response.returnValue !== null) {
    resultText += `\nReturn value: ${JSON.stringify(response.returnValue)}`;
  }

  return {
    content: [{
      type: response.type,
      text: resultText
    }]
  };
}
