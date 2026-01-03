using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Permission modes for tool execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PermissionMode>))]
public enum PermissionMode
{
    /// <summary>
    /// Default permission mode - prompts for dangerous operations.
    /// </summary>
    [EnumMember(Value = "default")]
    Default,

    /// <summary>
    /// Automatically accept file edits without prompting.
    /// </summary>
    [EnumMember(Value = "acceptEdits")]
    AcceptEdits,

    /// <summary>
    /// Plan-only mode - Claude can only propose changes, not execute them.
    /// </summary>
    [EnumMember(Value = "plan")]
    Plan,

    /// <summary>
    /// Bypass all permission checks (use with caution).
    /// </summary>
    [EnumMember(Value = "bypassPermissions")]
    BypassPermissions
}

/// <summary>
/// Hook events that can trigger callbacks during execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HookEvent>))]
public enum HookEvent
{
    /// <summary>
    /// Triggered before a tool is executed.
    /// </summary>
    [EnumMember(Value = "PreToolUse")]
    PreToolUse,

    /// <summary>
    /// Triggered after a tool has executed.
    /// </summary>
    [EnumMember(Value = "PostToolUse")]
    PostToolUse,

    /// <summary>
    /// Triggered when a user prompt is submitted.
    /// </summary>
    [EnumMember(Value = "UserPromptSubmit")]
    UserPromptSubmit,

    /// <summary>
    /// Triggered when the conversation stops.
    /// </summary>
    [EnumMember(Value = "Stop")]
    Stop,

    /// <summary>
    /// Triggered when a subagent stops.
    /// </summary>
    [EnumMember(Value = "SubagentStop")]
    SubagentStop,

    /// <summary>
    /// Triggered before context compaction.
    /// </summary>
    [EnumMember(Value = "PreCompact")]
    PreCompact
}

/// <summary>
/// Error types that can occur in assistant messages.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AssistantMessageErrorType>))]
public enum AssistantMessageErrorType
{
    /// <summary>
    /// Authentication with the API failed.
    /// </summary>
    [EnumMember(Value = "authentication_failed")]
    AuthenticationFailed,

    /// <summary>
    /// Billing-related error occurred.
    /// </summary>
    [EnumMember(Value = "billing_error")]
    BillingError,

    /// <summary>
    /// Rate limit was exceeded.
    /// </summary>
    [EnumMember(Value = "rate_limit")]
    RateLimit,

    /// <summary>
    /// The request was invalid.
    /// </summary>
    [EnumMember(Value = "invalid_request")]
    InvalidRequest,

    /// <summary>
    /// A server error occurred.
    /// </summary>
    [EnumMember(Value = "server_error")]
    ServerError,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    [EnumMember(Value = "unknown")]
    Unknown
}

/// <summary>
/// Permission behaviors for tool access.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PermissionBehavior>))]
public enum PermissionBehavior
{
    /// <summary>
    /// Allow the tool to be used.
    /// </summary>
    [EnumMember(Value = "allow")]
    Allow,

    /// <summary>
    /// Deny the tool from being used.
    /// </summary>
    [EnumMember(Value = "deny")]
    Deny,

    /// <summary>
    /// Ask the user for permission.
    /// </summary>
    [EnumMember(Value = "ask")]
    Ask
}

/// <summary>
/// Destinations for permission updates.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PermissionUpdateDestination>))]
public enum PermissionUpdateDestination
{
    /// <summary>
    /// Update user-level settings.
    /// </summary>
    [EnumMember(Value = "userSettings")]
    UserSettings,

    /// <summary>
    /// Update project-level settings.
    /// </summary>
    [EnumMember(Value = "projectSettings")]
    ProjectSettings,

    /// <summary>
    /// Update local settings.
    /// </summary>
    [EnumMember(Value = "localSettings")]
    LocalSettings,

    /// <summary>
    /// Update session-only settings.
    /// </summary>
    [EnumMember(Value = "session")]
    Session
}

/// <summary>
/// Types of permission updates.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PermissionUpdateType>))]
public enum PermissionUpdateType
{
    /// <summary>
    /// Add new permission rules.
    /// </summary>
    [EnumMember(Value = "addRules")]
    AddRules,

    /// <summary>
    /// Replace existing permission rules.
    /// </summary>
    [EnumMember(Value = "replaceRules")]
    ReplaceRules,

    /// <summary>
    /// Remove permission rules.
    /// </summary>
    [EnumMember(Value = "removeRules")]
    RemoveRules,

    /// <summary>
    /// Set the permission mode.
    /// </summary>
    [EnumMember(Value = "setMode")]
    SetMode,

    /// <summary>
    /// Add allowed directories.
    /// </summary>
    [EnumMember(Value = "addDirectories")]
    AddDirectories,

    /// <summary>
    /// Remove allowed directories.
    /// </summary>
    [EnumMember(Value = "removeDirectories")]
    RemoveDirectories
}

/// <summary>
/// Sources for settings in hierarchical configuration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SettingSource>))]
public enum SettingSource
{
    /// <summary>
    /// User-level settings.
    /// </summary>
    [EnumMember(Value = "user")]
    User,

    /// <summary>
    /// Project-level settings.
    /// </summary>
    [EnumMember(Value = "project")]
    Project,

    /// <summary>
    /// Local settings.
    /// </summary>
    [EnumMember(Value = "local")]
    Local
}

/// <summary>
/// Model selection for agents.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AgentModel>))]
public enum AgentModel
{
    /// <summary>
    /// Use Claude Sonnet model.
    /// </summary>
    [EnumMember(Value = "sonnet")]
    Sonnet,

    /// <summary>
    /// Use Claude Opus model.
    /// </summary>
    [EnumMember(Value = "opus")]
    Opus,

    /// <summary>
    /// Use Claude Haiku model.
    /// </summary>
    [EnumMember(Value = "haiku")]
    Haiku,

    /// <summary>
    /// Inherit model from parent agent.
    /// </summary>
    [EnumMember(Value = "inherit")]
    Inherit
}

/// <summary>
/// Extension methods for hook events.
/// </summary>
public static class HookEventExtensions
{
    /// <summary>
    /// Converts a hook event to its string representation.
    /// </summary>
    /// <param name="evt">The hook event.</param>
    /// <returns>The string name of the event.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown event types.</exception>
    public static string ToEventName(this HookEvent evt) => evt switch
    {
        HookEvent.PreToolUse => "PreToolUse",
        HookEvent.PostToolUse => "PostToolUse",
        HookEvent.UserPromptSubmit => "UserPromptSubmit",
        HookEvent.Stop => "Stop",
        HookEvent.SubagentStop => "SubagentStop",
        HookEvent.PreCompact => "PreCompact",
        _ => throw new ArgumentOutOfRangeException(nameof(evt))
    };
}
