using System.Reflection;
using System.Text.Json;

namespace ih0.Claude.Agent.SDK.Mcp;

/// <summary>
/// A simple implementation of <see cref="ISdkMcpServer"/> for hosting MCP tools in-process.
/// </summary>
/// <example>
/// <code>
/// var server = SdkMcpServer.Create("calculator", "1.0.0", new[]
/// {
///     SdkMcpToolBuilder.CreateTool("add", "Add two numbers",
///         new Dictionary&lt;string, Type&gt; { ["a"] = typeof(int), ["b"] = typeof(int) },
///         (args, ct) => Task.FromResult(SdkMcpToolBuilder.TextResult(
///             (args.GetProperty("a").GetInt32() + args.GetProperty("b").GetInt32()).ToString())))
/// });
/// </code>
/// </example>
public sealed class SdkMcpServer : ISdkMcpServer
{
    private readonly Dictionary<string, SdkMcpTool> _tools = new();

    /// <summary>
    /// The name of this MCP server.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The version of this MCP server.
    /// </summary>
    public string Version { get; }

    private SdkMcpServer(string name, string version)
    {
        Name = name;
        Version = version;
    }

    /// <summary>
    /// Creates a new MCP server with the specified tools.
    /// </summary>
    /// <param name="name">The name of the server.</param>
    /// <param name="version">The version of the server.</param>
    /// <param name="tools">The tools to register.</param>
    /// <returns>A new MCP server instance.</returns>
    public static SdkMcpServer Create(
        string name,
        string version = "1.0.0",
        IEnumerable<SdkMcpTool>? tools = null)
    {
        var server = new SdkMcpServer(name, version);

        if (tools != null)
        {
            foreach (var tool in tools)
            {
                server._tools[tool.Name] = tool;
            }
        }

        return server;
    }

    /// <summary>
    /// Lists all tools available from this server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of tool definitions.</returns>
    public Task<IReadOnlyList<SdkMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = _tools.Values.Select(t => new SdkMcpToolDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema,
            Annotations = t.Annotations
        }).ToList();

        return Task.FromResult<IReadOnlyList<SdkMcpToolDefinition>>(definitions);
    }

    /// <summary>
    /// Invokes a tool with the given arguments.
    /// </summary>
    /// <param name="name">The name of the tool to invoke.</param>
    /// <param name="arguments">The JSON arguments for the tool.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the tool invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tool is not found.</exception>
    public async Task<SdkMcpToolResult> CallToolAsync(
        string name,
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            throw new InvalidOperationException($"Tool '{name}' not found");
        }

        return await tool.Handler(arguments, cancellationToken);
    }
}

/// <summary>
/// Represents an MCP tool with its definition and handler.
/// </summary>
public sealed record SdkMcpTool
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

    /// <summary>
    /// The handler function that executes the tool.
    /// </summary>
    public required Func<JsonElement, CancellationToken, Task<SdkMcpToolResult>> Handler { get; init; }

    /// <summary>
    /// Optional annotations describing tool behavior hints.
    /// </summary>
    public ToolAnnotations? Annotations { get; init; }
}

/// <summary>
/// Helper class for building MCP tools.
/// </summary>
public static class SdkMcpToolBuilder
{
    /// <summary>
    /// Creates a new MCP tool with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">A description of what the tool does.</param>
    /// <param name="parameters">The parameter names and types.</param>
    /// <param name="handler">The handler function.</param>
    /// <param name="annotations">Optional tool behavior annotations.</param>
    /// <returns>A new tool definition.</returns>
    public static SdkMcpTool CreateTool(
        string name,
        string description,
        Dictionary<string, Type> parameters,
        Func<JsonElement, CancellationToken, Task<SdkMcpToolResult>> handler,
        ToolAnnotations? annotations = null)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var (paramName, paramType) in parameters)
        {
            string jsonType = paramType switch
            {
                var t when t == typeof(string) => "string",
                var t when t == typeof(int) || t == typeof(long) => "integer",
                var t when t == typeof(float) || t == typeof(double) || t == typeof(decimal) => "number",
                var t when t == typeof(bool) => "boolean",
                _ => "string"
            };

            properties[paramName] = new Dictionary<string, string> { ["type"] = jsonType };
            required.Add(paramName);
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };

        var schemaJson = JsonSerializer.Serialize(schema);
        var schemaElement = JsonDocument.Parse(schemaJson).RootElement.Clone();

        return new SdkMcpTool
        {
            Name = name,
            Description = description,
            InputSchema = schemaElement,
            Handler = handler,
            Annotations = annotations
        };
    }

    /// <summary>
    /// Creates a successful text result.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>A tool result with text content.</returns>
    public static SdkMcpToolResult TextResult(string text)
    {
        return new SdkMcpToolResult
        {
            Content = new List<SdkMcpContent>
            {
                new SdkMcpTextContent { Text = text }
            }
        };
    }

    /// <summary>
    /// Creates an error result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A tool result indicating an error.</returns>
    public static SdkMcpToolResult ErrorResult(string error)
    {
        return new SdkMcpToolResult
        {
            Content = new List<SdkMcpContent>
            {
                new SdkMcpTextContent { Text = error }
            },
            IsError = true
        };
    }
}
