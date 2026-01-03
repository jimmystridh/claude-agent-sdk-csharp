using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class BuilderTests
{
    [Fact]
    public void Builder_CreatesEmptyOptions()
    {
        var options = new ClaudeAgentOptionsBuilder().Build();

        Assert.Null(options.Model);
        Assert.Null(options.MaxTurns);
        Assert.Null(options.AllowedTools);
    }

    [Fact]
    public void Builder_SetsModel()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-sonnet-4-20250514")
            .Build();

        Assert.Equal("claude-sonnet-4-20250514", options.Model);
    }

    [Fact]
    public void Builder_SetsMaxTurns()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMaxTurns(10)
            .Build();

        Assert.Equal(10, options.MaxTurns);
    }

    [Fact]
    public void Builder_SetsMaxBudgetUsd()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMaxBudgetUsd(5.0)
            .Build();

        Assert.Equal(5.0, options.MaxBudgetUsd);
    }

    [Fact]
    public void Builder_AddsAllowedTools()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddAllowedTool("Read")
            .AddAllowedTool("Write")
            .Build();

        Assert.NotNull(options.AllowedTools);
        Assert.Equal(2, options.AllowedTools.Count);
        Assert.Contains("Read", options.AllowedTools);
        Assert.Contains("Write", options.AllowedTools);
    }

    [Fact]
    public void Builder_AddsAllowedToolsMultiple()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddAllowedTools("Read", "Write", "Bash")
            .Build();

        Assert.NotNull(options.AllowedTools);
        Assert.Equal(3, options.AllowedTools.Count);
    }

    [Fact]
    public void Builder_AddsDisallowedTools()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddDisallowedTool("Bash")
            .AddDisallowedTools("Write", "Edit")
            .Build();

        Assert.NotNull(options.DisallowedTools);
        Assert.Equal(3, options.DisallowedTools.Count);
        Assert.Contains("Bash", options.DisallowedTools);
    }

    [Fact]
    public void Builder_SetsPermissionMode()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.AcceptEdits)
            .Build();

        Assert.Equal(PermissionMode.AcceptEdits, options.PermissionMode);
    }

    [Fact]
    public void Builder_SetsSystemPrompt()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithSystemPrompt("You are a helpful assistant.")
            .Build();

        Assert.True(options.SystemPrompt.HasValue);
        Assert.True(options.SystemPrompt.Value.IsT0);
        Assert.Equal("You are a helpful assistant.", options.SystemPrompt.Value.AsT0);
    }

    [Fact]
    public void Builder_SetsSystemPromptPreset()
    {
        var preset = new SystemPromptPreset { Append = "Be concise." };
        var options = new ClaudeAgentOptionsBuilder()
            .WithSystemPrompt(preset)
            .Build();

        Assert.True(options.SystemPrompt.HasValue);
        Assert.True(options.SystemPrompt.Value.IsT1);
        Assert.Equal("Be concise.", options.SystemPrompt.Value.AsT1.Append);
    }

    [Fact]
    public void Builder_SetsFallbackModel()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-opus-4")
            .WithFallbackModel("claude-sonnet-4-20250514")
            .Build();

        Assert.Equal("claude-opus-4", options.Model);
        Assert.Equal("claude-sonnet-4-20250514", options.FallbackModel);
    }

    [Fact]
    public void Builder_SetsCwd()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithCwd("/home/user/project")
            .Build();

        Assert.Equal("/home/user/project", options.Cwd);
    }

    [Fact]
    public void Builder_AddsEnvVariables()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddEnv("MY_VAR", "value1")
            .AddEnv("OTHER_VAR", "value2")
            .Build();

        Assert.NotNull(options.Env);
        Assert.Equal(2, options.Env.Count);
        Assert.Equal("value1", options.Env["MY_VAR"]);
        Assert.Equal("value2", options.Env["OTHER_VAR"]);
    }

    [Fact]
    public void Builder_AddsDirs()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddDir("/home/user/dir1")
            .AddDir("/home/user/dir2")
            .Build();

        Assert.NotNull(options.AddDirs);
        Assert.Equal(2, options.AddDirs.Count);
    }

    [Fact]
    public void Builder_SetsBetas()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddBeta("feature1")
            .AddBeta("feature2")
            .Build();

        Assert.NotNull(options.Betas);
        Assert.Equal(2, options.Betas.Count);
        Assert.Contains("feature1", options.Betas);
    }

    [Fact]
    public void Builder_SetsMaxThinkingTokens()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMaxThinkingTokens(1000)
            .Build();

        Assert.Equal(1000, options.MaxThinkingTokens);
    }

    [Fact]
    public void Builder_SetsFileCheckpointing()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithFileCheckpointing(true)
            .Build();

        Assert.True(options.EnableFileCheckpointing);
    }

    [Fact]
    public void Builder_SetsIncludePartialMessages()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithIncludePartialMessages(true)
            .Build();

        Assert.True(options.IncludePartialMessages);
    }

    [Fact]
    public void Builder_SetsUser()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithUser("user-123")
            .Build();

        Assert.Equal("user-123", options.User);
    }

    [Fact]
    public void Builder_AddsMcpServer()
    {
        var config = new McpStdioServerConfig
        {
            Command = "node",
            Args = new[] { "server.js" }
        };

        var options = new ClaudeAgentOptionsBuilder()
            .AddMcpServer("my-server", config)
            .Build();

        Assert.True(options.McpServers.HasValue);
        Assert.True(options.McpServers.Value.IsT0);
        Assert.Contains("my-server", options.McpServers.Value.AsT0.Keys);
    }

    [Fact]
    public void Builder_SetsMcpServersConfig()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMcpServersConfig("/path/to/config.json")
            .Build();

        Assert.True(options.McpServers.HasValue);
        Assert.True(options.McpServers.Value.IsT1);
        Assert.Equal("/path/to/config.json", options.McpServers.Value.AsT1);
    }

    [Fact]
    public void Builder_AddsAgent()
    {
        var agent = new AgentDefinition
        {
            Description = "Test agent",
            Prompt = "You are a test agent"
        };

        var options = new ClaudeAgentOptionsBuilder()
            .AddAgent("test-agent", agent)
            .Build();

        Assert.NotNull(options.Agents);
        Assert.Contains("test-agent", options.Agents.Keys);
        Assert.Equal("Test agent", options.Agents["test-agent"].Description);
    }

    [Fact]
    public void Builder_SetsResume()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithResume("session-123")
            .WithForkSession(true)
            .Build();

        Assert.Equal("session-123", options.Resume);
        Assert.True(options.ForkSession);
    }

    [Fact]
    public void Builder_SetsSettings()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithSettings("/path/to/settings.json")
            .Build();

        Assert.Equal("/path/to/settings.json", options.Settings);
    }

    [Fact]
    public void Builder_ChainMultipleOperations()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-sonnet-4-20250514")
            .WithMaxTurns(5)
            .WithMaxBudgetUsd(2.0)
            .AddAllowedTools("Read", "Write")
            .WithPermissionMode(PermissionMode.AcceptEdits)
            .WithSystemPrompt("Be helpful")
            .WithCwd("/project")
            .AddEnv("KEY", "value")
            .Build();

        Assert.Equal("claude-sonnet-4-20250514", options.Model);
        Assert.Equal(5, options.MaxTurns);
        Assert.Equal(2.0, options.MaxBudgetUsd);
        Assert.Equal(2, options.AllowedTools!.Count);
        Assert.Equal(PermissionMode.AcceptEdits, options.PermissionMode);
        Assert.Equal("Be helpful", options.SystemPrompt!.Value.AsT0);
        Assert.Equal("/project", options.Cwd);
        Assert.Equal("value", options.Env!["KEY"]);
    }

    [Fact]
    public void ToBuilder_CopiesBasicProperties()
    {
        var original = new ClaudeAgentOptions
        {
            Model = "claude-sonnet-4-20250514",
            MaxTurns = 5,
            MaxBudgetUsd = 2.0,
            Cwd = "/project"
        };

        var copy = original.ToBuilder().Build();

        Assert.Equal(original.Model, copy.Model);
        Assert.Equal(original.MaxTurns, copy.MaxTurns);
        Assert.Equal(original.MaxBudgetUsd, copy.MaxBudgetUsd);
        Assert.Equal(original.Cwd, copy.Cwd);
    }

    [Fact]
    public void ToBuilder_AllowsModification()
    {
        var original = new ClaudeAgentOptions
        {
            Model = "claude-sonnet-4-20250514",
            MaxTurns = 5
        };

        var modified = original.ToBuilder()
            .WithMaxTurns(10)
            .WithMaxBudgetUsd(3.0)
            .Build();

        Assert.Equal("claude-sonnet-4-20250514", modified.Model);
        Assert.Equal(10, modified.MaxTurns);
        Assert.Equal(3.0, modified.MaxBudgetUsd);
    }

    [Fact]
    public void ToBuilder_CopiesAllowedTools()
    {
        var original = new ClaudeAgentOptions
        {
            AllowedTools = new[] { "Read", "Write" }
        };

        var copy = original.ToBuilder().Build();

        Assert.NotNull(copy.AllowedTools);
        Assert.Equal(2, copy.AllowedTools.Count);
        Assert.Contains("Read", copy.AllowedTools);
        Assert.Contains("Write", copy.AllowedTools);
    }

    [Fact]
    public void ToBuilder_CopiesEnv()
    {
        var original = new ClaudeAgentOptions
        {
            Env = new Dictionary<string, string> { ["KEY"] = "value" }
        };

        var copy = original.ToBuilder().Build();

        Assert.NotNull(copy.Env);
        Assert.Equal("value", copy.Env["KEY"]);
    }
}
