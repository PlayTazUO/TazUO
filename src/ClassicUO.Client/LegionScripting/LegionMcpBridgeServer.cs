using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.LegionScripting.ApiClasses;
using ClassicUO.Utility.Logging;

namespace ClassicUO.LegionScripting;

/// <summary>
/// MCP JSON-RPC server for Codex/LLM clients.
/// Hosted via the map web server endpoint (/api/mcp).
/// </summary>
internal sealed class LegionMcpBridgeServer : IDisposable
{
    private const string JsonRpcVersion = "2.0";
    private const string DefaultProtocolVersion = "2025-03-26";

    private const string ToolInvoke = "legion_api_invoke";
    private const string ToolMethods = "legion_api_methods";
    private const string ToolScreenshot = "legion_screenshot";
    private const string ToolOpenGumps = "legion_open_gumps";
    private const string ToolGumpClickButton = "legion_gump_click_button";
    private const string ToolContainerItems = "legion_container_items";
    private const string ToolItemHoverText = "legion_item_hover_text";
    private const string ToolHealth = "legion_health";
    private const string ToolTileInfo = "legion_tile_info";
    private const string ToolGetTile = "legion_get_tile";
    private const string ToolRegionInfo = "legion_region_info";
    private const string ToolTilesInArea = "legion_tiles_in_area";
    private const string ToolStaticsInArea = "legion_statics_in_area";
    private const string ToolMultisInArea = "legion_multis_in_area";
    private const string ToolHousesInArea = "legion_houses_in_area";
    private const string ToolCanPlaceHouse = "legion_can_place_house";
    private const string ToolMarkTile = "legion_mark_tile";
    private const string ToolRemoveMarkedTile = "legion_remove_marked_tile";

