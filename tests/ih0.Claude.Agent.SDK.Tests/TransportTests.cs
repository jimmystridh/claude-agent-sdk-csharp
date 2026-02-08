using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Moq;
using OneOf;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class TransportTests
{
    private const string DefaultCliPath = "/usr/bin/claude";

    private static ClaudeAgentOptions MakeOptions(
        string? cliPath = null,
        string[]? allowedTools = null,
        string[]? disallowedTools = null,
        string? model = null,
        string? fallbackModel = null,
        PermissionMode? permissionMode = null,
        int? maxTurns = null,
        int? maxThinkingTokens = null,
        string[]? addDirs = null,
        bool continueConversation = false,
        string? resume = null,
        string? settings = null,
        Dictionary<string, string?>? extraArgs = null,
        OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>? mcpServers = null,
        SandboxSettings? sandbox = null,
        OneOf<IReadOnlyList<string>, ToolsPreset>? tools = null,
        OneOf<string, SystemPromptPreset>? systemPrompt = null)
    {
        return new ClaudeAgentOptions
        {
            CliPath = cliPath ?? DefaultCliPath,
            AllowedTools = allowedTools,
            DisallowedTools = disallowedTools,
            Model = model,
            FallbackModel = fallbackModel,
            PermissionMode = permissionMode,
            MaxTurns = maxTurns,
            MaxThinkingTokens = maxThinkingTokens,
            AddDirs = addDirs,
            ContinueConversation = continueConversation,
            Resume = resume,
            Settings = settings,
            ExtraArgs = extraArgs,
            McpServers = mcpServers,
            Sandbox = sandbox,
            Tools = tools,
            SystemPrompt = systemPrompt
        };
    }

    public class CommandBuildingTests
    {
        [Fact]
        public void BuildCommand_Basic_IncludesRequiredFlags()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("Hello");
            var transport = new SubprocessCliTransport(prompt, MakeOptions());

            var cmd = transport.BuildCommand();

            cmd[0].Should().Be(DefaultCliPath);
            cmd.Should().Contain("--output-format");
            cmd.Should().Contain("stream-json");
            cmd.Should().Contain("--input-format");
            cmd.Should().NotContain("--print");
            cmd.Should().Contain("--system-prompt");
        }

        [Fact]
        public void BuildCommand_AlwaysUsesStreamingMode()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions());

            var cmd = transport.BuildCommand();

            var inputFormatIdx = cmd.IndexOf("--input-format");
            inputFormatIdx.Should().BeGreaterThan(-1);
            cmd[inputFormatIdx + 1].Should().Be("stream-json");
        }

        [Fact]
        public void BuildCommand_DoesNotIncludeAgentsFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var options = new ClaudeAgentOptions
            {
                CliPath = DefaultCliPath,
                Agents = new Dictionary<string, AgentDefinition>
                {
                    ["test-agent"] = new AgentDefinition
                    {
                        Description = "Test agent",
                        Prompt = "You are a test agent"
                    }
                }
            };
            var transport = new SubprocessCliTransport(prompt, options);

            var cmd = transport.BuildCommand();

            cmd.Should().NotContain("--agents");
        }

        [Fact]
        public void BuildCommand_WithSystemPromptString_IncludesSystemPrompt()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                systemPrompt: OneOf<string, SystemPromptPreset>.FromT0("Be helpful")));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--system-prompt");
            cmd.Should().Contain("Be helpful");
        }

        [Fact]
        public void BuildCommand_WithOptions_IncludesAllFlags()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                allowedTools: new[] { "Read", "Write" },
                disallowedTools: new[] { "Bash" },
                model: "claude-sonnet-4-5",
                permissionMode: PermissionMode.AcceptEdits,
                maxTurns: 5
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--allowedTools");
            cmd.Should().Contain("Read,Write");
            cmd.Should().Contain("--disallowedTools");
            cmd.Should().Contain("Bash");
            cmd.Should().Contain("--model");
            cmd.Should().Contain("claude-sonnet-4-5");
            cmd.Should().Contain("--permission-mode");
            cmd.Should().Contain("acceptEdits");
            cmd.Should().Contain("--max-turns");
            cmd.Should().Contain("5");
        }

        [Fact]
        public void BuildCommand_WithFallbackModel_IncludesFallbackModelFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                model: "opus",
                fallbackModel: "sonnet"
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--model");
            cmd.Should().Contain("opus");
            cmd.Should().Contain("--fallback-model");
            cmd.Should().Contain("sonnet");
        }

        [Fact]
        public void BuildCommand_WithMaxThinkingTokens_IncludesFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                maxThinkingTokens: 5000
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--max-thinking-tokens");
            cmd.Should().Contain("5000");
        }

        [Fact]
        public void BuildCommand_WithAddDirs_IncludesMultipleFlags()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                addDirs: new[] { "/path/to/dir1", "/path/to/dir2" }
            ));

            var cmd = transport.BuildCommand();

            var addDirIndices = cmd.Select((item, index) => (item, index))
                .Where(x => x.item == "--add-dir")
                .Select(x => x.index)
                .ToList();

            addDirIndices.Should().HaveCount(2);
            cmd[addDirIndices[0] + 1].Should().Be("/path/to/dir1");
            cmd[addDirIndices[1] + 1].Should().Be("/path/to/dir2");
        }

        [Fact]
        public void BuildCommand_WithSessionContinuation_IncludesFlags()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("Continue from before");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                continueConversation: true,
                resume: "session-123"
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--continue");
            cmd.Should().Contain("--resume");
            cmd.Should().Contain("session-123");
        }

        [Fact]
        public void BuildCommand_WithSettingsFile_IncludesSettingsFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                settings: "/path/to/settings.json"
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--settings");
            cmd.Should().Contain("/path/to/settings.json");
        }

        [Fact]
        public void BuildCommand_WithSettingsJson_IncludesSettingsFlag()
        {
            var settingsJson = """{"permissions": {"allow": ["Bash(ls:*)"]}}""";
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                settings: settingsJson
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--settings");
            cmd.Should().Contain(settingsJson);
        }

        [Fact]
        public void BuildCommand_WithExtraArgs_IncludesAdditionalFlags()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                extraArgs: new Dictionary<string, string?>
                {
                    { "new-flag", "value" },
                    { "boolean-flag", null },
                    { "another-option", "test-value" }
                }
            ));

            var cmd = transport.BuildCommand();
            var cmdStr = string.Join(" ", cmd);

            cmdStr.Should().Contain("--new-flag value");
            cmdStr.Should().Contain("--another-option test-value");
            cmd.Should().Contain("--boolean-flag");

            var booleanIdx = cmd.IndexOf("--boolean-flag");
            (booleanIdx == cmd.Count - 1 || cmd[booleanIdx + 1].StartsWith("--")).Should().BeTrue();
        }

        [Fact]
        public void BuildCommand_WithMcpServers_IncludesMcpConfigFlag()
        {
            var mcpServers = new Dictionary<string, McpServerConfig>
            {
                ["test-server"] = new McpStdioServerConfig
                {
                    Command = "/path/to/server",
                    Args = new[] { "--option", "value" }
                }
            };

            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                mcpServers: OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>.FromT0(mcpServers)
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--mcp-config");
            var mcpIdx = cmd.IndexOf("--mcp-config");
            var mcpConfigValue = cmd[mcpIdx + 1];

            var config = JsonDocument.Parse(mcpConfigValue);
            config.RootElement.TryGetProperty("mcpServers", out var servers).Should().BeTrue();
        }

        [Fact]
        public void BuildCommand_WithMcpServersAsFilePath_PassesPathDirectly()
        {
            var stringPath = "/path/to/mcp-config.json";
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                mcpServers: OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>.FromT1(stringPath)
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--mcp-config");
            var mcpIdx = cmd.IndexOf("--mcp-config");
            cmd[mcpIdx + 1].Should().Be(stringPath);
        }

        [Fact]
        public void BuildCommand_WithSandboxOnly_MergesIntoSettings()
        {
            var sandbox = new SandboxSettings
            {
                Enabled = true,
                AutoAllowBashIfSandboxed = true
            };

            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(sandbox: sandbox));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--settings");
            var settingsIdx = cmd.IndexOf("--settings");
            var settingsValue = cmd[settingsIdx + 1];

            var parsed = JsonDocument.Parse(settingsValue);
            parsed.RootElement.TryGetProperty("sandbox", out var sandboxEl).Should().BeTrue();
        }

        [Fact]
        public void BuildCommand_WithToolsArray_IncludesToolsFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                tools: OneOf<IReadOnlyList<string>, ToolsPreset>.FromT0(new[] { "Read", "Edit", "Bash" })
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--tools");
            var toolsIdx = cmd.IndexOf("--tools");
            cmd[toolsIdx + 1].Should().Be("Read,Edit,Bash");
        }

        [Fact]
        public void BuildCommand_WithEmptyToolsArray_PassesEmptyString()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions(
                tools: OneOf<IReadOnlyList<string>, ToolsPreset>.FromT0(Array.Empty<string>())
            ));

            var cmd = transport.BuildCommand();

            cmd.Should().Contain("--tools");
            var toolsIdx = cmd.IndexOf("--tools");
            cmd[toolsIdx + 1].Should().Be("");
        }

        [Fact]
        public void BuildCommand_WithoutTools_DoesNotIncludeToolsFlag()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions());

            var cmd = transport.BuildCommand();

            cmd.Should().NotContain("--tools");
        }
    }

    public class TransportInterfaceTests
    {
        [Fact]
        public void Transport_ImplementsITransport()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions());

            transport.Should().BeAssignableTo<ITransport>();
        }

        [Fact]
        public void Transport_InitiallyNotReady()
        {
            var prompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0("test");
            var transport = new SubprocessCliTransport(prompt, MakeOptions());

            transport.IsReady.Should().BeFalse();
        }
    }
}
