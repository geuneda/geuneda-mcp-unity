import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// ============================================================================
// CREATE SCRIPT TOOL
// ============================================================================

const createScriptToolName = 'create_script';
const createScriptToolDescription = 'Creates a new C# script file from a template (MonoBehaviour, ScriptableObject, or plain class)';
const createScriptParamsSchema = z.object({
  scriptName: z.string().describe('The name of the script (without .cs extension)'),
  namespaceName: z.string().optional().describe('Optional namespace for the script'),
  folderPath: z.string().optional().default('Assets/Scripts').describe('The folder path where the script will be created (default: "Assets/Scripts")'),
  scriptType: z.enum(['MonoBehaviour', 'ScriptableObject', 'plain']).optional().default('MonoBehaviour').describe('The type of script to create (default: "MonoBehaviour")')
});

/**
 * Registers the Create Script tool with the MCP server
 */
export function registerCreateScriptTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${createScriptToolName}`);

  server.tool(
    createScriptToolName,
    createScriptToolDescription,
    createScriptParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${createScriptToolName}`, params);
        const result = await createScriptHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${createScriptToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${createScriptToolName}`, error);
        throw error;
      }
    }
  );
}

async function createScriptHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.scriptName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'scriptName' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: createScriptToolName,
    params: {
      scriptName: params.scriptName,
      namespaceName: params.namespaceName,
      folderPath: params.folderPath ?? 'Assets/Scripts',
      scriptType: params.scriptType ?? 'MonoBehaviour'
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to create script'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully created script '${params.scriptName}'`
    }]
  };
}

// ============================================================================
// ATTACH SCRIPT TOOL
// ============================================================================

const attachScriptToolName = 'attach_script';
const attachScriptToolDescription = 'Attaches a script component to a GameObject by finding the MonoScript asset';
const attachScriptParamsSchema = z.object({
  instanceId: z.number().optional().describe('The instance ID of the GameObject'),
  objectPath: z.string().optional().describe('The path of the GameObject in the hierarchy (alternative to instanceId)'),
  scriptName: z.string().describe('The name of the script to attach (without .cs extension)')
});

/**
 * Registers the Attach Script tool with the MCP server
 */
export function registerAttachScriptTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${attachScriptToolName}`);

  server.tool(
    attachScriptToolName,
    attachScriptToolDescription,
    attachScriptParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${attachScriptToolName}`, params);
        const result = await attachScriptHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${attachScriptToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${attachScriptToolName}`, error);
        throw error;
      }
    }
  );
}

async function attachScriptHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if ((params.instanceId === undefined || params.instanceId === null) &&
      (!params.objectPath || params.objectPath.trim() === '')) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Either 'instanceId' or 'objectPath' must be provided"
    );
  }

  if (!params.scriptName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'scriptName' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: attachScriptToolName,
    params: {
      instanceId: params.instanceId,
      objectPath: params.objectPath,
      scriptName: params.scriptName
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to attach script'
    );
  }

  return {
    content: [{
      type: response.type,
      text: response.message || `Successfully attached script '${params.scriptName}'`
    }]
  };
}

// ============================================================================
// GET SCRIPT INFO TOOL
// ============================================================================

const getScriptInfoToolName = 'get_script_info';
const getScriptInfoToolDescription = 'Gets information about a script including its serialized fields and public methods';
const getScriptInfoParamsSchema = z.object({
  scriptName: z.string().describe('The name of the script to inspect (without .cs extension)')
});

/**
 * Registers the Get Script Info tool with the MCP server
 */
export function registerGetScriptInfoTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${getScriptInfoToolName}`);

  server.tool(
    getScriptInfoToolName,
    getScriptInfoToolDescription,
    getScriptInfoParamsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${getScriptInfoToolName}`, params);
        const result = await getScriptInfoHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${getScriptInfoToolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${getScriptInfoToolName}`, error);
        throw error;
      }
    }
  );
}

async function getScriptInfoHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  if (!params.scriptName) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      "Required parameter 'scriptName' must be provided"
    );
  }

  const response = await mcpUnity.sendRequest({
    method: getScriptInfoToolName,
    params: {
      scriptName: params.scriptName
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to get script info'
    );
  }

  // Format readable output
  let text = `Script: ${response.scriptName}\n`;
  text += `Path: ${response.scriptPath}\n`;
  text += `Class: ${response.className}\n`;
  text += `Base Class: ${response.baseClass}\n\n`;

  if (response.fields && Array.isArray(response.fields)) {
    text += `Fields:\n`;
    for (const field of response.fields) {
      const visibility = field.isPublic ? 'public' : 'private [SerializeField]';
      text += `  ${field.name} (${field.type}) - ${visibility}\n`;
    }
    text += '\n';
  }

  if (response.methods && Array.isArray(response.methods)) {
    text += `Methods:\n`;
    for (const method of response.methods) {
      const paramList = method.parameters
        ? method.parameters.map((p: any) => `${p.type} ${p.name}`).join(', ')
        : '';
      text += `  ${method.returnType} ${method.name}(${paramList})\n`;
    }
  }

  return {
    content: [{
      type: 'text',
      text: text
    }],
    data: {
      scriptName: response.scriptName,
      scriptPath: response.scriptPath,
      className: response.className,
      baseClass: response.baseClass,
      fields: response.fields,
      methods: response.methods
    }
  };
}

/**
 * Registers all script management tools
 */
export function registerScriptTools(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  registerCreateScriptTool(server, mcpUnity, logger);
  registerAttachScriptTool(server, mcpUnity, logger);
  registerGetScriptInfoTool(server, mcpUnity, logger);
}
