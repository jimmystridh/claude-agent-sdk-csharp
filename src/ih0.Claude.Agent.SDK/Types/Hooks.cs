using System.Text.Json;
using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Base class for hook input data.
/// </summary>
public abstract record HookInput
{
    /// <summary>
    /// The session ID for the current conversation.
    /// </summary>
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    /// <summary>
    /// Path to the conversation transcript file.
    /// </summary>
    [JsonPropertyName("transcript_path")]
    public required string TranscriptPath { get; init; }

    /// <summary>
    /// Current working directory.
    /// </summary>
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    /// <summary>
    /// Current permission mode.
    /// </summary>
    [JsonPropertyName("permission_mode")]
    public string? PermissionMode { get; init; }
}

/// <summary>
/// Input data for pre-tool-use hooks.
/// </summary>
public sealed record PreToolUseHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "PreToolUse";

    /// <summary>
    /// The name of the tool being invoked.
    /// </summary>
    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    /// <summary>
    /// The input parameters for the tool.
    /// </summary>
    [JsonPropertyName("tool_input")]
    public required JsonElement ToolInput { get; init; }
}

/// <summary>
/// Input data for post-tool-use hooks.
/// </summary>
public sealed record PostToolUseHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "PostToolUse";

    /// <summary>
    /// The name of the tool that was invoked.
    /// </summary>
    [JsonPropertyName("tool_name")]
    public required string ToolName { get; init; }

    /// <summary>
    /// The input parameters that were passed to the tool.
    /// </summary>
    [JsonPropertyName("tool_input")]
    public required JsonElement ToolInput { get; init; }

    /// <summary>
    /// The response from the tool execution.
    /// </summary>
    [JsonPropertyName("tool_response")]
    public required JsonElement ToolResponse { get; init; }
}

/// <summary>
/// Input data for user prompt submission hooks.
/// </summary>
public sealed record UserPromptSubmitHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "UserPromptSubmit";

    /// <summary>
    /// The user's prompt text.
    /// </summary>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }
}

/// <summary>
/// Input data for stop hooks.
/// </summary>
public sealed record StopHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "Stop";

    /// <summary>
    /// Whether a stop hook is currently active.
    /// </summary>
    [JsonPropertyName("stop_hook_active")]
    public required bool StopHookActive { get; init; }
}

/// <summary>
/// Input data for subagent stop hooks.
/// </summary>
public sealed record SubagentStopHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "SubagentStop";

    /// <summary>
    /// Whether a stop hook is currently active.
    /// </summary>
    [JsonPropertyName("stop_hook_active")]
    public required bool StopHookActive { get; init; }
}

/// <summary>
/// Input data for pre-compact hooks.
/// </summary>
public sealed record PreCompactHookInput : HookInput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hook_event_name")]
    public string HookEventName => "PreCompact";

    /// <summary>
    /// The trigger that caused compaction.
    /// </summary>
    [JsonPropertyName("trigger")]
    public required string Trigger { get; init; }

    /// <summary>
    /// Custom instructions for the compaction process.
    /// </summary>
    [JsonPropertyName("custom_instructions")]
    public string? CustomInstructions { get; init; }
}

/// <summary>
/// Output from a hook callback.
/// </summary>
public record HookOutput
{
    /// <summary>
    /// Whether to continue processing.
    /// </summary>
    [JsonPropertyName("continue")]
    public bool? Continue { get; init; }

    /// <summary>
    /// Whether to suppress output.
    /// </summary>
    [JsonPropertyName("suppressOutput")]
    public bool? SuppressOutput { get; init; }

    /// <summary>
    /// Reason for stopping, if applicable.
    /// </summary>
    [JsonPropertyName("stopReason")]
    public string? StopReason { get; init; }

    /// <summary>
    /// The decision made by the hook.
    /// </summary>
    [JsonPropertyName("decision")]
    public string? Decision { get; init; }

    /// <summary>
    /// A system message to inject.
    /// </summary>
    [JsonPropertyName("systemMessage")]
    public string? SystemMessage { get; init; }

    /// <summary>
    /// Reason for the decision.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Hook-specific output data.
    /// </summary>
    [JsonPropertyName("hookSpecificOutput")]
    public JsonElement? HookSpecificOutput { get; init; }
}

/// <summary>
/// Output for asynchronous hook processing.
/// </summary>
public record AsyncHookOutput
{
    /// <summary>
    /// Whether this is an async response.
    /// </summary>
    [JsonPropertyName("async")]
    public bool Async { get; init; } = true;

    /// <summary>
    /// Timeout in milliseconds for async processing.
    /// </summary>
    [JsonPropertyName("asyncTimeout")]
    public int? AsyncTimeout { get; init; }
}

/// <summary>
/// Context information passed to hook callbacks.
/// </summary>
public sealed record HookContext
{
    /// <summary>
    /// Signal object for async coordination.
    /// </summary>
    public object? Signal { get; init; }
}

/// <summary>
/// Callback delegate for hook processing.
/// </summary>
/// <param name="input">The hook input data.</param>
/// <param name="toolUseId">The ID of the tool use, if applicable.</param>
/// <param name="context">The hook context.</param>
/// <returns>The hook output.</returns>
public delegate Task<HookOutput> HookCallback(HookInput input, string? toolUseId, HookContext context);

/// <summary>
/// Configuration for matching and handling hooks.
/// </summary>
public sealed record HookMatcher
{
    /// <summary>
    /// Pattern to match against (e.g., tool name).
    /// </summary>
    public string? Matcher { get; init; }

    /// <summary>
    /// List of hook callbacks to invoke.
    /// </summary>
    public IReadOnlyList<HookCallback> Hooks { get; init; } = [];

    /// <summary>
    /// Timeout in seconds for hook execution.
    /// </summary>
    public double? Timeout { get; init; }
}

/// <summary>
/// Hook-specific output for pre-tool-use hooks.
/// </summary>
public sealed record PreToolUseHookSpecificOutput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hookEventName")]
    public string HookEventName => "PreToolUse";

    /// <summary>
    /// Permission decision (allow, deny, ask).
    /// </summary>
    [JsonPropertyName("permissionDecision")]
    public string? PermissionDecision { get; init; }

    /// <summary>
    /// Reason for the permission decision.
    /// </summary>
    [JsonPropertyName("permissionDecisionReason")]
    public string? PermissionDecisionReason { get; init; }

    /// <summary>
    /// Modified input to pass to the tool.
    /// </summary>
    [JsonPropertyName("updatedInput")]
    public JsonElement? UpdatedInput { get; init; }
}

/// <summary>
/// Hook-specific output for post-tool-use hooks.
/// </summary>
public sealed record PostToolUseHookSpecificOutput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hookEventName")]
    public string HookEventName => "PostToolUse";

    /// <summary>
    /// Additional context to add to the conversation.
    /// </summary>
    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}

/// <summary>
/// Hook-specific output for user prompt submission hooks.
/// </summary>
public sealed record UserPromptSubmitHookSpecificOutput
{
    /// <summary>
    /// The name of the hook event.
    /// </summary>
    [JsonPropertyName("hookEventName")]
    public string HookEventName => "UserPromptSubmit";

    /// <summary>
    /// Additional context to add to the conversation.
    /// </summary>
    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}
