import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// enter_play_mode Tool
// ============================================================================

const enterPlayModeToolName = 'enter_play_mode';
const enterPlayModeToolDescription = 'Enters Play Mode in the Unity Editor.';
const enterPlayModeParamsSchema = z.object({});

/**
 * Registers the enter_play_mode tool with the MCP server
 */
export function registerEnterPlayModeTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${enterPlayModeToolName}`);

  server.tool(
    enterPlayModeToolName,
    enterPlayModeToolDescription,
    enterPlayModeParamsSchema.shape,
    async (params: z.infer<typeof enterPlayModeParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${enterPlayModeToolName}`, params);
        const result = await enterPlayModeHandler(mcpUnity);
        logger.info(`Tool execution successful: ${enterPlayModeToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${enterPlayModeToolName}`, error);
        throw error;
      }
    }
  );
}

async function enterPlayModeHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: enterPlayModeToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to enter Play Mode'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully entered Play Mode'
    }]
  };
}

// ============================================================================
// exit_play_mode Tool
// ============================================================================

const exitPlayModeToolName = 'exit_play_mode';
const exitPlayModeToolDescription = 'Exits Play Mode in the Unity Editor.';
const exitPlayModeParamsSchema = z.object({});

/**
 * Registers the exit_play_mode tool with the MCP server
 */
export function registerExitPlayModeTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${exitPlayModeToolName}`);

  server.tool(
    exitPlayModeToolName,
    exitPlayModeToolDescription,
    exitPlayModeParamsSchema.shape,
    async (params: z.infer<typeof exitPlayModeParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${exitPlayModeToolName}`, params);
        const result = await exitPlayModeHandler(mcpUnity);
        logger.info(`Tool execution successful: ${exitPlayModeToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${exitPlayModeToolName}`, error);
        throw error;
      }
    }
  );
}

async function exitPlayModeHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: exitPlayModeToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to exit Play Mode'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully exited Play Mode'
    }]
  };
}

// ============================================================================
// pause_editor Tool
// ============================================================================

const pauseEditorToolName = 'pause_editor';
const pauseEditorToolDescription = 'Pauses or unpauses the Unity Editor. Can toggle or set an explicit paused state.';
const pauseEditorParamsSchema = z.object({
  paused: z.boolean().optional().describe('Explicit paused state to set. If omitted, toggles the current state.')
});

/**
 * Registers the pause_editor tool with the MCP server
 */
export function registerPauseEditorTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${pauseEditorToolName}`);

  server.tool(
    pauseEditorToolName,
    pauseEditorToolDescription,
    pauseEditorParamsSchema.shape,
    async (params: z.infer<typeof pauseEditorParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${pauseEditorToolName}`, params);
        const result = await pauseEditorHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${pauseEditorToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${pauseEditorToolName}`, error);
        throw error;
      }
    }
  );
}

async function pauseEditorHandler(mcpUnity: McpUnity, params: z.infer<typeof pauseEditorParamsSchema>): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: pauseEditorToolName,
    params: {
      paused: params.paused
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to pause/unpause editor'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Editor pause state changed'
    }]
  };
}

// ============================================================================
// step_frame Tool
// ============================================================================

const stepFrameToolName = 'step_frame';
const stepFrameToolDescription = 'Advances a single frame while the Editor is paused in Play Mode.';
const stepFrameParamsSchema = z.object({});

/**
 * Registers the step_frame tool with the MCP server
 */
export function registerStepFrameTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${stepFrameToolName}`);

  server.tool(
    stepFrameToolName,
    stepFrameToolDescription,
    stepFrameParamsSchema.shape,
    async (params: z.infer<typeof stepFrameParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${stepFrameToolName}`, params);
        const result = await stepFrameHandler(mcpUnity);
        logger.info(`Tool execution successful: ${stepFrameToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${stepFrameToolName}`, error);
        throw error;
      }
    }
  );
}

async function stepFrameHandler(mcpUnity: McpUnity): Promise<CallToolResult> {
  const response = await mcpUnity.sendRequest({
    method: stepFrameToolName,
    params: {}
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to step frame'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'Successfully stepped one frame'
    }]
  };
}

// ============================================================================
// Combined registration
// ============================================================================

/**
 * Registers all Play Mode tools with the MCP server
 */
export function registerPlayModeTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerEnterPlayModeTool(server, mcpUnity, logger);
  registerExitPlayModeTool(server, mcpUnity, logger);
  registerPauseEditorTool(server, mcpUnity, logger);
  registerStepFrameTool(server, mcpUnity, logger);
}
