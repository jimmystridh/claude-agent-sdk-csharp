using System.Reflection;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class TypesTests
{
    public class MessageTypesTests
    {
        [Fact]
        public void UserMessage_Creation_WithStringContent()
        {
            var msg = new UserMessage { Content = "Hello, Claude!" };
            
            msg.Content.IsT0.Should().BeTrue();
            msg.Content.AsT0.Should().Be("Hello, Claude!");
        }

        [Fact]
        public void AssistantMessage_WithTextContent()
        {
            var textBlock = new TextBlock { Text = "Hello, human!" };
            var msg = new AssistantMessage
            {
                Content = new[] { textBlock },
                Model = "claude-opus-4-1-20250805"
            };

            msg.Content.Should().HaveCount(1);
            msg.Content[0].Should().BeOfType<TextBlock>();
            ((TextBlock)msg.Content[0]).Text.Should().Be("Hello, human!");
        }

        [Fact]
        public void AssistantMessage_WithThinkingContent()
        {
            var thinkingBlock = new ThinkingBlock
            {
                Thinking = "I'm thinking...",
                Signature = "sig-123"
            };
            var msg = new AssistantMessage
            {
                Content = new[] { thinkingBlock },
                Model = "claude-opus-4-1-20250805"
            };

            msg.Content.Should().HaveCount(1);
            msg.Content[0].Should().BeOfType<ThinkingBlock>();
            var thinking = (ThinkingBlock)msg.Content[0];
            thinking.Thinking.Should().Be("I'm thinking...");
            thinking.Signature.Should().Be("sig-123");
        }

        [Fact]
        public void ToolUseBlock_Creation()
        {
            var block = new ToolUseBlock
            {
                Id = "tool-123",
                Name = "Read",
                Input = System.Text.Json.JsonDocument.Parse("""{"file_path": "/test.txt"}""").RootElement
            };

            block.Id.Should().Be("tool-123");
            block.Name.Should().Be("Read");
            block.Input.GetProperty("file_path").GetString().Should().Be("/test.txt");
        }

        [Fact]
        public void ToolResultBlock_Creation()
        {
            var block = new ToolResultBlock
            {
                ToolUseId = "tool-123",
                Content = System.Text.Json.JsonDocument.Parse("\"File contents here\"").RootElement,
                IsError = false
            };

            block.ToolUseId.Should().Be("tool-123");
            block.IsError.Should().BeFalse();
        }

        [Fact]
        public void ResultMessage_Creation()
        {
            var msg = new ResultMessage
            {
                Subtype = "success",
                DurationMs = 1500,
                DurationApiMs = 1200,
                IsError = false,
                NumTurns = 1,
                SessionId = "session-123",
                TotalCostUsd = 0.01
            };

            msg.Subtype.Should().Be("success");
            msg.TotalCostUsd.Should().Be(0.01);
            msg.SessionId.Should().Be("session-123");
        }
    }

    public class OptionsTests
    {
        [Fact]
        public void DefaultOptions_HasCorrectDefaults()
        {
            var options = new ClaudeAgentOptions();

            options.AllowedTools.Should().BeNull();
            options.SystemPrompt.Should().BeNull();
            options.PermissionMode.Should().BeNull();
            options.ContinueConversation.Should().BeNull();
            options.DisallowedTools.Should().BeNull();
        }

        [Fact]
        public void Options_WithTools()
        {
            var options = new ClaudeAgentOptions
            {
                AllowedTools = new[] { "Read", "Write", "Edit" },
                DisallowedTools = new[] { "Bash" }
            };

            options.AllowedTools.Should().BeEquivalentTo(new[] { "Read", "Write", "Edit" });
            options.DisallowedTools.Should().BeEquivalentTo(new[] { "Bash" });
        }

        [Fact]
        public void Options_WithPermissionMode()
        {
            var options = new ClaudeAgentOptions { PermissionMode = Types.PermissionMode.BypassPermissions };
            options.PermissionMode.Should().Be(Types.PermissionMode.BypassPermissions);

            var optionsPlan = new ClaudeAgentOptions { PermissionMode = Types.PermissionMode.Plan };
            optionsPlan.PermissionMode.Should().Be(Types.PermissionMode.Plan);

            var optionsDefault = new ClaudeAgentOptions { PermissionMode = Types.PermissionMode.Default };
            optionsDefault.PermissionMode.Should().Be(Types.PermissionMode.Default);

            var optionsAccept = new ClaudeAgentOptions { PermissionMode = Types.PermissionMode.AcceptEdits };
            optionsAccept.PermissionMode.Should().Be(Types.PermissionMode.AcceptEdits);
        }

        [Fact]
        public void Options_WithSystemPromptString()
        {
            var options = new ClaudeAgentOptions
            {
                SystemPrompt = "You are a helpful assistant."
            };

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.IsT0.Should().BeTrue();
            options.SystemPrompt!.Value.AsT0.Should().Be("You are a helpful assistant.");
        }

        [Fact]
        public void Options_WithSessionContinuation()
        {
            var options = new ClaudeAgentOptions
            {
                ContinueConversation = true,
                Resume = "session-123"
            };

            options.ContinueConversation.Should().BeTrue();
            options.Resume.Should().Be("session-123");
        }

        [Fact]
        public void Options_WithModelSpecification()
        {
            var options = new ClaudeAgentOptions
            {
                Model = "claude-sonnet-4-5",
                PermissionPromptToolName = "CustomTool"
            };

            options.Model.Should().Be("claude-sonnet-4-5");
            options.PermissionPromptToolName.Should().Be("CustomTool");
        }

        [Fact]
        public void Options_WithThinking()
        {
            var options = new ClaudeAgentOptions
            {
                Thinking = new ThinkingConfigAdaptive { BudgetTokens = 16000 }
            };

            options.Thinking.Should().BeOfType<ThinkingConfigAdaptive>();
            ((ThinkingConfigAdaptive)options.Thinking).BudgetTokens.Should().Be(16000);
        }

        [Fact]
        public void Options_WithEffort()
        {
            var options = new ClaudeAgentOptions
            {
                Effort = "high"
            };

            options.Effort.Should().Be("high");
        }

#pragma warning disable CS0618 // MaxThinkingTokens is obsolete
        [Fact]
        public void Options_MaxThinkingTokens_HasObsoleteAttribute()
        {
            var property = typeof(ClaudeAgentOptions).GetProperty(nameof(ClaudeAgentOptions.MaxThinkingTokens));
            var obsolete = property!.GetCustomAttribute<ObsoleteAttribute>();

            obsolete.Should().NotBeNull();
            obsolete!.Message.Should().Contain("Thinking");
        }
#pragma warning restore CS0618
    }

    public class ThinkingConfigTests
    {
        [Fact]
        public void ThinkingConfigAdaptive_IsThinkingConfig()
        {
            ThinkingConfig config = new ThinkingConfigAdaptive { BudgetTokens = 16000 };

            config.Should().BeOfType<ThinkingConfigAdaptive>();
        }

        [Fact]
        public void ThinkingConfigEnabled_IsThinkingConfig()
        {
            ThinkingConfig config = new ThinkingConfigEnabled { BudgetTokens = 10000 };

            config.Should().BeOfType<ThinkingConfigEnabled>();
            ((ThinkingConfigEnabled)config).BudgetTokens.Should().Be(10000);
        }

        [Fact]
        public void ThinkingConfigDisabled_IsThinkingConfig()
        {
            ThinkingConfig config = new ThinkingConfigDisabled();

            config.Should().BeOfType<ThinkingConfigDisabled>();
        }

        [Fact]
        public void ThinkingConfig_PatternMatching()
        {
            ThinkingConfig adaptive = new ThinkingConfigAdaptive { BudgetTokens = 5000 };
            ThinkingConfig enabled = new ThinkingConfigEnabled { BudgetTokens = 10000 };
            ThinkingConfig disabled = new ThinkingConfigDisabled();

            Match(adaptive).Should().Be("adaptive:5000");
            Match(enabled).Should().Be("enabled:10000");
            Match(disabled).Should().Be("disabled");

            static string Match(ThinkingConfig config) => config switch
            {
                ThinkingConfigAdaptive a => $"adaptive:{a.BudgetTokens}",
                ThinkingConfigEnabled e => $"enabled:{e.BudgetTokens}",
                ThinkingConfigDisabled => "disabled",
                _ => "unknown"
            };
        }

        [Fact]
        public void ThinkingConfigAdaptive_DefaultBudgetTokens_IsNull()
        {
            var config = new ThinkingConfigAdaptive();

            config.BudgetTokens.Should().BeNull();
        }
    }
}
