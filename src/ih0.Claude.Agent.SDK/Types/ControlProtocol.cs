using System.Text.Json;
using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

public abstract record ControlRequest
{
    [JsonPropertyName("subtype")]
    public abstract string Subtype { get; }
}

public sealed record InterruptRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "interrupt";
}

public sealed record InitializeRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "initialize";

    [JsonPropertyName("hooks")]
    public JsonElement? Hooks { get; init; }
}

public sealed record SetPermissionModeRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "set_permission_mode";

    [JsonPropertyName("mode")]
    public required string Mode { get; init; }
}

public sealed record SetModelRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "set_model";

    [JsonPropertyName("model")]
    public string? Model { get; init; }
}

public sealed record RewindFilesRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "rewind_files";

    [JsonPropertyName("user_message_id")]
    public required string UserMessageId { get; init; }
}

public sealed record CanUseToolRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "can_use_tool";

    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }

    [JsonPropertyName("permission_suggestions")]
    public IReadOnlyList<JsonElement>? PermissionSuggestions { get; init; }

    [JsonPropertyName("blocked_path")]
    public string? BlockedPath { get; init; }
}

public sealed record HookCallbackRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "hook_callback";

    [JsonPropertyName("callback_id")]
    public required string CallbackId { get; init; }

    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }

    [JsonPropertyName("tool_use_id")]
    public string? ToolUseId { get; init; }
}

public sealed record McpMessageRequest : ControlRequest
{
    [JsonPropertyName("subtype")]
    public override string Subtype => "mcp_message";

    [JsonPropertyName("server_name")]
    public required string ServerName { get; init; }

    [JsonPropertyName("message")]
    public required JsonElement Message { get; init; }
}

public sealed record SdkControlRequest
{
    [JsonPropertyName("type")]
    public string Type => "control_request";

    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("request")]
    public required JsonElement Request { get; init; }
}

public sealed record ControlSuccessResponse
{
    [JsonPropertyName("subtype")]
    public string Subtype => "success";

    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("response")]
    public JsonElement? Response { get; init; }
}

public sealed record ControlErrorResponse
{
    [JsonPropertyName("subtype")]
    public string Subtype => "error";

    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("error")]
    public required string Error { get; init; }
}

public sealed record SdkControlResponse
{
    [JsonPropertyName("type")]
    public string Type => "control_response";

    [JsonPropertyName("response")]
    public required JsonElement Response { get; init; }
}
