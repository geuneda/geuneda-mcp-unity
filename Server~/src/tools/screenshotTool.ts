import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'capture_screenshot';
const toolDescription = 'Captures a screenshot of the Unity Game View or Scene View and returns it as a base64-encoded PNG image';
const paramsSchema = z.object({
  viewType: z.enum(['game', 'scene']).optional().default('game').describe('The view to capture: "game" for Game View, "scene" for Scene View'),
  width: z.number().int().positive().optional().describe('Width of the screenshot in pixels (Scene View only, defaults to current view size)'),
  height: z.number().int().positive().optional().describe('Height of the screenshot in pixels (Scene View only, defaults to current view size)'),
  superSize: z.number().int().min(1).max(8).optional().default(1).describe('Super sampling size multiplier for Game View (1-8, default: 1)')
});

/**
 * Registers the Capture Screenshot tool with the MCP server
 */
export function registerCaptureScreenshotTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);

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

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const {
    viewType = 'game',
    width,
    height,
    superSize = 1
  } = params;

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      viewType,
      width,
      height,
      superSize
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to capture screenshot'
    );
  }

  return {
    content: [{
      type: "image",
      data: response.imageBase64,
      mimeType: response.mimeType || "image/png"
    }]
  };
}
