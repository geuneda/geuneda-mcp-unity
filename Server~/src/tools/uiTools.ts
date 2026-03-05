import * as z from 'zod';
import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// create_ui_element Tool
// ============================================================================

const createUIElementToolName = 'create_ui_element';
const createUIElementToolDescription = 'Creates a UI element (Canvas, Button, Text, Image, Panel, Slider, Toggle, InputField, Dropdown, ScrollView) with automatic Canvas and EventSystem setup.';
const createUIElementParamsSchema = z.object({
  elementType: z.enum(['Canvas', 'Button', 'Text', 'Image', 'Panel', 'Slider', 'Toggle', 'InputField', 'Dropdown', 'ScrollView'])
    .describe('The type of UI element to create'),
  parentPath: z.string().optional().describe('The hierarchy path of the parent GameObject. Defaults to Canvas if not specified'),
  name: z.string().optional().describe('Custom name for the created element')
});

/**
 * Registers the create_ui_element tool with the MCP server
 */
export function registerCreateUIElementTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${createUIElementToolName}`);

  server.tool(
    createUIElementToolName,
    createUIElementToolDescription,
    createUIElementParamsSchema.shape,
    async (params: z.infer<typeof createUIElementParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${createUIElementToolName}`, params);
        const result = await createUIElementHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${createUIElementToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${createUIElementToolName}`, error);
        throw error;
      }
    }
  );
}

async function createUIElementHandler(mcpUnity: McpUnity, params: z.infer<typeof createUIElementParamsSchema>): Promise<CallToolResult> {
  if (!params.elementType) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'elementType' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: createUIElementToolName,
    params: {
      elementType: params.elementType,
      parentPath: params.parentPath,
      name: params.name
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to create UI element'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Created UI element '${params.elementType}'`
    }]
  };
}

// ============================================================================
// modify_ui_element Tool
// ============================================================================

const modifyUIElementToolName = 'modify_ui_element';
const modifyUIElementToolDescription = 'Modifies UI element properties such as text, fontSize, color, anchoredPosition, sizeDelta, and enabled state.';
const modifyUIElementParamsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the UI GameObject'),
  objectPath: z.string().optional().describe('The path of the UI GameObject in the hierarchy (alternative to instanceId)'),
  properties: z.object({
    text: z.string().optional().describe('Text content for Text components'),
    fontSize: z.number().int().optional().describe('Font size for Text components'),
    color: z.object({
      r: z.number().min(0).max(1).describe('Red component (0-1)'),
      g: z.number().min(0).max(1).describe('Green component (0-1)'),
      b: z.number().min(0).max(1).describe('Blue component (0-1)'),
      a: z.number().min(0).max(1).optional().describe('Alpha component (0-1)')
    }).optional().describe('Color for Graphic components (Image, Text, etc.)'),
    anchoredPosition: z.object({
      x: z.number().describe('X position'),
      y: z.number().describe('Y position')
    }).optional().describe('Anchored position for RectTransform'),
    sizeDelta: z.object({
      x: z.number().describe('Width'),
      y: z.number().describe('Height')
    }).optional().describe('Size delta for RectTransform'),
    enabled: z.boolean().optional().describe('Whether the GameObject is active')
  }).describe('Properties to modify on the UI element')
});

/**
 * Registers the modify_ui_element tool with the MCP server
 */
export function registerModifyUIElementTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${modifyUIElementToolName}`);

  server.tool(
    modifyUIElementToolName,
    modifyUIElementToolDescription,
    modifyUIElementParamsSchema.shape,
    async (params: z.infer<typeof modifyUIElementParamsSchema>) => {
      try {
        logger.info(`Executing tool: ${modifyUIElementToolName}`, params);
        const result = await modifyUIElementHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${modifyUIElementToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${modifyUIElementToolName}`, error);
        throw error;
      }
    }
  );
}

async function modifyUIElementHandler(mcpUnity: McpUnity, params: z.infer<typeof modifyUIElementParamsSchema>): Promise<CallToolResult> {
  validateGameObjectIdentifier(params);

  if (!params.properties || Object.keys(params.properties).length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'properties' must be provided and contain at least one property"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: modifyUIElementToolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      properties: params.properties
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to modify UI element'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || 'UI element modified successfully'
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
 * Registers all UI tools with the MCP server
 */
export function registerUITools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerCreateUIElementTool(server, mcpUnity, logger);
  registerModifyUIElementTool(server, mcpUnity, logger);
}
