using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Base class for MCP (Model Context Protocol) server configurations.
/// </summary>
public abstract record McpServerConfig
{
    /// <summary>
    /// The type of MCP server.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Configuration for an MCP server using stdio transport.
/// </summary>
public sealed record McpStdioServerConfig : McpServerConfig
{
    /// <summary>
    /// The type identifier for stdio servers.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type => "stdio";

    /// <summary>
    /// The command to execute to start the server.
    /// </summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    /// <summary>
    /// Command-line arguments for the server.
    /// </summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    /// <summary>
    /// Environment variables for the server process.
    /// </summary>
    [JsonPropertyName("env")]
    public IReadOnlyDictionary<string, string>? Env { get; init; }
}

/// <summary>
/// Configuration for an MCP server using Server-Sent Events transport.
/// </summary>
public sealed record McpSseServerConfig : McpServerConfig
{
    /// <summary>
    /// The type identifier for SSE servers.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type => "sse";

    /// <summary>
    /// The URL of the SSE endpoint.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// HTTP headers to include in requests.
    /// </summary>
    [JsonPropertyName("headers")]
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Configuration for an MCP server using HTTP transport.
/// </summary>
public sealed record McpHttpServerConfig : McpServerConfig
{
    /// <summary>
    /// The type identifier for HTTP servers.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type => "http";

    /// <summary>
    /// The URL of the HTTP endpoint.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// HTTP headers to include in requests.
    /// </summary>
    [JsonPropertyName("headers")]
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Configuration for an SDK-hosted MCP server (in-process).
/// </summary>
public sealed record McpSdkServerConfig : McpServerConfig
{
    /// <summary>
    /// The type identifier for SDK servers.
    /// </summary>
    [JsonPropertyName("type")]
    public override string Type => "sdk";

    /// <summary>
    /// The name of this MCP server.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The SDK MCP server instance.
    /// </summary>
    [JsonIgnore]
    public required ISdkMcpServer Instance { get; init; }
}

/// <summary>
/// Network configuration for sandbox mode.
/// </summary>
public sealed record SandboxNetworkConfig
{
    /// <summary>
    /// List of Unix socket paths to allow.
    /// </summary>
    [JsonPropertyName("allowUnixSockets")]
    public IReadOnlyList<string>? AllowUnixSockets { get; init; }

    /// <summary>
    /// Whether to allow all Unix socket connections.
    /// </summary>
    [JsonPropertyName("allowAllUnixSockets")]
    public bool? AllowAllUnixSockets { get; init; }

    /// <summary>
    /// Whether to allow binding to local ports.
    /// </summary>
    [JsonPropertyName("allowLocalBinding")]
    public bool? AllowLocalBinding { get; init; }

    /// <summary>
    /// Port for the HTTP proxy.
    /// </summary>
    [JsonPropertyName("httpProxyPort")]
    public int? HttpProxyPort { get; init; }

    /// <summary>
    /// Port for the SOCKS proxy.
    /// </summary>
    [JsonPropertyName("socksProxyPort")]
    public int? SocksProxyPort { get; init; }
}

/// <summary>
/// Configuration for ignoring sandbox violations.
/// </summary>
public sealed record SandboxIgnoreViolations
{
    /// <summary>
    /// File access violations to ignore.
    /// </summary>
    [JsonPropertyName("file")]
    public IReadOnlyList<string>? File { get; init; }

    /// <summary>
    /// Network violations to ignore.
    /// </summary>
    [JsonPropertyName("network")]
    public IReadOnlyList<string>? Network { get; init; }
}

/// <summary>
/// Configuration for sandbox mode execution.
/// </summary>
public sealed record SandboxSettings
{
    /// <summary>
    /// Whether sandboxing is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>
    /// Whether to auto-allow bash commands when sandboxed.
    /// </summary>
    [JsonPropertyName("autoAllowBashIfSandboxed")]
    public bool? AutoAllowBashIfSandboxed { get; init; }

    /// <summary>
    /// Commands to exclude from sandboxing.
    /// </summary>
    [JsonPropertyName("excludedCommands")]
    public IReadOnlyList<string>? ExcludedCommands { get; init; }

    /// <summary>
    /// Whether to allow unsandboxed command execution.
    /// </summary>
    [JsonPropertyName("allowUnsandboxedCommands")]
    public bool? AllowUnsandboxedCommands { get; init; }

    /// <summary>
    /// Network configuration for the sandbox.
    /// </summary>
    [JsonPropertyName("network")]
    public SandboxNetworkConfig? Network { get; init; }

    /// <summary>
    /// Violations to ignore.
    /// </summary>
    [JsonPropertyName("ignoreViolations")]
    public SandboxIgnoreViolations? IgnoreViolations { get; init; }

    /// <summary>
    /// Whether to enable weaker nested sandbox mode.
    /// </summary>
    [JsonPropertyName("enableWeakerNestedSandbox")]
    public bool? EnableWeakerNestedSandbox { get; init; }
}

/// <summary>
/// Configuration for an SDK plugin.
/// </summary>
public sealed record SdkPluginConfig
{
    /// <summary>
    /// The type of plugin.
    /// </summary>
    [JsonPropertyName("type")]
    public required string PluginType { get; init; }

    /// <summary>
    /// Path to the plugin.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

/// <summary>
/// Definition for a subagent.
/// </summary>
public sealed record AgentDefinition
{
    /// <summary>
    /// Description of what the agent does.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// The system prompt for the agent.
    /// </summary>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    /// <summary>
    /// Tools available to the agent.
    /// </summary>
    [JsonPropertyName("tools")]
    public IReadOnlyList<string>? Tools { get; init; }

    /// <summary>
    /// The model for the agent to use.
    /// </summary>
    [JsonPropertyName("model")]
    public AgentModel? Model { get; init; }
}

/// <summary>
/// A preset system prompt configuration.
/// </summary>
public sealed record SystemPromptPreset
{
    /// <summary>
    /// The type identifier.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "preset";

    /// <summary>
    /// The preset name.
    /// </summary>
    [JsonPropertyName("preset")]
    public string Preset { get; init; } = "claude_code";

    /// <summary>
    /// Additional text to append to the preset prompt.
    /// </summary>
    [JsonPropertyName("append")]
    public string? Append { get; init; }
}

/// <summary>
/// A preset tools configuration.
/// </summary>
public sealed record ToolsPreset
{
    /// <summary>
    /// The type identifier.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "preset";

    /// <summary>
    /// The preset name.
    /// </summary>
    [JsonPropertyName("preset")]
    public string Preset { get; init; } = "claude_code";
}
