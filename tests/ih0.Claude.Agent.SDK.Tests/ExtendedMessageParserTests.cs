using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Extended message parsing tests matching Rust SDK test_message_parser.rs
/// </summary>
public class ExtendedMessageParserTests
{
    public class UserMessageTests
    {
        [Fact]
        public void ParseUserMessage_Text()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "user",
                "message": {
                    "content": "Hello, Claude!"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);

            msg.Should().BeOfType<UserMessage>();
            var user = (UserMessage)msg;
            user.Content.IsT0.Should().BeTrue();
            user.Content.AsT0.Should().Be("Hello, Claude!");
        }

        [Fact]
        public void ParseUserMessage_WithUuid()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "user",
                "message": {
                    "content": "Hello"
                },
                "uuid": "user_123"
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var user = (UserMessage)msg;
            user.Uuid.Should().Be("user_123");
        }

        [Fact]
        public void ParseUserMessage_WithContentBlocks()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "user",
                "message": {
                    "content": [
                        {"type": "text", "text": "Hello"},
                        {"type": "text", "text": " world"}
                    ]
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var user = (UserMessage)msg;
            user.Content.IsT1.Should().BeTrue();
            user.Content.AsT1.Should().HaveCount(2);
        }
    }

    public class AssistantMessageTests
    {
        [Fact]
        public void ParseAssistantMessage_Text()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "assistant",
                "message": {
                    "content": [
                        {"type": "text", "text": "Hello, I'm Claude!"}
                    ],
                    "model": "claude-3-sonnet-20240229"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);

            msg.Should().BeOfType<AssistantMessage>();
            var asst = (AssistantMessage)msg;
            var textBlock = asst.Content[0] as TextBlock;
            textBlock!.Text.Should().Be("Hello, I'm Claude!");
            asst.Model.Should().Be("claude-3-sonnet-20240229");
        }

        [Fact]
        public void ParseAssistantMessage_WithToolUse()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "assistant",
                "message": {
                    "content": [
                        {"type": "text", "text": "Let me check that for you."},
                        {
                            "type": "tool_use",
                            "id": "toolu_01234",
                            "name": "Bash",
                            "input": {"command": "ls -la"}
                        }
                    ],
                    "model": "claude-3"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var asst = (AssistantMessage)msg;

            asst.Content.Should().HaveCount(2);
            var toolBlock = asst.Content[1] as ToolUseBlock;
            toolBlock!.Name.Should().Be("Bash");
            toolBlock.Input.GetProperty("command").GetString().Should().Be("ls -la");
        }

        [Fact]
        public void ParseAssistantMessage_WithThinking()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "assistant",
                "message": {
                    "content": [
                        {
                            "type": "thinking",
                            "thinking": "Let me think about this...",
                            "signature": "sig123"
                        },
                        {"type": "text", "text": "Here's my answer."}
                    ],
                    "model": "claude-3-opus"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var asst = (AssistantMessage)msg;

            asst.Content.Should().HaveCount(2);
            var thinking = asst.Content[0] as ThinkingBlock;
            thinking!.Thinking.Should().Be("Let me think about this...");
            thinking.Signature.Should().Be("sig123");
        }

        [Fact]
        public void ParseAssistantMessage_WithError()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "assistant",
                "message": {
                    "content": [{"type": "text", "text": "Error occurred"}],
                    "model": "claude-3",
                    "error": "rate_limit"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var asst = (AssistantMessage)msg;
            asst.Error.Should().Be(AssistantMessageErrorType.RateLimit);
        }
    }

    public class SystemMessageTests
    {
        [Fact]
        public void ParseSystemMessage()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "system",
                "subtype": "init",
                "session_id": "sess_123"
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);

            msg.Should().BeOfType<SystemMessage>();
            var sys = (SystemMessage)msg;
            sys.Subtype.Should().Be("init");
            sys.Data.GetProperty("session_id").GetString().Should().Be("sess_123");
        }
    }

    public class ResultMessageTests
    {
        [Fact]
        public void ParseResultMessage()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "result",
                "subtype": "success",
                "duration_ms": 1500,
                "duration_api_ms": 800,
                "is_error": false,
                "num_turns": 5,
                "session_id": "sess_abc123",
                "total_cost_usd": 0.0123,
                "usage": {
                    "input_tokens": 100,
                    "output_tokens": 200
                },
                "result": "Task completed successfully"
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);

            msg.Should().BeOfType<ResultMessage>();
            var result = (ResultMessage)msg;
            result.Subtype.Should().Be("success");
            result.DurationMs.Should().Be(1500);
            result.DurationApiMs.Should().Be(800);
            result.IsError.Should().BeFalse();
            result.NumTurns.Should().Be(5);
            result.SessionId.Should().Be("sess_abc123");
            result.TotalCostUsd.Should().Be(0.0123);
            result.Usage.Should().NotBeNull();
            result.Result.Should().Be("Task completed successfully");
        }
    }

    public class StreamEventTests
    {
        [Fact]
        public void ParseStreamEvent()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "stream_event",
                "uuid": "evt_123",
                "session_id": "sess_456",
                "event": {
                    "type": "content_block_delta",
                    "delta": {"type": "text_delta", "text": "Hello"}
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);

            msg.Should().BeOfType<StreamEvent>();
            var evt = (StreamEvent)msg;
            evt.Uuid.Should().Be("evt_123");
            evt.SessionId.Should().Be("sess_456");
            evt.Event.GetProperty("type").GetString().Should().Be("content_block_delta");
        }
    }

    public class ToolResultBlockTests
    {
        [Fact]
        public void ParseToolResultBlock()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "assistant",
                "message": {
                    "content": [
                        {
                            "type": "tool_result",
                            "tool_use_id": "toolu_123",
                            "content": "Command output here",
                            "is_error": false
                        }
                    ],
                    "model": "claude-3"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var asst = (AssistantMessage)msg;
            var toolResult = asst.Content[0] as ToolResultBlock;

            toolResult!.ToolUseId.Should().Be("toolu_123");
            // is_error: false is now correctly parsed as false
            toolResult.IsError.Should().BeFalse();
        }
    }

    public class ErrorHandlingTests
    {
        [Fact]
        public void ParseUnknownMessageType_ThrowsException()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "unknown_type",
                "data": {}
            }
            """).RootElement;

            var act = () => MessageParser.Parse(raw);
            act.Should().Throw<MessageParseException>();
        }

        [Fact]
        public void ParseMissingType_ThrowsException()
        {
            var raw = JsonDocument.Parse("""
            {
                "content": "Hello"
            }
            """).RootElement;

            var act = () => MessageParser.Parse(raw);
            act.Should().Throw<MessageParseException>();
        }

        [Fact]
        public void ParseMissingRequiredField_ThrowsException()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "result",
                "subtype": "success"
            }
            """).RootElement;

            var act = () => MessageParser.Parse(raw);
            act.Should().Throw<MessageParseException>();
        }
    }

    public class EdgeCaseTests
    {
        [Fact]
        public void ParseEmptyContent()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "user",
                "message": {
                    "content": ""
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var user = (UserMessage)msg;
            user.Content.IsT0.Should().BeTrue();
            user.Content.AsT0.Should().BeEmpty();
        }

        [Fact]
        public void ParseNullOptionalFields()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "result",
                "subtype": "success",
                "duration_ms": 100,
                "duration_api_ms": 50,
                "is_error": false,
                "num_turns": 1,
                "session_id": "test",
                "total_cost_usd": null,
                "usage": null,
                "result": null
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var result = (ResultMessage)msg;
            result.TotalCostUsd.Should().BeNull();
            result.Usage.Should().BeNull();
            result.Result.Should().BeNull();
        }

        [Fact]
        public void ParseUnicodeContent()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "user",
                "message": {
                    "content": "你好世界 🌍 مرحبا"
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var user = (UserMessage)msg;
            user.Content.AsT0.Should().Be("你好世界 🌍 مرحبا");
        }

        [Fact]
        public void ParseLargeTokenCounts()
        {
            var raw = JsonDocument.Parse("""
            {
                "type": "result",
                "subtype": "success",
                "duration_ms": 100000,
                "duration_api_ms": 50000,
                "is_error": false,
                "num_turns": 100,
                "session_id": "test",
                "usage": {
                    "input_tokens": 1000000,
                    "output_tokens": 500000
                }
            }
            """).RootElement;

            var msg = MessageParser.Parse(raw);
            var result = (ResultMessage)msg;
            result.Usage!.Value.GetProperty("input_tokens").GetInt32().Should().Be(1000000);
            result.Usage!.Value.GetProperty("output_tokens").GetInt32().Should().Be(500000);
        }
    }
}
