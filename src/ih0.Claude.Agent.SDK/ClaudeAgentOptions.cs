using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using OneOf;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Callback for custom tool permission handling.
/// </summary>
/// <param name="toolName">The name of the tool being invoked.</param>
/// <param name="input">The tool's input parameters as JSON.</param>
/// <param name="context">Additional context about the tool invocation.</param>
/// <returns>A permission result indicating whether the tool can be used.</returns>
public delegate Task<PermissionResult> CanUseToolCallback(
    string toolName,
    JsonElement input,
    ToolPermissionContext context);

/// <summary>
/// Configuration options for Claude Agent SDK queries and clients.
/// </summary>
/// <remarks>
/// This record contains all configurable options for interacting with Claude Code,
/// including tool permissions, model selection, MCP servers, hooks, and various limits.
/// </remarks>
public sealed record ClaudeAgentOptions
{
    /// <summary>
    /// The tools available to Claude. Can be a list of tool names or a <see cref="ToolsPreset"/>.
    /// </summary>
    public OneOf<IReadOnlyList<string>, ToolsPreset>? Tools { get; init; }

    /// <summary>
    /// Explicit list of tools that Claude is allowed to use.
    /// </summary>
    public IReadOnlyList<string>? AllowedTools { get; init; }

    /// <summary>
    /// Explicit list of tools that Claude is not allowed to use.
    /// </summary>
    public IReadOnlyList<string>? DisallowedTools { get; init; }

    /// <summary>
    /// The permission mode for tool usage.
    /// </summary>
    public PermissionMode? PermissionMode { get; init; }

    /// <summary>
    /// Custom system prompt or a <see cref="SystemPromptPreset"/>.
    /// </summary>
    public OneOf<string, SystemPromptPreset>? SystemPrompt { get; init; }

    /// <summary>
    /// The primary model to use for queries (e.g., "claude-sonnet-4-20250514").
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Fallback model to use if the primary model is unavailable.
    /// </summary>
    public string? FallbackModel { get; init; }

    /// <summary>
    /// Whether to continue an existing conversation.
    /// </summary>
    public bool? ContinueConversation { get; init; }

    /// <summary>
    /// Session ID to resume from a previous conversation.
    /// </summary>
    public string? Resume { get; init; }

    /// <summary>
    /// Whether to fork the session when resuming.
    /// </summary>
    public bool? ForkSession { get; init; }

    /// <summary>
    /// Maximum number of conversation turns before stopping.
    /// </summary>
    public int? MaxTurns { get; init; }

    /// <summary>
    /// Maximum budget in USD for API usage.
    /// </summary>
    public double? MaxBudgetUsd { get; init; }

    /// <summary>
    /// MCP server configurations. Can be a dictionary of server configs or a path to a config file.
    /// </summary>
    public OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>? McpServers { get; init; }

    /// <summary>
    /// Callback for custom tool permission decisions.
    /// </summary>
    /// <remarks>
    /// When set, this callback is invoked before each tool use to determine if it should be allowed.
    /// Cannot be used together with <see cref="PermissionPromptToolName"/>.
    /// </remarks>
    public CanUseToolCallback? CanUseTool { get; init; }

    /// <summary>
    /// Hook configurations for various lifecycle events.
    /// </summary>
    public IReadOnlyDictionary<HookEvent, IReadOnlyList<HookMatcher>>? Hooks { get; init; }

    /// <summary>
    /// Callback for stderr output from Claude Code.
    /// </summary>
    public Action<string>? Stderr { get; init; }

    /// <summary>
    /// Working directory for Claude Code execution.
    /// </summary>
    public string? Cwd { get; init; }

    /// <summary>
    /// Custom path to the Claude CLI executable.
    /// </summary>
    public string? CliPath { get; init; }

    /// <summary>
    /// Environment variables to pass to Claude Code.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Env { get; init; }

    /// <summary>
    /// Additional directories to include in the context.
    /// </summary>
    public IReadOnlyList<string>? AddDirs { get; init; }

    /// <summary>
    /// Path to settings file or inline settings JSON.
    /// </summary>
    public string? Settings { get; init; }

    /// <summary>
    /// Tool name used for permission prompts in streaming mode.
    /// </summary>
    /// <remarks>
    /// Set automatically to "stdio" when <see cref="CanUseTool"/> is provided.
    /// Cannot be set together with <see cref="CanUseTool"/>.
    /// </remarks>
    public string? PermissionPromptToolName { get; init; }

    /// <summary>
    /// Beta features to enable.
    /// </summary>
    public IReadOnlyList<string>? Betas { get; init; }

    /// <summary>
    /// Maximum tokens for extended thinking.
    /// </summary>
    public int? MaxThinkingTokens { get; init; }

    /// <summary>
    /// Whether to enable file checkpointing for undo support.
    /// </summary>
    public bool? EnableFileCheckpointing { get; init; }

    /// <summary>
    /// Sandbox configuration for isolated execution.
    /// </summary>
    public SandboxSettings? Sandbox { get; init; }

    /// <summary>
    /// Output format configuration.
    /// </summary>
    public JsonElement? OutputFormat { get; init; }

    /// <summary>
    /// Whether to include partial messages during streaming.
    /// </summary>
    public bool? IncludePartialMessages { get; init; }

    /// <summary>
    /// Maximum buffer size for streaming.
    /// </summary>
    public int? MaxBufferSize { get; init; }

    /// <summary>
    /// User identifier for API tracking.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Agent definitions for multi-agent scenarios.
    /// </summary>
    public IReadOnlyDictionary<string, AgentDefinition>? Agents { get; init; }

    /// <summary>
    /// Setting sources for hierarchical configuration.
    /// </summary>
    public IReadOnlyList<SettingSource>? SettingSources { get; init; }

    /// <summary>
    /// SDK plugin configurations.
    /// </summary>
    public IReadOnlyList<SdkPluginConfig>? Plugins { get; init; }

    /// <summary>
    /// Extra command-line arguments to pass to Claude Code.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? ExtraArgs { get; init; }
}
