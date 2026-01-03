using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Extended client tests matching Rust SDK test_client.rs
/// </summary>
public class ExtendedClientTests
{
    public class OptionsDefaultStateTests
    {
        [Fact]
        public void NewOptions_HasAllFieldsUnset()
        {
            var options = new ClaudeAgentOptions();

            options.Model.Should().BeNull("model should be None by default");
            options.SystemPrompt.Should().BeNull("system_prompt should be None by default");
            options.PermissionMode.Should().BeNull("permission_mode should be None by default");
            options.MaxTurns.Should().BeNull("max_turns should be None by default");
            options.MaxBudgetUsd.Should().BeNull("max_budget_usd should be None by default");
            options.AllowedTools.Should().BeNull("allowed_tools should be null by default");
            options.DisallowedTools.Should().BeNull("disallowed_tools should be null by default");
            options.ContinueConversation.Should().BeNull("continue_conversation should be null by default");
            options.Resume.Should().BeNull("resume should be None by default");
            options.ForkSession.Should().BeNull("fork_session should be null by default");
            options.CanUseTool.Should().BeNull("can_use_tool should be null by default");
            options.Hooks.Should().BeNull("hooks should be null by default");
            options.IncludePartialMessages.Should().BeNull("include_partial_messages should be null by default");
        }
    }

    public class BuilderChainTests
    {
        [Fact]
        public void BuilderChain_SetsAllFieldsCorrectly()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithModel("claude-sonnet-4-5")
                .WithMaxTurns(10)
                .WithPermissionMode(PermissionMode.AcceptEdits)
                .WithSystemPrompt("Be helpful and concise")
                .WithCwd("/test/path")
                .AddAllowedTools("Read", "Write")
                .WithIncludePartialMessages(true)
                .Build();

            options.Model.Should().Be("claude-sonnet-4-5", "model should match set value");
            options.MaxTurns.Should().Be(10, "max_turns should match set value");
            options.PermissionMode.Should().Be(PermissionMode.AcceptEdits, "permission_mode should match set value");
            options.Cwd.Should().Be("/test/path", "cwd should match set value");
            options.AllowedTools.Should().BeEquivalentTo(new[] { "Read", "Write" }, "allowed_tools should match set value");
            options.IncludePartialMessages.Should().BeTrue("include_partial_messages should be true");
            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.AsT0.Should().Be("Be helpful and concise");
        }

