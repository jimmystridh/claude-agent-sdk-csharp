using System.Text.Json;
using System.Text.Json.Serialization;
using ih0.Claude.Agent.SDK.Types;

namespace ih0.Claude.Agent.SDK.Internal;

/// <summary>
/// JSON serialization context for source-generated serialization.
/// This improves performance by avoiding runtime reflection.
/// </summary>
/// <remarks>
/// Note: Not all serialization in the SDK uses this context yet.
/// Some places use anonymous types or Dictionary serialization that
/// would require refactoring to use source generation.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(AgentDefinition))]
[JsonSerializable(typeof(Dictionary<string, AgentDefinition>))]
[JsonSerializable(typeof(Dictionary<string, McpServerConfig>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(McpStdioServerConfig))]
[JsonSerializable(typeof(McpSseServerConfig))]
[JsonSerializable(typeof(McpSdkServerConfig))]
[JsonSerializable(typeof(SandboxSettings))]
[JsonSerializable(typeof(SystemPromptPreset))]
[JsonSerializable(typeof(ToolsPreset))]
[JsonSerializable(typeof(HookMatcher))]
[JsonSerializable(typeof(List<HookMatcher>))]
[JsonSerializable(typeof(Dictionary<string, List<HookMatcherConfig>>))]
[JsonSerializable(typeof(HookMatcherConfig))]
[JsonSerializable(typeof(ControlRequest))]
[JsonSerializable(typeof(ControlResponse))]
[JsonSerializable(typeof(PermissionResult))]
[JsonSerializable(typeof(PermissionAllow))]
[JsonSerializable(typeof(PermissionDeny))]
[JsonSerializable(typeof(PermissionUpdate))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
internal partial class ClaudeJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Control request message sent to Claude CLI.
/// </summary>
internal record ControlRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("response")]
    public required ControlRequestResponse Response { get; init; }
}

/// <summary>
/// Response portion of a control request.
/// </summary>
internal record ControlRequestResponse
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("subtype")]
    public required string Subtype { get; init; }

    [JsonPropertyName("response")]
    public JsonElement? ResponseData { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Success response for control requests.
/// </summary>
internal record ControlSuccessResponse
{
    [JsonPropertyName("type")]
    public string Type => "control_request";

    [JsonPropertyName("response")]
    public required ControlResponsePayload Response { get; init; }
}

/// <summary>
/// Error response for control requests.
/// </summary>
internal record ControlErrorResponse
{
    [JsonPropertyName("type")]
    public string Type => "control_request";

    [JsonPropertyName("response")]
    public required ControlErrorPayload Response { get; init; }
}

/// <summary>
/// Success response payload.
/// </summary>
internal record ControlResponsePayload
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("subtype")]
    public required string Subtype { get; init; }

    [JsonPropertyName("response")]
    public JsonElement? ResponseData { get; init; }
}

/// <summary>
/// Error response payload.
/// </summary>
internal record ControlErrorPayload
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("subtype")]
    public string Subtype => "error";

    [JsonPropertyName("error")]
    public required string Error { get; init; }
}

/// <summary>
/// Control response received from Claude CLI.
/// </summary>
internal record ControlResponse
{
    [JsonPropertyName("subtype")]
    public string? Subtype { get; init; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("response")]
    public JsonElement? ResponseData { get; init; }
}

/// <summary>
/// Message types for internal protocol.
/// </summary>
internal record EndMessage
{
    [JsonPropertyName("type")]
    public string Type => "end";
}

/// <summary>
/// Error message for internal protocol.
/// </summary>
internal record ErrorMessage
{
    [JsonPropertyName("type")]
    public string Type => "error";

    [JsonPropertyName("error")]
    public required string Error { get; init; }
}
