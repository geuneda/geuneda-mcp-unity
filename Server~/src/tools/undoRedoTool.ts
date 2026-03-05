import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// undo Tool
// ============================================================================

const undoToolName = 'undo';
const undoToolDescription = 'Performs an undo operation in the Unity Editor.';
const undoParamsSchema = z.object({});

/**
 * Registers the undo tool with the MCP server
 */
export function registerUndoTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${undoToolName}`);

  server.tool(
    undoToolName,
    undoToolDescription,
    undoParamsSchema.shape,
    async (params: z.infer<typeof undoParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${undoToolName}`, params);
        const result = await undoHandler(mcpUnity);
        logger.info(`Tool execution successful: ${undoToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${undoToolName}`, error);
        throw error;
      }
    }
  );
}

async function undoHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: undoToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to perform undo'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Undo performed successfully'
    }]
  };
}

// ============================================================================
// redo Tool
// ============================================================================

const redoToolName = 'redo';
const redoToolDescription = 'Performs a redo operation in the Unity Editor.';
const redoParamsSchema = z.object({});

/**
 * Registers the redo tool with the MCP server
 */
export function registerRedoTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${redoToolName}`);

  server.tool(
    redoToolName,
    redoToolDescription,
    redoParamsSchema.shape,
    async (params: z.infer<typeof redoParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${redoToolName}`, params);
        const result = await redoHandler(mcpUnity);
        logger.info(`Tool execution successful: ${redoToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${redoToolName}`, error);
        throw error;
      }
    }
  );
}

async function redoHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: redoToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to perform redo'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Redo performed successfully'
    }]
  };
}

// ============================================================================
// get_undo_history Tool
// ============================================================================

const getUndoHistoryToolName = 'get_undo_history';
const getUndoHistoryToolDescription = 'Gets the current undo history state, including the current group name.';
const getUndoHistoryParamsSchema = z.object({});

/**
 * Registers the get_undo_history tool with the MCP server
 */
export function registerGetUndoHistoryTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getUndoHistoryToolName}`);

  server.tool(
    getUndoHistoryToolName,
    getUndoHistoryToolDescription,
    getUndoHistoryParamsSchema.shape,
    async (params: z.infer<typeof getUndoHistoryParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${getUndoHistoryToolName}`, params);
        const result = await getUndoHistoryHandler(mcpUnity);
        logger.info(`Tool execution successful: ${getUndoHistoryToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getUndoHistoryToolName}`, error);
        throw error;
      }
    }
  );
}

async function getUndoHistoryHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: getUndoHistoryToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get undo history'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Undo history retrieved'
    }]
  };
}

// ============================================================================
// Combined registration
// ============================================================================

/**
 * Registers all Undo/Redo tools with the MCP server
 */
export function registerUndoRedoTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerUndoTool(server, mcpUnity, logger);
  registerRedoTool(server, mcpUnity, logger);
  registerGetUndoHistoryTool(server, mcpUnity, logger);
}
