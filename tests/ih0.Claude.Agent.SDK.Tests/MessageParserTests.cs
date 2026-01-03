using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class MessageParserTests
{
    [Fact]
    public void Parse_ValidUserMessage_ReturnsUserMessage()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {"content": [{"type": "text", "text": "Hello"}]}
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        userMessage.Content.IsT1.Should().BeTrue();
        var blocks = userMessage.Content.AsT1;
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<TextBlock>();
        ((TextBlock)blocks[0]).Text.Should().Be("Hello");
    }

    [Fact]
    public void Parse_UserMessageWithUuid_ExtractsUuid()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "uuid": "msg-abc123-def456",
            "message": {"content": [{"type": "text", "text": "Hello"}]}
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        userMessage.Uuid.Should().Be("msg-abc123-def456");
    }

    [Fact]
    public void Parse_UserMessageWithToolUse_ParsesToolUseBlock()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {
                "content": [
                    {"type": "text", "text": "Let me read this file"},
                    {
                        "type": "tool_use",
                        "id": "tool_456",
                        "name": "Read",
                        "input": {"file_path": "/example.txt"}
                    }
                ]
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        userMessage.Content.IsT1.Should().BeTrue();
        var blocks = userMessage.Content.AsT1;
        blocks.Should().HaveCount(2);
        blocks[0].Should().BeOfType<TextBlock>();
        blocks[1].Should().BeOfType<ToolUseBlock>();
        var toolUse = (ToolUseBlock)blocks[1];
        toolUse.Id.Should().Be("tool_456");
        toolUse.Name.Should().Be("Read");
    }

    [Fact]
    public void Parse_UserMessageWithToolResult_ParsesToolResultBlock()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {
                "content": [
                    {
                        "type": "tool_result",
                        "tool_use_id": "tool_789",
                        "content": "File contents here"
                    }
                ]
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        userMessage.Content.IsT1.Should().BeTrue();
        var blocks = userMessage.Content.AsT1;
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<ToolResultBlock>();
        var toolResult = (ToolResultBlock)blocks[0];
        toolResult.ToolUseId.Should().Be("tool_789");
    }

    [Fact]
    public void Parse_UserMessageWithToolResultError_ParsesIsError()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {
                "content": [
                    {
                        "type": "tool_result",
                        "tool_use_id": "tool_error",
                        "content": "File not found",
                        "is_error": true
                    }
                ]
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        var blocks = userMessage.Content.AsT1;
        var toolResult = (ToolResultBlock)blocks[0];
        toolResult.IsError.Should().BeTrue();
    }

    [Fact]
    public void Parse_UserMessageWithMixedContent_ParsesAllBlocks()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {
                "content": [
                    {"type": "text", "text": "Here's what I found:"},
                    {"type": "tool_use", "id": "use_1", "name": "Search", "input": {"query": "test"}},
                    {"type": "tool_result", "tool_use_id": "use_1", "content": "Search results"},
                    {"type": "text", "text": "What do you think?"}
                ]
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        var blocks = userMessage.Content.AsT1;
        blocks.Should().HaveCount(4);
        blocks[0].Should().BeOfType<TextBlock>();
        blocks[1].Should().BeOfType<ToolUseBlock>();
        blocks[2].Should().BeOfType<ToolResultBlock>();
        blocks[3].Should().BeOfType<TextBlock>();
    }

    [Fact]
    public void Parse_UserMessageInsideSubagent_ParsesParentToolUseId()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "user",
            "message": {"content": [{"type": "text", "text": "Hello"}]},
            "parent_tool_use_id": "toolu_01Xrwd5Y13sEHtzScxR77So8"
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<UserMessage>();
        var userMessage = (UserMessage)message;
        userMessage.ParentToolUseId.Should().Be("toolu_01Xrwd5Y13sEHtzScxR77So8");
    }

    [Fact]
    public void Parse_ValidAssistantMessage_ReturnsAssistantMessage()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "assistant",
            "message": {
                "content": [
                    {"type": "text", "text": "Hello"},
                    {"type": "tool_use", "id": "tool_123", "name": "Read", "input": {"file_path": "/test.txt"}}
                ],
                "model": "claude-opus-4-1-20250805"
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<AssistantMessage>();
        var assistantMessage = (AssistantMessage)message;
        assistantMessage.Content.Should().HaveCount(2);
        assistantMessage.Content[0].Should().BeOfType<TextBlock>();
        assistantMessage.Content[1].Should().BeOfType<ToolUseBlock>();
    }

    [Fact]
    public void Parse_AssistantMessageWithThinking_ParsesThinkingBlock()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "assistant",
            "message": {
                "content": [
                    {
                        "type": "thinking",
                        "thinking": "I'm thinking about the answer...",
                        "signature": "sig-123"
                    },
                    {"type": "text", "text": "Here's my response"}
                ],
                "model": "claude-opus-4-1-20250805"
            }
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<AssistantMessage>();
        var assistantMessage = (AssistantMessage)message;
        assistantMessage.Content.Should().HaveCount(2);
        assistantMessage.Content[0].Should().BeOfType<ThinkingBlock>();
        var thinking = (ThinkingBlock)assistantMessage.Content[0];
        thinking.Thinking.Should().Be("I'm thinking about the answer...");
        thinking.Signature.Should().Be("sig-123");
        assistantMessage.Content[1].Should().BeOfType<TextBlock>();
        ((TextBlock)assistantMessage.Content[1]).Text.Should().Be("Here's my response");
    }

    [Fact]
    public void Parse_ValidSystemMessage_ReturnsSystemMessage()
    {
        var data = JsonDocument.Parse("""
        {"type": "system", "subtype": "start"}
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<SystemMessage>();
        var systemMessage = (SystemMessage)message;
        systemMessage.Subtype.Should().Be("start");
    }

    [Fact]
    public void Parse_AssistantMessageInsideSubagent_ParsesParentToolUseId()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "assistant",
            "message": {
                "content": [
                    {"type": "text", "text": "Hello"},
                    {"type": "tool_use", "id": "tool_123", "name": "Read", "input": {"file_path": "/test.txt"}}
                ],
                "model": "claude-opus-4-1-20250805"
            },
            "parent_tool_use_id": "toolu_01Xrwd5Y13sEHtzScxR77So8"
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<AssistantMessage>();
        var assistantMessage = (AssistantMessage)message;
        assistantMessage.ParentToolUseId.Should().Be("toolu_01Xrwd5Y13sEHtzScxR77So8");
    }

    [Fact]
    public void Parse_ValidResultMessage_ReturnsResultMessage()
    {
        var data = JsonDocument.Parse("""
        {
            "type": "result",
            "subtype": "success",
            "duration_ms": 1000,
            "duration_api_ms": 500,
            "is_error": false,
            "num_turns": 2,
            "session_id": "session_123"
        }
        """).RootElement;

        var message = MessageParser.Parse(data);

        message.Should().BeOfType<ResultMessage>();
        var resultMessage = (ResultMessage)message;
        resultMessage.Subtype.Should().Be("success");
    }

    [Fact]
    public void Parse_InvalidDataType_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("\"not a dict\"").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Invalid message data type*expected dict, got String*");
    }

    [Fact]
    public void Parse_MissingTypeField_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"message": {"content": []}}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Message missing 'type' field*");
    }

    [Fact]
    public void Parse_UnknownMessageType_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"type": "unknown_type"}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Unknown message type: unknown_type*");
    }

    [Fact]
    public void Parse_UserMessageMissingFields_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"type": "user"}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Missing required field in user message*");
    }

    [Fact]
    public void Parse_AssistantMessageMissingFields_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"type": "assistant"}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Missing required field in assistant message*");
    }

    [Fact]
    public void Parse_SystemMessageMissingFields_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"type": "system"}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Missing required field in system message*");
    }

    [Fact]
    public void Parse_ResultMessageMissingFields_ThrowsMessageParseException()
    {
        var data = JsonDocument.Parse("""{"type": "result", "subtype": "success"}""").RootElement;

        var action = () => MessageParser.Parse(data);

        action.Should().Throw<MessageParseException>()
            .WithMessage("*Missing required field in result message*");
    }

    [Fact]
    public void Parse_MessageParseError_ContainsData()
    {
        var data = JsonDocument.Parse("""{"type": "unknown", "some": "data"}""").RootElement;

        try
        {
            MessageParser.Parse(data);
            Assert.Fail("Expected MessageParseException");
        }
        catch (MessageParseException ex)
        {
            ex.MessageData.Should().NotBeNull();
            ex.MessageData!.Value.TryGetProperty("type", out var typeEl).Should().BeTrue();
            typeEl.GetString().Should().Be("unknown");
        }
    }
}
