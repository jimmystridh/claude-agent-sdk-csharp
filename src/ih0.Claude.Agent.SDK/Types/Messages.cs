using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Base class for all message types in the Claude Agent SDK.
/// </summary>
public abstract record Message;

/// <summary>
/// Represents a message from the user.
/// </summary>
public sealed record UserMessage : Message
{
    /// <summary>
    /// The content of the user message. Can be a simple string or a list of content blocks.
    /// </summary>
    public required OneOf<string, IReadOnlyList<ContentBlock>> Content { get; init; }

    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string? Uuid { get; init; }

    /// <summary>
    /// The ID of the parent tool use that triggered this message.
    /// </summary>
    public string? ParentToolUseId { get; init; }
}

/// <summary>
/// Represents a message from Claude (the assistant).
/// </summary>
public sealed record AssistantMessage : Message
{
    /// <summary>
    /// The content blocks of the assistant's response.
    /// </summary>
    public required IReadOnlyList<ContentBlock> Content { get; init; }

    /// <summary>
    /// The model that generated this response.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The ID of the parent tool use that triggered this message.
    /// </summary>
    public string? ParentToolUseId { get; init; }

    /// <summary>
    /// Error information if the message generation failed.
    /// </summary>
    public AssistantMessageErrorType? Error { get; init; }
}

/// <summary>
/// Represents a system-level message with metadata.
/// </summary>
public sealed record SystemMessage : Message
{
    /// <summary>
    /// The subtype of the system message.
    /// </summary>
    public required string Subtype { get; init; }

    /// <summary>
    /// The data payload of the system message.
    /// </summary>
    public required JsonElement Data { get; init; }
}

/// <summary>
/// Represents the final result of a conversation or turn.
/// </summary>
public sealed record ResultMessage : Message
{
    /// <summary>
    /// The subtype of the result (e.g., "success", "error", "end_turn").
    /// </summary>
    public required string Subtype { get; init; }

    /// <summary>
    /// Total duration of the operation in milliseconds.
    /// </summary>
    public required int DurationMs { get; init; }

    /// <summary>
    /// Duration spent waiting for API responses in milliseconds.
    /// </summary>
    public required int DurationApiMs { get; init; }

    /// <summary>
    /// Whether the result indicates an error.
    /// </summary>
    public required bool IsError { get; init; }

    /// <summary>
    /// Number of conversation turns completed.
    /// </summary>
    public required int NumTurns { get; init; }

    /// <summary>
    /// The session ID for this conversation.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Total cost of API usage in USD.
    /// </summary>
    public double? TotalCostUsd { get; init; }

    /// <summary>
    /// Token usage information.
    /// </summary>
    public JsonElement? Usage { get; init; }

    /// <summary>
    /// The final result text, if any.
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Structured output from the conversation.
    /// </summary>
    public JsonElement? StructuredOutput { get; init; }
}

/// <summary>
/// Represents a streaming event during message generation.
/// </summary>
public sealed record StreamEvent : Message
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public required string Uuid { get; init; }

    /// <summary>
    /// The session ID for this conversation.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The event payload.
    /// </summary>
    public required JsonElement Event { get; init; }

    /// <summary>
    /// The ID of the parent tool use that triggered this event.
    /// </summary>
    public string? ParentToolUseId { get; init; }
}
