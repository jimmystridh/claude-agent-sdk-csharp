using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Tests for type serialization and deserialization matching Rust SDK test_types.rs
/// </summary>
public class TypesSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public class PermissionModeSerializationTests
    {
        [Fact]
        public void PermissionMode_HasAllExpectedValues()
        {
            // Verify all PermissionMode values exist and are distinct
            var modes = Enum.GetValues<PermissionMode>();
            modes.Should().HaveCount(4, "Should have 4 permission modes");
            modes.Should().Contain(PermissionMode.Default);
            modes.Should().Contain(PermissionMode.AcceptEdits);
            modes.Should().Contain(PermissionMode.Plan);
            modes.Should().Contain(PermissionMode.BypassPermissions);
        }

        [Fact]
        public void PermissionMode_Serialization_IsString()
        {
            // JsonStringEnumConverter serializes as the enum member name
            var json = JsonSerializer.Serialize(PermissionMode.AcceptEdits);
            json.Should().StartWith("\"").And.EndWith("\"", "Should serialize as a quoted string");
        }

        [Fact]
        public void PermissionMode_Roundtrip()
        {
            var modes = new[] { PermissionMode.Default, PermissionMode.AcceptEdits, PermissionMode.Plan, PermissionMode.BypassPermissions };

            foreach (var mode in modes)
            {
                var json = JsonSerializer.Serialize(mode);
                var deserialized = JsonSerializer.Deserialize<PermissionMode>(json);
                deserialized.Should().Be(mode, $"Round-trip failed for {mode}");
            }
        }
    }

    public class PermissionResultSerializationTests
    {
        [Fact]
        public void PermissionAllow_CreatesCorrectJson()
        {
            var result = new PermissionAllow();
            var json = JsonSerializer.SerializeToElement(result, JsonOptions);

            json.GetProperty("behavior").GetString().Should().Be("allow", "behavior should be 'allow'");
        }

        [Fact]
        public void PermissionDeny_CreatesCorrectJson()
        {
            var result = new PermissionDeny { Message = "" };
            var json = JsonSerializer.SerializeToElement(result, JsonOptions);

            json.GetProperty("behavior").GetString().Should().Be("deny", "behavior should be 'deny'");
        }

        [Fact]
        public void PermissionDeny_WithMessage_IncludesMessage()
        {
            var result = new PermissionDeny { Message = "Operation not allowed" };
            var json = JsonSerializer.SerializeToElement(result, JsonOptions);

            json.GetProperty("behavior").GetString().Should().Be("deny");
            json.GetProperty("message").GetString().Should().Be("Operation not allowed");
        }

        [Fact]
        public void PermissionAllow_WithUpdatedInput()
        {
            var updatedInput = JsonDocument.Parse("""{"modified": true, "extra_field": "added"}""").RootElement;
            var result = new PermissionAllow { UpdatedInput = updatedInput };
            var json = JsonSerializer.SerializeToElement(result, JsonOptions);

            json.GetProperty("behavior").GetString().Should().Be("allow");
            json.GetProperty("updatedInput").GetProperty("modified").GetBoolean().Should().BeTrue();
            json.GetProperty("updatedInput").GetProperty("extra_field").GetString().Should().Be("added");
        }
    }

    public class ContentBlockSerializationTests
    {
        [Fact]
        public void TextBlock_AsText_ReturnsContent()
        {
            var block = new TextBlock { Text = "Hello, world!" };

            block.Text.Should().Be("Hello, world!", "Text should return the content");
        }

        [Fact]
        public void ToolUseBlock_IsToolUse()
        {
            var block = new ToolUseBlock
            {
                Id = "tool_123",
                Name = "Bash",
                Input = JsonDocument.Parse("""{"command": "ls -la"}""").RootElement
            };

            block.Name.Should().Be("Bash");
            block.Id.Should().Be("tool_123");
        }

        [Fact]
        public void ToolResultBlock_Fields()
        {
            var block = new ToolResultBlock
            {
                ToolUseId = "tool_123",
                Content = JsonDocument.Parse("\"Command output here\"").RootElement,
                IsError = false
            };

            block.ToolUseId.Should().Be("tool_123");
            block.Content!.Value.GetString().Should().Be("Command output here");
            block.IsError.Should().BeFalse();
        }

        [Fact]
        public void ThinkingBlock_Fields()
        {
            var block = new ThinkingBlock
            {
                Thinking = "Let me analyze this...",
                Signature = "sig_abc123"
            };

            block.Thinking.Should().Be("Let me analyze this...");
            block.Signature.Should().Be("sig_abc123");
        }
    }

    public class HookEventSerializationTests
    {
        [Fact]
        public void HookEvent_Serialization()
        {
            JsonSerializer.Serialize(HookEvent.PreToolUse)
                .Should().Be("\"PreToolUse\"");
            JsonSerializer.Serialize(HookEvent.PostToolUse)
                .Should().Be("\"PostToolUse\"");
        }

        [Fact]
        public void HookEvent_Roundtrip()
        {
            var events = new[] { HookEvent.PreToolUse, HookEvent.PostToolUse };

            foreach (var evt in events)
            {
                var json = JsonSerializer.Serialize(evt);
                var deserialized = JsonSerializer.Deserialize<HookEvent>(json);
                deserialized.Should().Be(evt);
            }
        }
    }

    public class HookOutputSerializationTests
    {
        [Fact]
        public void HookOutput_UsesContinueNotContinueUnderscore()
        {
            var output = new HookOutput
            {
                Continue = true,
                SuppressOutput = false
            };

            var json = JsonSerializer.SerializeToElement(output, JsonOptions);

            json.TryGetProperty("continue", out _).Should().BeTrue("JSON should use 'continue'");
        }

        [Fact]
        public void HookOutput_Default()
        {
            var output = new HookOutput();

            output.Continue.Should().BeNull();
            output.SuppressOutput.Should().BeNull();
            output.StopReason.Should().BeNull();
            output.Decision.Should().BeNull();
            output.Reason.Should().BeNull();
        }
    }

    public class AgentDefinitionSerializationTests
    {
        [Fact]
        public void AgentDefinition_Serialization()
        {
            var agent = new AgentDefinition
            {
                Description = "A coding assistant",
                Prompt = "You are a helpful coding assistant.",
                Tools = new[] { "Bash", "Read", "Write" },
                Model = AgentModel.Sonnet
            };

            var json = JsonSerializer.SerializeToElement(agent, JsonOptions);

            json.GetProperty("description").GetString().Should().Be("A coding assistant");
            json.GetProperty("tools")[0].GetString().Should().Be("Bash");
        }

        [Fact]
        public void AgentModel_HasAllExpectedValues()
        {
            var models = Enum.GetValues<AgentModel>();
            models.Should().HaveCount(4, "Should have 4 agent models");
            models.Should().Contain(AgentModel.Sonnet);
            models.Should().Contain(AgentModel.Opus);
            models.Should().Contain(AgentModel.Haiku);
            models.Should().Contain(AgentModel.Inherit);
        }

        [Fact]
        public void AgentModel_Serialization_IsString()
        {
            var json = JsonSerializer.Serialize(AgentModel.Sonnet);
            json.Should().StartWith("\"").And.EndWith("\"", "Should serialize as a quoted string");
        }
    }

    public class SettingSourceSerializationTests
    {
        [Fact]
        public void SettingSource_HasAllExpectedValues()
        {
            var sources = Enum.GetValues<SettingSource>();
            sources.Should().HaveCount(3, "Should have 3 setting sources");
            sources.Should().Contain(SettingSource.User);
            sources.Should().Contain(SettingSource.Project);
            sources.Should().Contain(SettingSource.Local);
        }

        [Fact]
        public void SettingSource_Serialization_IsString()
        {
            var json = JsonSerializer.Serialize(SettingSource.User);
            json.Should().StartWith("\"").And.EndWith("\"", "Should serialize as a quoted string");
        }
    }

    public class EdgeCaseSerializationTests
    {
        [Fact]
        public void ResultMessage_WithZeroDuration()
        {
            var result = new ResultMessage
            {
                Subtype = "success",
                DurationMs = 0,
                DurationApiMs = 0,
                IsError = false,
                NumTurns = 0,
                SessionId = "test",
                TotalCostUsd = 0.0
            };

            result.DurationMs.Should().Be(0);
            result.NumTurns.Should().Be(0);
            result.TotalCostUsd.Should().Be(0.0);
        }

        [Fact]
        public void ToolUseBlock_WithComplexInput()
        {
            var complexInput = JsonDocument.Parse("""
            {
                "nested": {
                    "array": [1, 2, 3],
                    "object": {"key": "value"}
                },
                "unicode": "你好🌍",
                "null_field": null,
                "number": 42.5
            }
            """).RootElement;

            var block = new ToolUseBlock
            {
                Id = "tool_complex",
                Name = "ComplexTool",
                Input = complexInput
            };

            block.Input.GetProperty("nested").GetProperty("array")[1].GetInt32().Should().Be(2);
            block.Input.GetProperty("unicode").GetString().Should().Be("你好🌍");
        }
    }
}