        [Fact]
        public void BuilderMethods_AreIdempotent()
        {
            var options1 = new ClaudeAgentOptionsBuilder()
                .WithModel("claude-3")
                .WithMaxTurns(5)
                .Build();

            var options2 = new ClaudeAgentOptionsBuilder()
                .WithModel("claude-3")
                .WithMaxTurns(5)
                .Build();

            options1.Model.Should().Be(options2.Model, "Same builder calls should produce same model");
            options1.MaxTurns.Should().Be(options2.MaxTurns, "Same builder calls should produce same max_turns");
        }
    }

    public class SystemPromptConfigurationTests
    {
        [Fact]
        public void SystemPrompt_TextConfiguration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithSystemPrompt("Custom instructions")
                .Build();

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.IsT0.Should().BeTrue();
            options.SystemPrompt!.Value.AsT0.Should().Be("Custom instructions");
        }

        [Fact]
        public void SystemPrompt_PresetWithoutAppend()
        {
            var preset = new SystemPromptPreset { Preset = "claude_code" };
            var options = new ClaudeAgentOptionsBuilder()
                .WithSystemPrompt(preset)
                .Build();

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.IsT1.Should().BeTrue();
            options.SystemPrompt!.Value.AsT1.Preset.Should().Be("claude_code");
            options.SystemPrompt!.Value.AsT1.Append.Should().BeNull("append should be null");
        }

        [Fact]
        public void SystemPrompt_PresetWithAppend()
        {
            var preset = new SystemPromptPreset
            {
                Preset = "claude_code",
                Append = "Be concise."
            };
            var options = new ClaudeAgentOptionsBuilder()
                .WithSystemPrompt(preset)
                .Build();

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.IsT1.Should().BeTrue();
            options.SystemPrompt!.Value.AsT1.Preset.Should().Be("claude_code");
            options.SystemPrompt!.Value.AsT1.Append.Should().Be("Be concise.");
        }
    }

    public class ToolsConfigurationTests
    {
        [Fact]
        public void AllowedTools_ListConfiguration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddAllowedTools("Read", "Write", "Bash")
                .Build();

            options.AllowedTools.Should().HaveCount(3);
            options.AllowedTools.Should().Contain("Read");
            options.AllowedTools.Should().Contain("Write");
            options.AllowedTools.Should().Contain("Bash");
        }

        [Fact]
        public void AllowedTools_EmptyListIsValid()
        {
            var options = new ClaudeAgentOptionsBuilder().Build();
            options.AllowedTools.Should().BeNull("Empty tools list should be null by default");
        }

        [Fact]
        public void AllowedAndDisallowedTools_BothSet()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddAllowedTools("Read", "Write")
                .AddDisallowedTools("Bash")
                .Build();

            options.AllowedTools.Should().BeEquivalentTo(new[] { "Read", "Write" });
            options.DisallowedTools.Should().BeEquivalentTo(new[] { "Bash" });
        }
    }

    public class SessionManagementTests
    {
        [Fact]
        public void SessionContinuation_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithContinueConversation(true)
                .WithResume("session-abc123")
                .Build();

            options.ContinueConversation.Should().BeTrue();
            options.Resume.Should().Be("session-abc123");
        }

        [Fact]
        public void ForkSession_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithResume("session-xyz789")
                .WithForkSession(true)
                .Build();

            options.ForkSession.Should().BeTrue();
            options.Resume.Should().Be("session-xyz789");
        }
    }

    public class ModelConfigurationTests
    {
        [Fact]
        public void Model_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithModel("claude-opus-4-5")
                .Build();
            options.Model.Should().Be("claude-opus-4-5");
        }

        [Fact]
        public void FallbackModel_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithModel("opus")
                .WithFallbackModel("sonnet")
                .Build();

            options.Model.Should().Be("opus");
            options.FallbackModel.Should().Be("sonnet");
        }

        [Fact]
        public void MaxThinkingTokens_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithMaxThinkingTokens(5000)
                .Build();

            options.MaxThinkingTokens.Should().Be(5000);
        }
    }

    public class DirectoryAndPathConfigurationTests
    {
        [Fact]
        public void AddDirs_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddDir("/path/to/dir1")
                .AddDir("/path/to/dir2")
                .Build();

            options.AddDirs.Should().HaveCount(2);
            options.AddDirs.Should().Contain("/path/to/dir1");
            options.AddDirs.Should().Contain("/path/to/dir2");
        }

        [Fact]
        public void Cwd_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithCwd("/custom/working/dir")
                .Build();
            options.Cwd.Should().Be("/custom/working/dir");
        }

        [Fact]
        public void CliPath_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithCliPath("/usr/local/bin/claude")
                .Build();

            options.CliPath.Should().Be("/usr/local/bin/claude");
        }
    }

    public class EnvironmentConfigurationTests
    {
        [Fact]
        public void EnvVars_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddEnv("MY_VAR", "my_value")
                .AddEnv("ANOTHER_VAR", "another_value")
                .Build();

            options.Env.Should().HaveCount(2);
            options.Env!["MY_VAR"].Should().Be("my_value");
            options.Env!["ANOTHER_VAR"].Should().Be("another_value");
        }
    }

    public class SettingsConfigurationTests
    {
        [Fact]
        public void Settings_StringConfiguration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithSettings("/path/to/settings.json")
                .Build();

            options.Settings.Should().Be("/path/to/settings.json");
        }
    }

    public class McpServerConfigurationTests
    {
        [Fact]
        public void McpServers_MapConfiguration()
        {
            var config = new McpStdioServerConfig
            {
                Command = "/path/to/server",
                Args = new[] { "--option", "value" }
            };

            var options = new ClaudeAgentOptionsBuilder()
                .AddMcpServer("test-server", config)
                .Build();

            options.McpServers.Should().NotBeNull();
            options.McpServers!.Value.IsT0.Should().BeTrue();
            options.McpServers!.Value.AsT0.Should().ContainKey("test-server");
        }

        [Fact]
        public void McpServers_PathConfiguration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithMcpServersConfig("/path/to/mcp-config.json")
                .Build();

            options.McpServers.Should().NotBeNull();
            options.McpServers!.Value.IsT1.Should().BeTrue();
            options.McpServers!.Value.AsT1.Should().Be("/path/to/mcp-config.json");
        }
    }

    public class AgentConfigurationTests
    {
        [Fact]
        public void Agents_Configuration()
        {
            var agent = new AgentDefinition
            {
                Description = "A test agent",
                Prompt = "You are a test agent",
                Tools = new[] { "Read" }
            };

            var options = new ClaudeAgentOptionsBuilder()
                .AddAgent("test-agent", agent)
                .Build();

            options.Agents.Should().NotBeNull();
            options.Agents.Should().ContainKey("test-agent");
            options.Agents!["test-agent"].Description.Should().Be("A test agent");
        }
    }

    public class MiscellaneousConfigurationTests
    {
        [Fact]
        public void PartialMessages_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithIncludePartialMessages(true)
                .Build();
            options.IncludePartialMessages.Should().BeTrue();
        }

        [Fact]
        public void User_Configuration()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithUser("claude-user")
                .Build();

            options.User.Should().Be("claude-user");
        }
    }

    public class EdgeCaseTests
    {
        [Fact]
        public void EmptyModelString_IsPreserved()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithModel("")
                .Build();
            options.Model.Should().Be("", "Empty model string should be preserved");
        }

        [Fact]
        public void ZeroMaxTurns_IsValid()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithMaxTurns(0)
                .Build();
            options.MaxTurns.Should().Be(0, "Zero max_turns should be allowed");
        }

        [Fact]
        public void LargeMaxTurns_Value()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithMaxTurns(int.MaxValue)
                .Build();
            options.MaxTurns.Should().Be(int.MaxValue, "Large max_turns should be allowed");
        }

        [Fact]
        public void WhitespaceOnlySystemPrompt()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithSystemPrompt("   ")
                .Build();

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.AsT0.Should().Be("   ", "Whitespace-only prompt should be preserved");
        }

        [Fact]
        public void UnicodeInSystemPrompt()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .WithSystemPrompt("你好世界 🌍 مرحبا")
                .Build();

            options.SystemPrompt.Should().NotBeNull();
            options.SystemPrompt!.Value.AsT0.Should().Be("你好世界 🌍 مرحبا", "Unicode should be preserved");
        }

        [Fact]
        public void VeryLongAllowedToolsList()
        {
            var tools = Enumerable.Range(0, 1000).Select(i => $"Tool{i}").ToArray();
            var options = new ClaudeAgentOptionsBuilder()
                .AddAllowedTools(tools)
                .Build();

            options.AllowedTools.Should().HaveCount(1000, "Large tools list should be handled");
        }

        [Fact]
        public void DuplicateToolsInAllowedTools()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddAllowedTools("Read", "Read", "Write")
                .Build();

            options.AllowedTools.Should().HaveCount(3, "Duplicate tools should be preserved (not deduplicated)");
        }

        [Fact]
        public void OverlappingAllowedAndDisallowedTools()
        {
            var options = new ClaudeAgentOptionsBuilder()
                .AddAllowedTool("Bash")
                .AddDisallowedTool("Bash")
                .Build();

            options.AllowedTools.Should().Contain("Bash");
            options.DisallowedTools.Should().Contain("Bash");
        }
    }

    public class MessageTypeDiscriminationTests
    {
        [Fact]
        public void AssistantMessage_IsAssistant()
        {
            Message msg = new AssistantMessage
            {
                Content = new ContentBlock[] { new TextBlock { Text = "Hello" } },
                Model = "claude-3"
            };

            msg.Should().BeOfType<AssistantMessage>("is_assistant() should return true for Assistant message");
        }

        [Fact]
        public void ResultMessage_IsResult()
        {
            Message msg = new ResultMessage
            {
                Subtype = "success",
                DurationMs = 100,
                DurationApiMs = 80,
                IsError = false,
                NumTurns = 1,
                SessionId = "test"
            };

            msg.Should().BeOfType<ResultMessage>("is_result() should return true for Result message");
        }

        [Fact]
        public void AssistantMessage_AsAssistant_ReturnsSome()
        {
            var msg = new AssistantMessage
            {
                Content = new ContentBlock[] { new TextBlock { Text = "Hello" } },
                Model = "claude-3"
            };

            msg.Should().NotBeNull();
            var textBlock = msg.Content[0] as TextBlock;
            textBlock!.Text.Should().Be("Hello");
        }

        [Fact]
        public void ResultMessage_AsResult_ReturnsSome()
        {
            var msg = new ResultMessage
            {
                Subtype = "success",
                DurationMs = 100,
                DurationApiMs = 80,
                IsError = false,
                NumTurns = 1,
                SessionId = "test-session",
                TotalCostUsd = 0.001
            };

            msg.Should().NotBeNull();
            msg.SessionId.Should().Be("test-session");
            msg.TotalCostUsd.Should().Be(0.001);
        }
    }
}
