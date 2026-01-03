using System.Text.Json;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Interface for SDK-hosted MCP (Model Context Protocol) servers.
/// </summary>
/// <remarks>
/// Implement this interface to create custom MCP servers that run in-process.
/// Use <see cref="Mcp.SdkMcpServer"/> for a convenient implementation.
/// </remarks>
public interface ISdkMcpServer
{
    /// <summary>
    /// The name of this MCP server.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The version of this MCP server.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Lists all tools available from this server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of tool definitions.</returns>
    Task<IReadOnlyList<SdkMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a tool with the given arguments.
    /// </summary>
    /// <param name="name">The name of the tool to invoke.</param>
    /// <param name="arguments">The JSON arguments for the tool.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the tool invocation.</returns>
    Task<SdkMcpToolResult> CallToolAsync(
        string name,
        JsonElement arguments,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Definition of an MCP tool.
/// </summary>
public sealed record SdkMcpToolDefinition
{
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A description of what the tool does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// JSON Schema for the tool's input parameters.
    /// </summary>
    public required JsonElement InputSchema { get; init; }
}

/// <summary>
/// Result of an MCP tool invocation.
/// </summary>
public sealed record SdkMcpToolResult
{
    /// <summary>
    /// The content blocks returned by the tool.
    /// </summary>
    public required IReadOnlyList<SdkMcpContent> Content { get; init; }

    /// <summary>
    /// Whether the tool execution resulted in an error.
    /// </summary>
    public bool? IsError { get; init; }
}

/// <summary>
/// Base class for MCP content blocks.
/// </summary>
public abstract record SdkMcpContent;

/// <summary>
/// Text content from an MCP tool.
/// </summary>
public sealed record SdkMcpTextContent : SdkMcpContent
{
    /// <summary>
    /// The text content.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Image content from an MCP tool.
/// </summary>
public sealed record SdkMcpImageContent : SdkMcpContent
{
    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// The MIME type of the image.
    /// </summary>
    public required string MimeType { get; init; }
}
