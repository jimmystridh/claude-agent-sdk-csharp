using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Types;

namespace ih0.Claude.Agent.SDK.Internal;

public static class MessageParser
{
    public static Message Parse(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            throw new MessageParseException(
                $"Invalid message data type: expected dict, got {data.ValueKind}");
        }

        if (!data.TryGetProperty("type", out var typeElement))
        {
            throw new MessageParseException("Message missing 'type' field");
        }

        var type = typeElement.GetString();

        return type switch
        {
            "user" => ParseUserMessage(data),
            "assistant" => ParseAssistantMessage(data),
            "system" => ParseSystemMessage(data),
            "result" => ParseResultMessage(data),
            "stream_event" => ParseStreamEvent(data),
            _ => throw new MessageParseException($"Unknown message type: {type}", data)
        };
    }

    private static UserMessage ParseUserMessage(JsonElement data)
    {
        if (!data.TryGetProperty("message", out var messageEl))
        {
            throw new MessageParseException("Missing required field in user message: message");
        }

        if (!messageEl.TryGetProperty("content", out var contentEl))
        {
            throw new MessageParseException("Missing required field in user message: content");
        }

        var content = ParseContent(contentEl);
        string? uuid = data.TryGetProperty("uuid", out var uuidEl) ? uuidEl.GetString() : null;
        string? parentToolUseId = data.TryGetProperty("parent_tool_use_id", out var pEl) && pEl.ValueKind != JsonValueKind.Null
            ? pEl.GetString()
            : null;

        return new UserMessage
        {
            Content = content,
            Uuid = uuid,
            ParentToolUseId = parentToolUseId
        };
    }

    private static AssistantMessage ParseAssistantMessage(JsonElement data)
    {
        if (!data.TryGetProperty("message", out var messageEl))
        {
            throw new MessageParseException("Missing required field in assistant message: message");
        }

        if (!messageEl.TryGetProperty("content", out var contentEl))
        {
            throw new MessageParseException("Missing required field in assistant message: content");
        }

        if (!messageEl.TryGetProperty("model", out var modelEl))
        {
            throw new MessageParseException("Missing required field in assistant message: model");
        }

        var contentBlocks = ParseContentBlocks(contentEl);
        var model = modelEl.GetString() ?? throw new MessageParseException("Model cannot be null");

        string? parentToolUseId = data.TryGetProperty("parent_tool_use_id", out var pEl) && pEl.ValueKind != JsonValueKind.Null
            ? pEl.GetString()
            : null;

        AssistantMessageErrorType? error = null;
        if (messageEl.TryGetProperty("error", out var errorEl) && errorEl.ValueKind == JsonValueKind.String)
        {
            var errorStr = errorEl.GetString();
            error = errorStr switch
            {
                "authentication_failed" => AssistantMessageErrorType.AuthenticationFailed,
                "billing_error" => AssistantMessageErrorType.BillingError,
                "rate_limit" => AssistantMessageErrorType.RateLimit,
                "invalid_request" => AssistantMessageErrorType.InvalidRequest,
                "server_error" => AssistantMessageErrorType.ServerError,
                _ => AssistantMessageErrorType.Unknown
            };
        }

        return new AssistantMessage
        {
            Content = contentBlocks,
            Model = model,
            ParentToolUseId = parentToolUseId,
            Error = error
        };
    }

    private static SystemMessage ParseSystemMessage(JsonElement data)
    {
        if (!data.TryGetProperty("subtype", out var subtypeEl))
        {
            throw new MessageParseException("Missing required field in system message: subtype");
        }

        var subtype = subtypeEl.GetString() ?? throw new MessageParseException("Subtype cannot be null");

        // Create a copy of the data without type and subtype as the "data" field
        var dataDict = new Dictionary<string, JsonElement>();
        foreach (var prop in data.EnumerateObject())
        {
            if (prop.Name != "type" && prop.Name != "subtype")
            {
                dataDict[prop.Name] = prop.Value.Clone();
            }
        }

        var dataJson = JsonSerializer.Serialize(dataDict);
        using var doc = JsonDocument.Parse(dataJson);

        return new SystemMessage
        {
            Subtype = subtype,
            Data = doc.RootElement.Clone()
        };
    }

    private static ResultMessage ParseResultMessage(JsonElement data)
    {
        if (!data.TryGetProperty("subtype", out var subtypeEl))
            throw new MessageParseException("Missing required field in result message: subtype");
        if (!data.TryGetProperty("duration_ms", out var durationMsEl))
            throw new MessageParseException("Missing required field in result message: duration_ms");
        if (!data.TryGetProperty("duration_api_ms", out var durationApiMsEl))
            throw new MessageParseException("Missing required field in result message: duration_api_ms");
        if (!data.TryGetProperty("is_error", out var isErrorEl))
            throw new MessageParseException("Missing required field in result message: is_error");
        if (!data.TryGetProperty("num_turns", out var numTurnsEl))
            throw new MessageParseException("Missing required field in result message: num_turns");
        if (!data.TryGetProperty("session_id", out var sessionIdEl))
            throw new MessageParseException("Missing required field in result message: session_id");

        return new ResultMessage
        {
            Subtype = subtypeEl.GetString() ?? "",
            DurationMs = durationMsEl.GetInt32(),
            DurationApiMs = durationApiMsEl.GetInt32(),
            IsError = isErrorEl.GetBoolean(),
            NumTurns = numTurnsEl.GetInt32(),
            SessionId = sessionIdEl.GetString() ?? "",
            TotalCostUsd = data.TryGetProperty("total_cost_usd", out var costEl) && costEl.ValueKind == JsonValueKind.Number
                ? costEl.GetDouble()
                : null,
            Usage = data.TryGetProperty("usage", out var usageEl) && usageEl.ValueKind != JsonValueKind.Null
                ? usageEl.Clone()
                : null,
            Result = data.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.String
                ? resultEl.GetString()
                : null,
            StructuredOutput = data.TryGetProperty("structured_output", out var structuredEl) && structuredEl.ValueKind != JsonValueKind.Null
                ? structuredEl.Clone()
                : null
        };
    }

    private static StreamEvent ParseStreamEvent(JsonElement data)
    {
        if (!data.TryGetProperty("uuid", out var uuidEl))
            throw new MessageParseException("Missing required field in stream event: uuid");
        if (!data.TryGetProperty("session_id", out var sessionIdEl))
            throw new MessageParseException("Missing required field in stream event: session_id");
        if (!data.TryGetProperty("event", out var eventEl))
            throw new MessageParseException("Missing required field in stream event: event");

        string? parentToolUseId = data.TryGetProperty("parent_tool_use_id", out var pEl) && pEl.ValueKind != JsonValueKind.Null
            ? pEl.GetString()
            : null;

        return new StreamEvent
        {
            Uuid = uuidEl.GetString() ?? "",
            SessionId = sessionIdEl.GetString() ?? "",
            Event = eventEl.Clone(),
            ParentToolUseId = parentToolUseId
        };
    }

    private static OneOf.OneOf<string, IReadOnlyList<ContentBlock>> ParseContent(JsonElement contentEl)
    {
        if (contentEl.ValueKind == JsonValueKind.String)
        {
            return OneOf.OneOf<string, IReadOnlyList<ContentBlock>>.FromT0(contentEl.GetString() ?? "");
        }

        if (contentEl.ValueKind == JsonValueKind.Array)
        {
            return OneOf.OneOf<string, IReadOnlyList<ContentBlock>>.FromT1(ParseContentBlocks(contentEl));
        }

        throw new MessageParseException($"Invalid content type: {contentEl.ValueKind}");
    }

    private static IReadOnlyList<ContentBlock> ParseContentBlocks(JsonElement contentEl)
    {
        var blocks = new List<ContentBlock>();

        foreach (var blockEl in contentEl.EnumerateArray())
        {
            if (!blockEl.TryGetProperty("type", out var typeEl))
                continue;

            var type = typeEl.GetString();
            ContentBlock block = type switch
            {
                "text" => new TextBlock
                {
                    Text = blockEl.GetProperty("text").GetString() ?? ""
                },
                "thinking" => new ThinkingBlock
                {
                    Thinking = blockEl.GetProperty("thinking").GetString() ?? "",
                    Signature = blockEl.GetProperty("signature").GetString() ?? ""
                },
                "tool_use" => new ToolUseBlock
                {
                    Id = blockEl.GetProperty("id").GetString() ?? "",
                    Name = blockEl.GetProperty("name").GetString() ?? "",
                    Input = blockEl.GetProperty("input").Clone()
                },
                "tool_result" => new ToolResultBlock
                {
                    ToolUseId = blockEl.GetProperty("tool_use_id").GetString() ?? "",
                    Content = blockEl.TryGetProperty("content", out var cEl) && cEl.ValueKind != JsonValueKind.Null
                        ? cEl.Clone()
                        : null,
                    IsError = blockEl.TryGetProperty("is_error", out var eEl) && eEl.ValueKind == JsonValueKind.True
                        ? true
                        : null
                },
                _ => throw new MessageParseException($"Unknown content block type: {type}")
            };

            blocks.Add(block);
        }

        return blocks;
    }
}
