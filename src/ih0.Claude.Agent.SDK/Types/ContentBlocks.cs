using System.Text.Json;
using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Base class for content blocks within messages.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ThinkingBlock), "thinking")]
[JsonDerivedType(typeof(ToolUseBlock), "tool_use")]
[JsonDerivedType(typeof(ToolResultBlock), "tool_result")]
public abstract record ContentBlock;

/// <summary>
/// A content block containing plain text.
/// </summary>
public sealed record TextBlock : ContentBlock
{
    /// <summary>
    /// The text content.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

/// <summary>
/// A content block containing Claude's extended thinking process.
/// </summary>
public sealed record ThinkingBlock : ContentBlock
{
    /// <summary>
    /// The thinking content.
    /// </summary>
    [JsonPropertyName("thinking")]
    public required string Thinking { get; init; }

    /// <summary>
    /// Cryptographic signature for the thinking block.
    /// </summary>
    [JsonPropertyName("signature")]
    public required string Signature { get; init; }
}

/// <summary>
/// A content block representing a tool invocation by Claude.
/// </summary>
public sealed record ToolUseBlock : ContentBlock
{
    /// <summary>
    /// Unique identifier for this tool use.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The name of the tool being invoked.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The input parameters for the tool.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }
}

/// <summary>
/// A content block containing the result of a tool execution.
/// </summary>
public sealed record ToolResultBlock : ContentBlock
{
    /// <summary>
    /// The ID of the tool use this result corresponds to.
    /// </summary>
    [JsonPropertyName("tool_use_id")]
    public required string ToolUseId { get; init; }

    /// <summary>
    /// The content of the tool result.
    /// </summary>
    [JsonPropertyName("content")]
    public JsonElement? Content { get; init; }

    /// <summary>
    /// Whether the tool execution resulted in an error.
    /// </summary>
    [JsonPropertyName("is_error")]
    public bool? IsError { get; init; }
}