    private static readonly string[] _supportedProtocolVersions = ["2025-06-18", "2025-03-26"];
    private static readonly Lazy<LegionMcpBridgeServer> _instance = new(() => new LegionMcpBridgeServer());
    private static readonly HashSet<string> _blockedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(LegionAPI.Dispose)
    };

    private readonly LegionAPI _api = new(new CSharpCallbackChannel(), null);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private LegionMcpBridgeServer()
    {
    }

    public static LegionMcpBridgeServer Instance => _instance.Value;

    /// <summary>
    /// Handles an MCP JSON-RPC payload.
    /// Returns an empty string for notifications/responses that should produce HTTP 202 with no body.
    /// </summary>
    public string HandleRequestJson(string payload, bool running, int port, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            object invalid = CreateJsonRpcErrorResponse(null, -32600, "Invalid Request", "Empty request payload.");
            return JsonSerializer.Serialize(invalid, _serializerOptions);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            object response = HandleParsedPayload(document.RootElement, running, port, endpoint);
            if (response == null)
                return string.Empty;

            return JsonSerializer.Serialize(response, _serializerOptions);
        }
        catch (JsonException ex)
        {
            Log.Error($"MCP parse error: {ex.Message}");
            object error = CreateJsonRpcErrorResponse(null, -32700, "Parse error", ex.Message);
            return JsonSerializer.Serialize(error, _serializerOptions);
        }
        catch (Exception ex)
        {
            Log.Error($"MCP server error: {ex}");
            object error = CreateJsonRpcErrorResponse(null, -32603, "Internal error", ex.Message);
            return JsonSerializer.Serialize(error, _serializerOptions);
        }
    }

    private object HandleParsedPayload(JsonElement root, bool running, int port, string endpoint)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return HandleBatchRequest(root, running, port, endpoint);

        if (root.ValueKind != JsonValueKind.Object)
            return CreateJsonRpcErrorResponse(null, -32600, "Invalid Request", "Request must be a JSON object.");

        return HandleJsonRpcMessage(root, running, port, endpoint);
    }

    private object HandleBatchRequest(JsonElement batch, bool running, int port, string endpoint)
    {
        if (batch.GetArrayLength() == 0)
            return CreateJsonRpcErrorResponse(null, -32600, "Invalid Request", "Batch request cannot be empty.");

        var responses = new List<object>();

        foreach (JsonElement item in batch.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                responses.Add(CreateJsonRpcErrorResponse(null, -32600, "Invalid Request", "Batch item must be a JSON object."));
                continue;
            }

            object response = HandleJsonRpcMessage(item, running, port, endpoint);
            if (response != null)
                responses.Add(response);
        }

        return responses.Count == 0 ? null : responses;
    }

    private object HandleJsonRpcMessage(JsonElement request, bool running, int port, string endpoint)
    {
        bool hasMethod = TryGetProperty(request, "method", out JsonElement methodElement) && methodElement.ValueKind == JsonValueKind.String;
        bool hasResult = TryGetProperty(request, "result", out _);
        bool hasError = TryGetProperty(request, "error", out _);
        bool hasId = TryGetProperty(request, "id", out JsonElement idElement);

        // JSON-RPC response objects sent by client (accepted as no-op per streamable-http).
        if (!hasMethod && (hasResult || hasError))
            return null;

        object requestId = null;
        if (hasId && !TryConvertRequestId(idElement, out requestId, out string idError))
            return CreateJsonRpcErrorResponse(null, -32600, "Invalid Request", idError);

        bool isNotification = !hasId;

        if (!TryGetProperty(request, "jsonrpc", out JsonElement jsonRpcElement) ||
            jsonRpcElement.ValueKind != JsonValueKind.String ||
            !string.Equals(jsonRpcElement.GetString(), JsonRpcVersion, StringComparison.Ordinal))
        {
            return isNotification ? null : CreateJsonRpcErrorResponse(requestId, -32600, "Invalid Request", "jsonrpc must be '2.0'.");
        }

        if (!hasMethod)
            return isNotification ? null : CreateJsonRpcErrorResponse(requestId, -32600, "Invalid Request", "Missing method.");

        string method = methodElement.GetString()?.Trim() ?? string.Empty;
        JsonElement paramsElement = default;
        bool hasParams = TryGetProperty(request, "params", out paramsElement) && paramsElement.ValueKind != JsonValueKind.Null;

        if (hasParams && paramsElement.ValueKind != JsonValueKind.Object)
            return isNotification ? null : CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "params must be an object.");

        return method switch
        {
            "initialize" => isNotification
                ? null
                : CreateJsonRpcSuccessResponse(requestId, BuildInitializeResult(hasParams ? paramsElement : default)),
            "ping" => isNotification ? null : CreateJsonRpcSuccessResponse(requestId, new Dictionary<string, object>()),
            "tools/list" => isNotification ? null : CreateJsonRpcSuccessResponse(requestId, BuildToolsListResult()),
            "tools/call" => isNotification
                ? null
                : HandleToolCall(requestId, hasParams ? paramsElement : default, running, port, endpoint),
            "resources/list" => isNotification ? null : CreateJsonRpcSuccessResponse(requestId, new { resources = Array.Empty<object>() }),
            "resources/templates/list" => isNotification ? null : CreateJsonRpcSuccessResponse(requestId, new { resourceTemplates = Array.Empty<object>() }),
            "prompts/list" => isNotification ? null : CreateJsonRpcSuccessResponse(requestId, new { prompts = Array.Empty<object>() }),
            "notifications/initialized" => null,
            "notifications/cancelled" => null,
            _ => isNotification ? null : CreateJsonRpcErrorResponse(requestId, -32601, "Method not found", $"Unknown method '{method}'.")
        };
    }

    private object BuildInitializeResult(JsonElement paramsElement)
    {
        string requestedProtocolVersion = TryGetString(paramsElement, "protocolVersion");
        string protocolVersion = NegotiateProtocolVersion(requestedProtocolVersion);

        return new
        {
            protocolVersion,
            capabilities = new
            {
                tools = new
                {
                    listChanged = false
                }
            },
            serverInfo = new
            {
                name = "tazuo-legion-mcp",
                version = "1.0.0"
            },
            instructions = "Use tools/list then tools/call. For Legion API methods, call legion_api_methods first, then legion_api_invoke."
        };
    }

    private object BuildToolsListResult()
    {
        object[] tools =
        [
            new
            {
                name = ToolHealth,
                title = "Legion MCP Health",
                description = "Returns health/status metadata for the in-game MCP endpoint.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>(),
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolMethods,
                title = "List Legion API Methods",
                description = "Lists invocable LegionAPI methods and overload signatures.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["filter"] = new
                        {
                            type = "string",
                            description = "Optional case-insensitive substring filter for method names/signatures."
                        },
["includeSignatures"] = new
{
type = "boolean",
description = "Whether to include full overload signatures.",
@default = true
},
["includeDefinitions"] = new
{
type = "boolean",
description = "Whether to include structured overload definitions with parameters, defaults, and invoke templates.",
@default = true
}
},
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolInvoke,
                title = "Invoke Legion API Method",
                description = "Invokes any public LegionAPI method by name. Use legion_api_methods to discover signatures first.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["method"] = new
                        {
                            type = "string",
                            description = "LegionAPI method name (case-insensitive)."
                        },
                        ["args"] = new
                        {
                            description = "Positional arguments (array) or named arguments (object).",
                            anyOf = new object[]
                            {
                                new { type = "array" },
                                new { type = "object" }
                            }
                        },
                        ["kwargs"] = new
                        {
                            type = "object",
                            description = "Optional named arguments merged with args object."
                        }
                    },
                    required = new[] { "method" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolOpenGumps,
                title = "List Open Gumps",
                description = "Returns metadata for currently visible open gumps.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>(),
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolGumpClickButton,
                title = "Click Gump Button",
                description = "Clicks an in-game/server gump button or a LegionScript API-created UI button.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["button"] = new
                        {
                            type = "integer",
                            description = "Button ID. For server gumps this is the reply button; for LegionScript UI it matches ButtonID/ButtonParameter."
                        },
                        ["buttonId"] = new
                        {
                            type = "integer",
                            description = "Alias for button."
                        },
                        ["gumpId"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Optional server or local gump serial/id. Accepts number, decimal string, or 0x-prefixed hex string."
                        },
                        ["gump"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Alias for gumpId."
                        },
                        ["kind"] = new
                        {
                            type = "string",
                            @enum = new[] { "auto", "server", "legion" },
                            description = "Click target kind. Use server for in-game gumps and legion for LegionScript API buttons. Defaults auto."
                        },
                        ["controlType"] = new
                        {
                            type = "string",
                            @enum = new[] { "any", "button", "niceButton" },
                            description = "LegionScript control type filter. Defaults any."
                        },
                        ["controlIndex"] = new
                        {
                            type = "integer",
                            description = "Zero-based index when multiple matching LegionScript controls exist. Defaults 0."
                        },
                        ["text"] = new
                        {
                            type = "string",
                            description = "Optional LegionScript button text filter."
                        },
                        ["switches"] = new
                        {
                            type = "array",
                            items = new { type = "integer" },
                            description = "Optional server gump switches for server replies."
                        },
                        ["entries"] = new
                        {
                            type = "array",
                            description = "Optional server gump text entries, as [index, text] pairs."
                        }
                    },
                    required = new[] { "button" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolContainerItems,
                title = "List Container Items",
                description = "Returns known item contents for a container serial, or the player backpack when no serial is provided.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["containerSerial"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Optional container serial. Accepts a number, decimal string, or 0x-prefixed hex string. Defaults to the player backpack."
                        },
                        ["serial"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Alias for containerSerial."
                        },
                        ["backpack"] = new
                        {
                            type = "boolean",
                            description = "When true, ignores containerSerial and uses the player backpack."
                        },
                        ["recursive"] = new
                        {
                            type = "boolean",
                            description = "When true, includes items in nested sub-containers."
                        },
                        ["includeHoverText"] = new
                        {
                            type = "boolean",
                            description = "When true, includes cached item hover/tooltip text for each item."
                        },
                        ["waitHoverText"] = new
                        {
                            type = "boolean",
                            description = "When true with includeHoverText, waits for missing hover text to arrive from the server."
                        },
                        ["hoverTextTimeout"] = new
                        {
                            type = "integer",
                            description = "Hover text wait timeout in seconds. Defaults to 10."
                        }
                    },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolItemHoverText,
                title = "Get Item Hover Text",
                description = "Returns item hover/tooltip text (Object Property List) for an item serial.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["serial"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Item serial. Accepts number, decimal string, or 0x-prefixed hex string."
                        },
                        ["wait"] = new
                        {
                            type = "boolean",
                            description = "When true, waits for missing hover text to arrive from the server."
                        },
                        ["timeout"] = new
                        {
                            type = "integer",
                            description = "Wait timeout in seconds. Defaults to 10."
                        }
                    },
                    required = new[] { "serial" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolTileInfo,
                title = "Inspect Map Tile",
                description = "Returns detailed land/static/multi flags for one map tile.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "Map X coordinate." },
                        ["y"] = new { type = "integer", description = "Map Y coordinate." }
                    },
                    required = new[] { "x", "y" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolRegionInfo,
                title = "Inspect Tile Region",
                description = "Returns region/no-housing metadata available to the client for one map tile.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "Map X coordinate." },
                        ["y"] = new { type = "integer", description = "Map Y coordinate." }
                    },
                    required = new[] { "x", "y" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolGetTile,
                title = "Get Map Tile",
                description = "Returns the wrapped game object at one map tile, including land/static/multi type-specific properties when available.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "Map X coordinate." },
                        ["y"] = new { type = "integer", description = "Map Y coordinate." }
                    },
                    required = new[] { "x", "y" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolTilesInArea,
                title = "Inspect Map Tiles In Area",
                description = "Returns detailed land/static/multi flags for a rectangular tile area.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x1"] = new { type = "integer", description = "First corner X coordinate." },
                        ["y1"] = new { type = "integer", description = "First corner Y coordinate." },
                        ["x2"] = new { type = "integer", description = "Second corner X coordinate." },
                        ["y2"] = new { type = "integer", description = "Second corner Y coordinate." },
                        ["maxTiles"] = new { type = "integer", description = "Maximum tiles returned. Defaults to 4096." }
                    },
                    required = new[] { "x1", "y1", "x2", "y2" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolStaticsInArea,
                title = "List Statics In Area",
                description = "Returns static objects and their tile flags within a rectangular map area.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x1"] = new { type = "integer", description = "First corner X coordinate." },
                        ["y1"] = new { type = "integer", description = "First corner Y coordinate." },
                        ["x2"] = new { type = "integer", description = "Second corner X coordinate." },
                        ["y2"] = new { type = "integer", description = "Second corner Y coordinate." }
                    },
                    required = new[] { "x1", "y1", "x2", "y2" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolMultisInArea,
                title = "List Multis In Area",
                description = "Returns known multi components in a rectangular map area, including house serial and multi metadata.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x1"] = new { type = "integer", description = "First corner X coordinate." },
                        ["y1"] = new { type = "integer", description = "First corner Y coordinate." },
                        ["x2"] = new { type = "integer", description = "Second corner X coordinate." },
                        ["y2"] = new { type = "integer", description = "Second corner Y coordinate." }
                    },
                    required = new[] { "x1", "y1", "x2", "y2" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolHousesInArea,
                title = "List Houses In Area",
                description = "Returns grouped known house/multi data intersecting a rectangular area.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x1"] = new { type = "integer", description = "First corner X coordinate." },
                        ["y1"] = new { type = "integer", description = "First corner Y coordinate." },
                        ["x2"] = new { type = "integer", description = "Second corner X coordinate." },
                        ["y2"] = new { type = "integer", description = "Second corner Y coordinate." },
                        ["clearance"] = new { type = "integer", description = "Optional extra tile radius around the area." }
                    },
                    required = new[] { "x1", "y1", "x2", "y2" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolCanPlaceHouse,
                title = "Estimate House Placement",
                description = "Client-side estimate of whether a rectangular house footprint can be placed. Returns blockers and reasons.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "North-west footprint X coordinate." },
                        ["y"] = new { type = "integer", description = "North-west footprint Y coordinate." },
                        ["width"] = new { type = "integer", description = "House footprint width in tiles." },
                        ["depth"] = new { type = "integer", description = "House footprint depth in tiles." },
                        ["direction"] = new { type = "string", description = "Optional facing direction label. Defaults to south." },
                        ["frontClearance"] = new { type = "integer", description = "Front clearance in tiles. Defaults to 6." },
                        ["backClearance"] = new { type = "integer", description = "Back clearance in tiles. Defaults to 5." },
                        ["sideClearance"] = new { type = "integer", description = "Left/right side clearance in tiles. Defaults to 1." },
                        ["maxZDelta"] = new { type = "integer", description = "Maximum allowed land z range. Defaults to 0." },
                        ["includeSteps"] = new { type = "boolean", description = "Labels the first front-clearance row as step clearance. Defaults to true." },
                        ["allowSmallPlants"] = new { type = "boolean", description = "Allows small vegetation/foliage/background statics. Defaults to true." }
                    },
                    required = new[] { "x", "y", "width", "depth" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolMarkTile,
                title = "Mark Map Tile",
                description = "Marks a map tile using either a UO hue ID or a true RGBA HTML color.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "Map X coordinate." },
                        ["y"] = new { type = "integer", description = "Map Y coordinate." },
                        ["hue"] = new { type = "integer", description = "Optional UO hue ID. Used when color is omitted." },
                        ["color"] = new { type = "string", description = "Optional HTML color: #RRGGBB or #RRGGBBAA." },
                        ["map"] = new { type = "integer", description = "Optional map index. Defaults to current map." }
                    },
                    required = new[] { "x", "y" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolRemoveMarkedTile,
                title = "Remove Marked Map Tile",
                description = "Removes a map tile marker.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["x"] = new { type = "integer", description = "Map X coordinate." },
                        ["y"] = new { type = "integer", description = "Map Y coordinate." },
                        ["map"] = new { type = "integer", description = "Optional map index. Defaults to current map." }
                    },
                    required = new[] { "x", "y" },
                    additionalProperties = false
                }
            },
            new
            {
                name = ToolScreenshot,
                title = "Capture Screenshot",
                description = "Captures a screenshot (full, region, or gump) and returns metadata plus image content.",
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["mode"] = new
                        {
                            type = "string",
                            @enum = new[] { "full", "region", "gump" },
                            description = "Capture mode."
                        },
                        ["path"] = new
                        {
                            type = "string",
                            description = "Optional output path."
                        },
                        ["x"] = new
                        {
                            type = "integer",
                            description = "Region X (required for region mode)."
                        },
                        ["y"] = new
                        {
                            type = "integer",
                            description = "Region Y (required for region mode)."
                        },
                        ["width"] = new
                        {
                            type = "integer",
                            description = "Region width (required for region mode)."
                        },
                        ["height"] = new
                        {
                            type = "integer",
                            description = "Region height (required for region mode)."
                        },
                        ["gumpId"] = new
                        {
                            anyOf = new object[]
                            {
                                new { type = "integer" },
                                new { type = "string" }
                            },
                            description = "Gump serial/id for gump mode."
                        },
                        ["padding"] = new
                        {
                            type = "integer",
                            description = "Optional padding around gump bounds."
                        }
                    },
                    additionalProperties = false
                }
            }
        ];

        return new
        {
            tools
        };
    }

    private object HandleToolCall(object requestId, JsonElement paramsElement, bool running, int port, string endpoint)
    {
        if (paramsElement.ValueKind != JsonValueKind.Object)
            return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "tools/call requires object params.");

        string toolName = TryGetString(paramsElement, "name");
        if (string.IsNullOrWhiteSpace(toolName))
            return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "tools/call requires a tool name.");

        JsonElement argumentsElement = default;
        bool hasArguments = TryGetProperty(paramsElement, "arguments", out argumentsElement) && argumentsElement.ValueKind != JsonValueKind.Null;
        if (hasArguments && argumentsElement.ValueKind != JsonValueKind.Object)
            return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "tools/call arguments must be an object.");

        return toolName switch
        {
            ToolHealth => CreateJsonRpcSuccessResponse(
                requestId,
                BuildToolResult(
                    new
                    {
                        status = "ok",
                        running,
                        port,
                        endpoint,
                        api = nameof(LegionAPI)
                    })),
            ToolMethods => HandleListMethodsTool(requestId, hasArguments ? argumentsElement : default),
            ToolInvoke => HandleInvokeTool(requestId, hasArguments ? argumentsElement : default),
            ToolOpenGumps => CreateJsonRpcSuccessResponse(requestId, BuildToolResult(_api.GetOpenGumpInfo())),
            ToolGumpClickButton => HandleGumpClickButtonTool(requestId, hasArguments ? argumentsElement : default),
            ToolContainerItems => HandleContainerItemsTool(requestId, hasArguments ? argumentsElement : default),
            ToolItemHoverText => HandleItemHoverTextTool(requestId, hasArguments ? argumentsElement : default),
            ToolTileInfo => HandleTileInfoTool(requestId, hasArguments ? argumentsElement : default),
            ToolGetTile => HandleGetTileTool(requestId, hasArguments ? argumentsElement : default),
            ToolRegionInfo => HandleRegionInfoTool(requestId, hasArguments ? argumentsElement : default),
            ToolTilesInArea => HandleTilesInAreaTool(requestId, hasArguments ? argumentsElement : default),
            ToolStaticsInArea => HandleStaticsInAreaTool(requestId, hasArguments ? argumentsElement : default),
            ToolMultisInArea => HandleMultisInAreaTool(requestId, hasArguments ? argumentsElement : default),
            ToolHousesInArea => HandleHousesInAreaTool(requestId, hasArguments ? argumentsElement : default),
            ToolCanPlaceHouse => HandleCanPlaceHouseTool(requestId, hasArguments ? argumentsElement : default),
            ToolMarkTile => HandleMarkTileTool(requestId, hasArguments ? argumentsElement : default),
            ToolRemoveMarkedTile => HandleRemoveMarkedTileTool(requestId, hasArguments ? argumentsElement : default),
            ToolScreenshot => HandleScreenshotTool(requestId, hasArguments ? argumentsElement : default),
            _ => CreateJsonRpcErrorResponse(requestId, -32601, "Method not found", $"Unknown tool '{toolName}'.")
        };
    }

    private object HandleListMethodsTool(object requestId, JsonElement argsElement)
    {
        string filter = TryGetString(argsElement, "filter");
        bool includeSignatures = true;

        if (TryGetProperty(argsElement, "includeSignatures", out JsonElement includeElement))
        {
            if (includeElement.ValueKind == JsonValueKind.True || includeElement.ValueKind == JsonValueKind.False)
                includeSignatures = includeElement.GetBoolean();
            else if (includeElement.ValueKind == JsonValueKind.String && bool.TryParse(includeElement.GetString(), out bool parsed))
                includeSignatures = parsed;
            else
                return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("includeSignatures must be a boolean."));
        }

        bool includeDefinitions = true;

        if (TryGetProperty(argsElement, "includeDefinitions", out JsonElement includeDefinitionsElement) ||
            TryGetProperty(argsElement, "include_definitions", out includeDefinitionsElement))
        {
            if (includeDefinitionsElement.ValueKind == JsonValueKind.True || includeDefinitionsElement.ValueKind == JsonValueKind.False)
                includeDefinitions = includeDefinitionsElement.GetBoolean();
            else if (includeDefinitionsElement.ValueKind == JsonValueKind.String && bool.TryParse(includeDefinitionsElement.GetString(), out bool parsed))
                includeDefinitions = parsed;
            else
                return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("includeDefinitions must be a boolean."));
        }

        MethodInfo[] methods = GetInvocableLegionMethods();
        var grouped = methods
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                name = g.Key,
                overloadCount = g.Count(),
                overloads = includeSignatures ? g.Select(BuildMethodSignature).ToArray() : Array.Empty<string>(),
                definitions = includeDefinitions ? g.Select(BuildMethodDefinition).ToArray() : Array.Empty<object>()
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            grouped = grouped
                .Where(g =>
                    g.name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    g.overloads.Any(o => o.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var payload = new Dictionary<string, object>
        {
            ["totalMethodNames"] = grouped.Count,
            ["methods"] = grouped
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleInvokeTool(object requestId, JsonElement argsElement)
    {
        string methodName = TryGetString(argsElement, "method");
        if (string.IsNullOrWhiteSpace(methodName))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_api_invoke requires a non-empty 'method' string."));

        JsonElement invokeArgs = default;
        JsonElement kwargs = default;

        bool hasInvokeArgs = TryGetProperty(argsElement, "args", out invokeArgs);
        bool hasKwargs = TryGetProperty(argsElement, "kwargs", out kwargs);

        if (hasKwargs && kwargs.ValueKind != JsonValueKind.Object && kwargs.ValueKind != JsonValueKind.Null)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("kwargs must be an object."));

        if (!TryInvokeMethod(methodName, hasInvokeArgs ? invokeArgs : default, hasKwargs ? kwargs : default, out object result, out string error))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult(error));

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(NormalizeResult(result)));
    }

    private object HandleGumpClickButtonTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "button", out int button) &&
            !TryGetInt(argsElement, "buttonId", out button) &&
            !TryGetInt(argsElement, "button_id", out button))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_gump_click_button requires integer 'button'."));
        }

        uint gumpId = uint.MaxValue;

        if (!TryGetUInt(argsElement, "gumpId", out gumpId) &&
            !TryGetUInt(argsElement, "gump_id", out gumpId) &&
            !TryGetUInt(argsElement, "gump", out gumpId) &&
            !TryGetUInt(argsElement, "serial", out gumpId))
        {
            gumpId = uint.MaxValue;
        }

        string kind = TryGetString(argsElement, "kind") ?? TryGetString(argsElement, "mode") ?? "auto";
        string controlType = TryGetString(argsElement, "controlType") ?? TryGetString(argsElement, "control_type") ?? "any";
        string text = TryGetString(argsElement, "text");
        int controlIndex = 0;

        if (TryGetInt(argsElement, "controlIndex", out int parsedControlIndex) ||
            TryGetInt(argsElement, "control_index", out parsedControlIndex) ||
            TryGetInt(argsElement, "index", out parsedControlIndex))
        {
            controlIndex = parsedControlIndex;
        }

        if (!TryGetIntArray(argsElement, out int[] switches, out string switchesError, "switches"))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult(switchesError));

        if (!TryGetLooseArray(argsElement, out List<object> entries, out string entriesError, "entries"))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult(entriesError));

        ApiGumpButtonClickResult result = _api.ClickGumpButton(
            button,
            gumpId,
            kind,
            controlType,
            controlIndex,
            text,
            switches,
            entries);

        if (result == null)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Gump button click returned no result."));

        if (!result.Success)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult(result.Error, details: result));

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(result));
    }

    private object HandleContainerItemsTool(object requestId, JsonElement argsElement)
    {
        bool recursive = TryGetBool(argsElement, "recursive", out bool parsedRecursive) && parsedRecursive;
        bool useBackpack = TryGetBool(argsElement, "backpack", out bool parsedBackpack) && parsedBackpack;
        bool includeHoverText = HasTruthyArgument(
            argsElement,
            "includeHoverText",
            "include_hover_text",
            "includeProperties",
            "include_properties");
        bool waitHoverText = HasTruthyArgument(argsElement, "waitHoverText", "wait_hover_text");
        int hoverTextTimeout = GetTimeoutSeconds(argsElement, 10, "hoverTextTimeout", "hover_text_timeout", "timeout");
        bool hasContainerSerial =
            TryGetUInt(argsElement, "containerSerial", out uint containerSerial) ||
            TryGetUInt(argsElement, "container_serial", out containerSerial) ||
            TryGetUInt(argsElement, "serial", out containerSerial);

        string source = "serial";
        if (!hasContainerSerial || useBackpack)
        {
            containerSerial = _api.Backpack;
            source = "backpack";
        }

        if (containerSerial == 0)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Player backpack was not found."));

        ApiItem[] items = _api.ItemsInContainer(containerSerial, recursive) ?? Array.Empty<ApiItem>();
        List<Dictionary<string, object>> itemPayloads = items
            .Select(item => BuildItemPayload(item, includeHoverText ? GetItemHoverText(item.Serial, waitHoverText, hoverTextTimeout) : null))
            .ToList();

        var payload = new Dictionary<string, object>
        {
            ["containerSerial"] = containerSerial,
            ["containerSerialHex"] = $"0x{containerSerial:X8}",
            ["source"] = source,
            ["recursive"] = recursive,
            ["includeHoverText"] = includeHoverText,
            ["waitHoverText"] = waitHoverText,
            ["hoverTextTimeout"] = hoverTextTimeout,
            ["count"] = items.Length,
            ["items"] = itemPayloads
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleItemHoverTextTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetUInt(argsElement, "serial", out uint serial) &&
            !TryGetUInt(argsElement, "itemSerial", out serial) &&
            !TryGetUInt(argsElement, "item_serial", out serial))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_item_hover_text requires item 'serial'."));
        }

        bool wait = TryGetBool(argsElement, "wait", out bool parsedWait) && parsedWait;
        int timeout = GetTimeoutSeconds(argsElement, 10, "timeout");
        string hoverText = GetItemHoverText(serial, wait, timeout);

        var payload = new Dictionary<string, object>
        {
            ["serial"] = serial,
            ["serialHex"] = $"0x{serial:X8}",
            ["wait"] = wait,
            ["timeout"] = timeout,
            ["found"] = !string.IsNullOrWhiteSpace(hoverText),
            ["hoverText"] = hoverText,
            ["lines"] = SplitHoverTextLines(hoverText)
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleTileInfoTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) || !TryGetInt(argsElement, "y", out int y))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_tile_info requires integer x and y."));

        ApiTileInfo tile = _api.GetTileInfo(x, y);
        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object> { ["tile"] = tile }));
    }

    private object HandleGetTileTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) || !TryGetInt(argsElement, "y", out int y))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_get_tile requires integer x and y."));

        ApiGameObject tile = _api.GetTile(x, y);
        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object>
        {
            ["x"] = x,
            ["y"] = y,
            ["found"] = tile != null,
            ["tile"] = tile
        }));
    }

    private object HandleRegionInfoTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) || !TryGetInt(argsElement, "y", out int y))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_region_info requires integer x and y."));

        ApiRegionInfo region = _api.GetRegionInfo(x, y);
        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object>
        {
            ["x"] = x,
            ["y"] = y,
            ["region"] = region
        }));
    }

    private object HandleTilesInAreaTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x1", out int x1) ||
            !TryGetInt(argsElement, "y1", out int y1) ||
            !TryGetInt(argsElement, "x2", out int x2) ||
            !TryGetInt(argsElement, "y2", out int y2))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_tiles_in_area requires integer x1, y1, x2, and y2."));
        }

        int maxTiles = TryGetInt(argsElement, "maxTiles", out int parsedMaxTiles) || TryGetInt(argsElement, "max_tiles", out parsedMaxTiles)
            ? parsedMaxTiles
            : 4096;

        List<ApiTileInfo> tiles = _api.GetTilesInArea(x1, y1, x2, y2, maxTiles);
        var payload = new Dictionary<string, object>
        {
            ["x1"] = x1,
            ["y1"] = y1,
            ["x2"] = x2,
            ["y2"] = y2,
            ["maxTiles"] = maxTiles,
            ["count"] = tiles.Count,
            ["tiles"] = tiles
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleStaticsInAreaTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x1", out int x1) ||
            !TryGetInt(argsElement, "y1", out int y1) ||
            !TryGetInt(argsElement, "x2", out int x2) ||
            !TryGetInt(argsElement, "y2", out int y2))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_statics_in_area requires integer x1, y1, x2, and y2."));
        }

        List<ApiStatic> statics = _api.GetStaticsInArea(x1, y1, x2, y2);
        var payload = new Dictionary<string, object>
        {
            ["x1"] = x1,
            ["y1"] = y1,
            ["x2"] = x2,
            ["y2"] = y2,
            ["count"] = statics.Count,
            ["statics"] = statics
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleMultisInAreaTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x1", out int x1) ||
            !TryGetInt(argsElement, "y1", out int y1) ||
            !TryGetInt(argsElement, "x2", out int x2) ||
            !TryGetInt(argsElement, "y2", out int y2))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_multis_in_area requires integer x1, y1, x2, and y2."));
        }

        List<ApiMulti> multis = _api.GetMultisInArea(x1, y1, x2, y2);
        var payload = new Dictionary<string, object>
        {
            ["x1"] = x1,
            ["y1"] = y1,
            ["x2"] = x2,
            ["y2"] = y2,
            ["count"] = multis.Count,
            ["multis"] = multis
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleHousesInAreaTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x1", out int x1) ||
            !TryGetInt(argsElement, "y1", out int y1) ||
            !TryGetInt(argsElement, "x2", out int x2) ||
            !TryGetInt(argsElement, "y2", out int y2))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_houses_in_area requires integer x1, y1, x2, and y2."));
        }

        int clearance = TryGetInt(argsElement, "clearance", out int parsedClearance) ? parsedClearance : 0;
        List<ApiHouseInfo> houses = _api.GetHousesInArea(x1, y1, x2, y2, clearance);
        var payload = new Dictionary<string, object>
        {
            ["x1"] = x1,
            ["y1"] = y1,
            ["x2"] = x2,
            ["y2"] = y2,
            ["clearance"] = clearance,
            ["count"] = houses.Count,
            ["houses"] = houses
        };

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(payload));
    }

    private object HandleCanPlaceHouseTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) ||
            !TryGetInt(argsElement, "y", out int y) ||
            !TryGetInt(argsElement, "width", out int width) ||
            !TryGetInt(argsElement, "depth", out int depth))
        {
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_can_place_house requires integer x, y, width, and depth."));
        }

        string direction = TryGetString(argsElement, "direction") ?? "south";
        int frontClearance = GetIntArgument(argsElement, 6, "frontClearance", "front_clearance");
        int backClearance = GetIntArgument(argsElement, 5, "backClearance", "back_clearance");
        int sideClearance = GetIntArgument(argsElement, 1, "sideClearance", "side_clearance");
        int maxZDelta = GetIntArgument(argsElement, 0, "maxZDelta", "max_z_delta");
        bool includeSteps = GetBoolArgument(argsElement, true, "includeSteps", "include_steps");
        bool allowSmallPlants = GetBoolArgument(argsElement, true, "allowSmallPlants", "allow_small_plants");

        ApiHousePlacementResult result = _api.CanPlaceHouse(
            x,
            y,
            width,
            depth,
            direction,
            frontClearance,
            backClearance,
            sideClearance,
            maxZDelta,
            allowSmallPlants,
            includeSteps);

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object> { ["placement"] = result }));
    }

    private object HandleMarkTileTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) || !TryGetInt(argsElement, "y", out int y))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_mark_tile requires integer x and y."));

        int map = TryGetInt(argsElement, "map", out int parsedMap) ? parsedMap : -1;
        string color = TryGetString(argsElement, "color") ?? TryGetString(argsElement, "htmlColor") ?? TryGetString(argsElement, "html_color");

        if (!string.IsNullOrWhiteSpace(color))
        {
            if (!_api.MarkTileColor(x, y, color, map))
                return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("color must be #RRGGBB, #RRGGBBAA, RRGGBB, RRGGBBAA, or 0x-prefixed hex, and a map must be available."));

            return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object>
            {
                ["x"] = x,
                ["y"] = y,
                ["map"] = map,
                ["mode"] = "color",
                ["color"] = color,
                ["marked"] = true
            }));
        }

        if (!TryGetInt(argsElement, "hue", out int hue))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_mark_tile requires either color or integer hue."));

        if (hue < ushort.MinValue || hue > ushort.MaxValue)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("hue must be between 0 and 65535."));

        _api.MarkTile(x, y, (ushort)hue, map);
        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object>
        {
            ["x"] = x,
            ["y"] = y,
            ["map"] = map,
            ["mode"] = "hue",
            ["hue"] = hue,
            ["marked"] = true
        }));
    }

    private object HandleRemoveMarkedTileTool(object requestId, JsonElement argsElement)
    {
        if (!TryGetInt(argsElement, "x", out int x) || !TryGetInt(argsElement, "y", out int y))
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult("Tool legion_remove_marked_tile requires integer x and y."));

        int map = TryGetInt(argsElement, "map", out int parsedMap) ? parsedMap : -1;
        _api.RemoveMarkedTile(x, y, map);
        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(new Dictionary<string, object>
        {
            ["x"] = x,
            ["y"] = y,
            ["map"] = map,
            ["removed"] = true
        }));
    }

    private object HandleScreenshotTool(object requestId, JsonElement argsElement)
    {
        string mode = TryGetString(argsElement, "mode")?.Trim().ToLowerInvariant() ?? "full";
        string path = TryGetString(argsElement, "path");

        ApiScreenshotResult screenshot = mode switch
        {
            "full" => _api.TakeScreenshot(path),
            "region" => TakeRegionScreenshot(argsElement, path),
            "gump" => TakeGumpScreenshot(argsElement, path),
            _ => new ApiScreenshotResult
            {
                Success = false,
                Mode = mode,
                Error = $"Unknown screenshot mode '{mode}'."
            }
        };

        if (screenshot == null)
        {
            return CreateJsonRpcSuccessResponse(
                requestId,
                BuildToolErrorResult("Screenshot call failed with null response."));
        }

        if (!screenshot.Success)
            return CreateJsonRpcSuccessResponse(requestId, BuildToolErrorResult(screenshot.Error, screenshot));

        return CreateJsonRpcSuccessResponse(requestId, BuildToolResult(screenshot, screenshot));
    }

    private ApiScreenshotResult TakeRegionScreenshot(JsonElement argsElement, string path)
    {
        if (!TryGetInt(argsElement, "x", out int x) ||
            !TryGetInt(argsElement, "y", out int y) ||
            !TryGetInt(argsElement, "width", out int width) ||
            !TryGetInt(argsElement, "height", out int height))
        {
            return new ApiScreenshotResult
            {
                Success = false,
                Mode = "region",
                Error = "Region mode requires x, y, width, and height."
            };
        }

        return _api.TakeScreenshotRegion(x, y, width, height, path);
    }

    private ApiScreenshotResult TakeGumpScreenshot(JsonElement argsElement, string path)
    {
        uint gumpId = uint.MaxValue;
        int padding = 0;

        if (TryGetUInt(argsElement, "gumpId", out uint parsedGumpId) || TryGetUInt(argsElement, "gump_id", out parsedGumpId))
            gumpId = parsedGumpId;

        if (TryGetInt(argsElement, "padding", out int parsedPadding))
            padding = parsedPadding;

        return _api.TakeScreenshotGump(gumpId, path, padding);
    }

    private object BuildToolResult(object structured, ApiScreenshotResult screenshot = null)
    {
        Dictionary<string, object> structuredContent = ToStructuredContent(structured);
        string structuredJson = JsonSerializer.Serialize(structuredContent, _serializerOptions);

        var content = new List<object>
        {
            new
            {
                type = "text",
                text = structuredJson
            }
        };

        if (screenshot != null && screenshot.Success && !string.IsNullOrWhiteSpace(screenshot.Path))
        {
            string imageError = TryBuildImageContentBlock(screenshot.Path, out object imageContent);
            if (imageError == null && imageContent != null)
                content.Add(imageContent);
            else if (imageError != null)
                content.Add(new { type = "text", text = imageError });
        }

        return new
        {
            content,
            structuredContent
        };
    }

    private object BuildToolErrorResult(string error, ApiScreenshotResult screenshot = null, object details = null)
    {
        string message = string.IsNullOrWhiteSpace(error) ? "Tool execution failed." : error.Trim();

        var content = new List<object>
        {
            new
            {
                type = "text",
                text = message
            }
        };

        Dictionary<string, object> structured = new()
        {
            ["error"] = message
        };

        if (screenshot != null)
            structured["screenshot"] = screenshot;

        if (details != null)
            structured["details"] = details;

        return new
        {
            content,
            structuredContent = structured,
            isError = true
        };
    }

    private string GetItemHoverText(uint serial, bool wait, int timeout)
    {
        try
        {
            return _api.ItemNameAndProps(serial, wait, timeout) ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to get item hover text for 0x{serial:X8}: {ex.Message}");
            return string.Empty;
        }
    }

    private static Dictionary<string, object> BuildItemPayload(ApiItem item, string hoverText = null)
    {
        if (item == null)
            return new Dictionary<string, object>();

        var payload = new Dictionary<string, object>
        {
            ["serial"] = item.Serial,
            ["serialHex"] = $"0x{item.Serial:X8}",
            ["name"] = item.Name,
            ["graphic"] = item.Graphic,
            ["graphicHex"] = $"0x{item.Graphic:X4}",
            ["hue"] = item.Hue,
            ["hueHex"] = $"0x{item.Hue:X4}",
            ["amount"] = item.Amount,
            ["container"] = item.Container,
            ["containerHex"] = $"0x{item.Container:X8}",
            ["rootContainer"] = item.RootContainer,
            ["rootContainerHex"] = $"0x{item.RootContainer:X8}",
            ["isContainer"] = item.IsContainer,
            ["isCorpse"] = item.IsCorpse,
            ["opened"] = item.Opened,
            ["onGround"] = item.OnGround,
            ["x"] = item.X,
            ["y"] = item.Y,
            ["z"] = item.Z,
            ["distance"] = item.Distance,
            ["matchesHighlight"] = item.MatchesHighlight,
            ["matchingHighlightName"] = item.MatchingHighlightName
        };

        if (hoverText != null)
        {
            payload["hoverText"] = hoverText;
            payload["hoverTextLines"] = SplitHoverTextLines(hoverText);
            payload["hoverTextFound"] = !string.IsNullOrWhiteSpace(hoverText);
        }

        return payload;
    }

    private static string[] SplitHoverTextLines(string hoverText)
    {
        if (string.IsNullOrEmpty(hoverText))
            return Array.Empty<string>();

        return hoverText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToArray();
    }

    private string TryBuildImageContentBlock(string screenshotPath, out object imageContent)
    {
        imageContent = null;

        try
        {
            if (!File.Exists(screenshotPath))
                return $"Screenshot path does not exist: {screenshotPath}";

            byte[] bytes = File.ReadAllBytes(screenshotPath);
            if (bytes.Length == 0)
                return $"Screenshot file is empty: {screenshotPath}";

            imageContent = new
            {
                type = "image",
                mimeType = "image/png",
                data = Convert.ToBase64String(bytes)
            };
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to load screenshot file '{screenshotPath}': {ex.Message}";
        }
    }

    private static Dictionary<string, object> ToStructuredContent(object value)
    {
        if (value == null)
            return new Dictionary<string, object>();

        if (value is Dictionary<string, object> dict)
            return dict;

        object normalized = NormalizeResult(value);
        if (normalized is Dictionary<string, object> normalizedDict)
            return normalizedDict;

        return new Dictionary<string, object>
        {
            ["result"] = normalized
        };
    }

    private static Dictionary<string, object> CreateJsonRpcSuccessResponse(object id, object result) => new()
    {
        ["jsonrpc"] = JsonRpcVersion,
        ["id"] = id,
        ["result"] = result
    };

    private static Dictionary<string, object> CreateJsonRpcErrorResponse(object id, int code, string message, object data = null)
    {
        var error = new Dictionary<string, object>
        {
            ["code"] = code,
            ["message"] = message
        };

        if (data != null)
            error["data"] = data;

        return new Dictionary<string, object>
        {
            ["jsonrpc"] = JsonRpcVersion,
            ["id"] = id,
            ["error"] = error
        };
    }

    private static bool TryConvertRequestId(JsonElement idElement, out object id, out string error)
    {
        id = null;
        error = string.Empty;

        switch (idElement.ValueKind)
        {
            case JsonValueKind.String:
                id = idElement.GetString();
                return true;
            case JsonValueKind.Number:
                if (idElement.TryGetInt64(out long longId))
                {
                    id = longId;
                    return true;
                }

                id = idElement.GetDouble();
                return true;
            default:
                error = "id must be a string or number.";
                return false;
        }
    }

    private static string NegotiateProtocolVersion(string requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) &&
            _supportedProtocolVersions.Contains(requested, StringComparer.Ordinal))
        {
            return requested;
        }

        return _supportedProtocolVersions.FirstOrDefault() ?? DefaultProtocolVersion;
    }

    private static MethodInfo[] GetInvocableLegionMethods() =>
        typeof(LegionAPI)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => !m.IsSpecialName)
            .Where(m => !_blockedMethods.Contains(m.Name))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.GetParameters().Length)
            .ToArray();

    private static string BuildMethodSignature(MethodInfo method)
    {
        string parameters = string.Join(", ", method.GetParameters().Select(p =>
        {
            string defaultPart = string.Empty;
            if (p.IsOptional)
            {
                string defaultValue = p.DefaultValue switch
                {
                    null => "null",
                    string s => $"\"{s}\"",
                    _ => p.DefaultValue?.ToString() ?? "null"
                };

                defaultPart = $" = {defaultValue}";
            }

            return $"{GetFriendlyTypeName(p.ParameterType)} {p.Name}{defaultPart}";
        }));

        return $"{GetFriendlyTypeName(method.ReturnType)} {method.Name}({parameters})";
    }

    private static object BuildMethodDefinition(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Dictionary<string, object> kwargs = parameters.ToDictionary(p => p.Name, BuildParameterTemplateValue, StringComparer.OrdinalIgnoreCase);
        string directTool = GetDirectToolForMethod(method.Name);

        return new Dictionary<string, object>
        {
            ["name"] = method.Name,
            ["signature"] = BuildMethodSignature(method),
            ["returnType"] = GetFriendlyTypeName(method.ReturnType),
            ["parameters"] = parameters.Select(BuildParameterDefinition).ToArray(),
            ["invoke"] = new Dictionary<string, object>
            {
                ["tool"] = ToolInvoke,
                ["method"] = method.Name,
                ["kwargs"] = kwargs
            },
            ["directTool"] = directTool
        };
    }

    private static object BuildParameterDefinition(ParameterInfo parameter)
    {
        bool optional = parameter.IsOptional || parameter.HasDefaultValue;

        return new Dictionary<string, object>
        {
            ["name"] = parameter.Name,
            ["position"] = parameter.Position,
            ["type"] = GetFriendlyTypeName(parameter.ParameterType),
            ["optional"] = optional,
            ["defaultValue"] = optional ? GetSerializableDefaultValue(parameter) : null,
            ["templateValue"] = BuildParameterTemplateValue(parameter)
        };
    }

    private static object BuildParameterTemplateValue(ParameterInfo parameter)
    {
        if (parameter.IsOptional || parameter.HasDefaultValue)
            return GetSerializableDefaultValue(parameter);

        Type type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (type == typeof(string))
            return string.Empty;

        if (type == typeof(bool))
            return false;

        if (type == typeof(int) || type == typeof(short) || type == typeof(byte) || type == typeof(long))
            return 0;

        if (type == typeof(uint) || type == typeof(ushort) || type == typeof(ulong))
            return 0;

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return 0;

        if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
            return Array.Empty<object>();

        return null;
    }

    private static object GetSerializableDefaultValue(ParameterInfo parameter)
    {
        if (!parameter.HasDefaultValue && !parameter.IsOptional)
            return null;

        object value = parameter.DefaultValue;

        if (value == null || value == DBNull.Value || value == Type.Missing)
            return null;

        if (value is uint uintValue && uintValue == uint.MaxValue)
            return "uint.MaxValue";

        if (value is Enum enumValue)
            return enumValue.ToString();

        return value;
    }

    private static string GetDirectToolForMethod(string methodName)
    {
        if (string.Equals(methodName, nameof(LegionAPI.ClickGumpButton), StringComparison.OrdinalIgnoreCase))
            return ToolGumpClickButton;

        if (string.Equals(methodName, nameof(LegionAPI.GetTile), StringComparison.OrdinalIgnoreCase))
            return ToolGetTile;

        if (string.Equals(methodName, nameof(LegionAPI.GetTileInfo), StringComparison.OrdinalIgnoreCase))
            return ToolTileInfo;

        if (string.Equals(methodName, nameof(LegionAPI.GetRegionInfo), StringComparison.OrdinalIgnoreCase))
            return ToolRegionInfo;

        if (string.Equals(methodName, nameof(LegionAPI.GetTilesInArea), StringComparison.OrdinalIgnoreCase))
            return ToolTilesInArea;

        if (string.Equals(methodName, nameof(LegionAPI.GetStaticsInArea), StringComparison.OrdinalIgnoreCase))
            return ToolStaticsInArea;

        if (string.Equals(methodName, nameof(LegionAPI.GetMultisInArea), StringComparison.OrdinalIgnoreCase))
            return ToolMultisInArea;

        if (string.Equals(methodName, nameof(LegionAPI.GetHousesInArea), StringComparison.OrdinalIgnoreCase))
            return ToolHousesInArea;

        if (string.Equals(methodName, nameof(LegionAPI.CanPlaceHouse), StringComparison.OrdinalIgnoreCase))
            return ToolCanPlaceHouse;

        if (string.Equals(methodName, nameof(LegionAPI.MarkTile), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(methodName, nameof(LegionAPI.MarkTileColor), StringComparison.OrdinalIgnoreCase))
            return ToolMarkTile;

        if (string.Equals(methodName, nameof(LegionAPI.RemoveMarkedTile), StringComparison.OrdinalIgnoreCase))
            return ToolRemoveMarkedTile;

        return null;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        Type nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
            return $"{GetFriendlyTypeName(nullable)}?";

        if (type.IsArray)
            return $"{GetFriendlyTypeName(type.GetElementType() ?? typeof(object))}[]";

        if (type.IsGenericType)
        {
            string name = type.Name;
            int tickIndex = name.IndexOf('`');
            if (tickIndex > 0)
                name = name.Substring(0, tickIndex);

            string args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{name}<{args}>";
        }

        return type.Name switch
        {
            "Void" => "void",
            "Boolean" => "bool",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Single" => "float",
            "Double" => "double",
            "Decimal" => "decimal",
            "String" => "string",
            "Object" => "object",
            _ => type.Name
        };
    }

    private bool TryInvokeMethod(string methodName, JsonElement argsElement, JsonElement kwargsElement, out object result, out string error)
    {
        result = null;
        error = string.Empty;

        if (_blockedMethods.Contains(methodName))
        {
            error = $"Method '{methodName}' is blocked by bridge policy.";
            return false;
        }

        MethodInfo[] candidates = typeof(LegionAPI)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
            .Where(m => !m.IsSpecialName)
            .ToArray();

        if (candidates.Length == 0)
        {
            error = $"LegionAPI method '{methodName}' was not found.";
            return false;
        }

        foreach (MethodInfo method in candidates.OrderBy(m => m.GetParameters().Length))
        {
            if (!TryBuildArguments(method, argsElement, kwargsElement, out object[] invokeArgs, out string buildError))
            {
                error = buildError;
                continue;
            }

            try
            {
                result = method.Invoke(_api, invokeArgs);
                return true;
            }
            catch (TargetInvocationException tie)
            {
                error = tie.InnerException?.Message ?? tie.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        return false;
    }

    private bool TryBuildArguments(MethodInfo method, JsonElement argsElement, JsonElement kwargsElement, out object[] invokeArgs, out string error)
    {
        invokeArgs = Array.Empty<object>();
        error = string.Empty;

        ParameterInfo[] parameters = method.GetParameters();
        var positional = new List<JsonElement>();
        var named = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (argsElement.ValueKind != JsonValueKind.Undefined && argsElement.ValueKind != JsonValueKind.Null)
        {
            if (argsElement.ValueKind == JsonValueKind.Array)
            {
                positional.AddRange(argsElement.EnumerateArray());
            }
            else if (argsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in argsElement.EnumerateObject())
                    named[property.Name] = property.Value;
            }
            else
            {
                error = "args must be either an array or an object.";
                return false;
            }
        }

        if (kwargsElement.ValueKind != JsonValueKind.Undefined && kwargsElement.ValueKind != JsonValueKind.Null)
        {
            if (kwargsElement.ValueKind != JsonValueKind.Object)
            {
                error = "kwargs must be an object.";
                return false;
            }

            foreach (JsonProperty property in kwargsElement.EnumerateObject())
                named[property.Name] = property.Value;
        }

        if (positional.Count > parameters.Length)
        {
            error = $"Too many positional args for method '{method.Name}'. Expected at most {parameters.Length}, got {positional.Count}.";
            return false;
        }

        invokeArgs = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            JsonElement valueElement = default;
            bool hasValue = false;

            if (i < positional.Count)
            {
                valueElement = positional[i];
                hasValue = true;
            }
            else if (named.TryGetValue(parameter.Name, out JsonElement namedValue))
            {
                valueElement = namedValue;
                hasValue = true;
            }

            if (hasValue)
            {
                if (!TryConvertJsonToType(valueElement, parameter.ParameterType, out object converted, out string convertError))
                {
                    error = $"Invalid value for parameter '{parameter.Name}' in method '{method.Name}': {convertError}";
                    return false;
                }

                invokeArgs[i] = converted;
                continue;
            }

            if (parameter.HasDefaultValue || parameter.IsOptional)
            {
                object defaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : Type.Missing;
                if (defaultValue == DBNull.Value || defaultValue == Type.Missing)
                    defaultValue = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
                invokeArgs[i] = defaultValue;
                continue;
            }

            error = $"Missing required parameter '{parameter.Name}' for method '{method.Name}'.";
            return false;
        }

        return true;
    }

    private bool TryConvertJsonToType(JsonElement element, Type targetType, out object converted, out string error)
    {
        converted = null;
        error = string.Empty;

        Type nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType != null)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                converted = null;
                return true;
            }

            targetType = nullableType;
        }

        if (targetType == typeof(JsonElement))
        {
            converted = element.Clone();
            return true;
        }

        if (targetType == typeof(object))
        {
            converted = ConvertToLooseObject(element);
            return true;
        }

        try
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    converted = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                    return true;
                case JsonValueKind.String:
                    if (targetType == typeof(string))
                    {
                        converted = element.GetString();
                        return true;
                    }

                    if (targetType == typeof(DateTime))
                    {
                        if (DateTime.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                        {
                            converted = parsedDate;
                            return true;
                        }

                        error = "Invalid DateTime string.";
                        return false;
                    }
                    break;
                case JsonValueKind.Number:
                    if (targetType == typeof(int))
                    {
                        converted = element.GetInt32();
                        return true;
                    }

                    if (targetType == typeof(uint))
                    {
                        converted = element.GetUInt32();
                        return true;
                    }

                    if (targetType == typeof(long))
                    {
                        converted = element.GetInt64();
                        return true;
                    }

                    if (targetType == typeof(ulong))
                    {
                        converted = element.GetUInt64();
                        return true;
                    }

                    if (targetType == typeof(short))
                    {
                        converted = checked((short)element.GetInt32());
                        return true;
                    }

                    if (targetType == typeof(ushort))
                    {
                        converted = checked((ushort)element.GetUInt32());
                        return true;
                    }

                    if (targetType == typeof(byte))
                    {
                        converted = checked((byte)element.GetUInt32());
                        return true;
                    }

                    if (targetType == typeof(sbyte))
                    {
                        converted = checked((sbyte)element.GetInt32());
                        return true;
                    }

                    if (targetType == typeof(float))
                    {
                        converted = element.GetSingle();
                        return true;
                    }

                    if (targetType == typeof(double))
                    {
                        converted = element.GetDouble();
                        return true;
                    }

                    if (targetType == typeof(decimal))
                    {
                        converted = element.GetDecimal();
                        return true;
                    }
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (targetType == typeof(bool))
                    {
                        converted = element.GetBoolean();
                        return true;
                    }
                    break;
            }

            if (targetType == typeof(string))
            {
                converted = element.ToString();
                return true;
            }

            if (targetType == typeof(bool) && element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out bool parsedBool))
            {
                converted = parsedBool;
                return true;
            }

            if (targetType.IsEnum)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    converted = Enum.Parse(targetType, element.GetString(), true);
                    return true;
                }

                if (element.ValueKind == JsonValueKind.Number)
                {
                    object enumValue = Convert.ChangeType(element.GetInt32(), Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture);
                    converted = Enum.ToObject(targetType, enumValue);
                    return true;
                }
            }

            if (targetType.IsArray)
            {
                if (element.ValueKind != JsonValueKind.Array)
                {
                    error = "Expected an array.";
                    return false;
                }

                Type itemType = targetType.GetElementType() ?? typeof(object);
                var items = new List<object>();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (!TryConvertJsonToType(item, itemType, out object convertedItem, out string itemError))
                    {
                        error = itemError;
                        return false;
                    }

                    items.Add(convertedItem);
                }

                Array array = Array.CreateInstance(itemType, items.Count);
                for (int i = 0; i < items.Count; i++)
                    array.SetValue(items[i], i);
                converted = array;
                return true;
            }

            if (targetType == typeof(IEnumerable) || targetType == typeof(ICollection) || targetType == typeof(IList))
            {
                if (element.ValueKind != JsonValueKind.Array)
                {
                    error = "Expected an array.";
                    return false;
                }

                var list = new ArrayList();
                foreach (JsonElement item in element.EnumerateArray())
                    list.Add(ConvertToLooseObject(item));

                converted = list;
                return true;
            }

            if (targetType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                if (element.ValueKind != JsonValueKind.Array)
                {
                    error = "Expected an array.";
                    return false;
                }

                Type itemType = targetType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
                Type listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList)Activator.CreateInstance(listType);

                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (!TryConvertJsonToType(item, itemType, out object convertedItem, out string itemError))
                    {
                        error = itemError;
                        return false;
                    }

                    list.Add(convertedItem);
                }

                converted = list;
                return true;
            }

            converted = JsonSerializer.Deserialize(element.GetRawText(), targetType, _serializerOptions);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static object ConvertToLooseObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertToLooseObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertToLooseObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out long l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static object NormalizeResult(object value)
    {
        if (value == null)
            return null;

        Type valueType = value.GetType();
        if (value is string ||
            value is bool ||
            value is byte ||
            value is sbyte ||
            value is short ||
            value is ushort ||
            value is int ||
            value is uint ||
            value is long ||
            value is ulong ||
            value is float ||
            value is double ||
            value is decimal ||
            value is DateTime)
        {
            return value;
        }

        if (value is Enum enumValue)
            return enumValue.ToString();

        if (value is ApiItem apiItem)
            return BuildItemPayload(apiItem);

        if (value is ApiGameObject ||
            value is ApiLand ||
            value is ApiStatic ||
            value is ApiMulti)
            return value;

        if (value is ApiScreenshotResult ||
            value is ApiOpenGumpInfo ||
            value is ApiOpenGumpInfo[] ||
            value is ApiGumpButtonClickResult ||
            value is ApiTileInfo ||
            value is ApiHouseInfo ||
            value is ApiHousePlacementResult ||
            value is ApiTileFlagInfo ||
            value is ApiLandTileInfo ||
            value is ApiStaticTileInfo ||
            value is ApiMultiComponentInfo ||
            value is ApiRegionInfo ||
            value is ApiHousePlacementBlocker)
            return value;

        if (value is IEnumerable enumerable && value is not string)
        {
            var list = new List<object>();
            int count = 0;

            foreach (object item in enumerable)
            {
                if (count++ >= 200)
                {
                    list.Add("...truncated...");
                    break;
                }

                list.Add(NormalizeResult(item));
            }

            return list;
        }

        return new Dictionary<string, object>
        {
            ["type"] = valueType.FullName ?? valueType.Name,
            ["value"] = value.ToString() ?? string.Empty
        };
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        value = default;

        if (root.ValueKind != JsonValueKind.Object)
            return false;

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static string TryGetString(JsonElement root, string name)
    {
        if (!TryGetProperty(root, name, out JsonElement value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static bool TryGetBool(JsonElement root, string name, out bool value)
    {
        value = false;

        if (!TryGetProperty(root, name, out JsonElement jsonValue))
            return false;

        if (jsonValue.ValueKind == JsonValueKind.True || jsonValue.ValueKind == JsonValueKind.False)
        {
            value = jsonValue.GetBoolean();
            return true;
        }

        if (jsonValue.ValueKind == JsonValueKind.String &&
            bool.TryParse(jsonValue.GetString(), out bool fromString))
        {
            value = fromString;
            return true;
        }

        return false;
    }

    private static bool TryGetIntArray(JsonElement root, out int[] values, out string error, params string[] names)
    {
        values = null;
        error = string.Empty;

        foreach (string name in names)
        {
            if (!TryGetProperty(root, name, out JsonElement jsonValue))
                continue;

            if (jsonValue.ValueKind == JsonValueKind.Null || jsonValue.ValueKind == JsonValueKind.Undefined)
                return true;

            if (jsonValue.ValueKind != JsonValueKind.Array)
            {
                error = $"{name} must be an array of integers.";
                return false;
            }

            var parsed = new List<int>();

            foreach (JsonElement item in jsonValue.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int fromNumber))
                {
                    parsed.Add(fromNumber);
                    continue;
                }

                if (item.ValueKind == JsonValueKind.String &&
                    int.TryParse(item.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fromString))
                {
                    parsed.Add(fromString);
                    continue;
                }

                error = $"{name} must contain only integers.";
                return false;
            }

            values = parsed.ToArray();
            return true;
        }

        return true;
    }

    private static bool TryGetLooseArray(JsonElement root, out List<object> values, out string error, params string[] names)
    {
        values = null;
        error = string.Empty;

        foreach (string name in names)
        {
            if (!TryGetProperty(root, name, out JsonElement jsonValue))
                continue;

            if (jsonValue.ValueKind == JsonValueKind.Null || jsonValue.ValueKind == JsonValueKind.Undefined)
                return true;

            if (jsonValue.ValueKind != JsonValueKind.Array)
            {
                error = $"{name} must be an array.";
                return false;
            }

            values = jsonValue.EnumerateArray().Select(ConvertToLooseObject).ToList();
            return true;
        }

        return true;
    }

    private static bool HasTruthyArgument(JsonElement root, params string[] names)
    {
        foreach (string name in names)
        {
            if (TryGetBool(root, name, out bool value) && value)
                return true;
        }

        return false;
    }

    private static int GetIntArgument(JsonElement root, int defaultValue, params string[] names)
    {
        foreach (string name in names)
        {
            if (TryGetInt(root, name, out int value))
                return value;
        }

        return defaultValue;
    }

    private static bool GetBoolArgument(JsonElement root, bool defaultValue, params string[] names)
    {
        foreach (string name in names)
        {
            if (TryGetBool(root, name, out bool value))
                return value;
        }

        return defaultValue;
    }

    private static int GetTimeoutSeconds(JsonElement root, int defaultValue, params string[] names)
    {
        foreach (string name in names)
        {
            if (TryGetInt(root, name, out int value))
                return ClampTimeoutSeconds(value);
        }

        return ClampTimeoutSeconds(defaultValue);
    }

    private static int ClampTimeoutSeconds(int value) => Math.Clamp(value, 0, 60);

    private static bool TryGetInt(JsonElement root, string name, out int value)
    {
        value = 0;
        if (!TryGetProperty(root, name, out JsonElement jsonValue))
            return false;

        if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetInt32(out int fromNumber))
        {
            value = fromNumber;
            return true;
        }

        if (jsonValue.ValueKind == JsonValueKind.String && int.TryParse(jsonValue.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fromString))
        {
            value = fromString;
            return true;
        }

        return false;
    }

    private static bool TryGetUInt(JsonElement root, string name, out uint value)
    {
        value = 0;
        if (!TryGetProperty(root, name, out JsonElement jsonValue))
            return false;

        if (jsonValue.ValueKind == JsonValueKind.Number && jsonValue.TryGetUInt32(out uint fromNumber))
        {
            value = fromNumber;
            return true;
        }

        if (jsonValue.ValueKind == JsonValueKind.String)
        {
            string raw = jsonValue.GetString()?.Trim() ?? string.Empty;
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(2);

                if (uint.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint prefixedHex))
                {
                    value = prefixedHex;
                    return true;
                }

                return false;
            }

            if (uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint fromString))
            {
                value = fromString;
                return true;
            }

            if (uint.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint fromHex))
            {
                value = fromHex;
                return true;
            }
        }

        return false;
    }

    public void Dispose() => _api.Dispose();
}
