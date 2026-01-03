using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Extensions;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class ConfigurationExtensionsTests
{
    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void GetClaudeAgentOptions_ReturnsEmptyOptions_WhenSectionMissing()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>());
        var options = config.GetClaudeAgentOptions();

        Assert.Null(options.Model);
        Assert.Null(options.MaxTurns);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsModel()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Model"] = "claude-sonnet-4-20250514"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("claude-sonnet-4-20250514", options.Model);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsMaxTurns()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:MaxTurns"] = "10"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal(10, options.MaxTurns);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsMaxBudgetUsd()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:MaxBudgetUsd"] = "5.5"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal(5.5, options.MaxBudgetUsd);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsPermissionMode()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:PermissionMode"] = "AcceptEdits"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal(PermissionMode.AcceptEdits, options.PermissionMode);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsPermissionMode_CaseInsensitive()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:PermissionMode"] = "acceptedits"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal(PermissionMode.AcceptEdits, options.PermissionMode);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsAllowedTools()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:AllowedTools:0"] = "Read",
            ["Claude:AllowedTools:1"] = "Write",
            ["Claude:AllowedTools:2"] = "Bash"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.NotNull(options.AllowedTools);
        Assert.Equal(3, options.AllowedTools.Count);
        Assert.Contains("Read", options.AllowedTools);
        Assert.Contains("Write", options.AllowedTools);
        Assert.Contains("Bash", options.AllowedTools);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsDisallowedTools()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:DisallowedTools:0"] = "Bash",
            ["Claude:DisallowedTools:1"] = "Edit"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.NotNull(options.DisallowedTools);
        Assert.Equal(2, options.DisallowedTools.Count);
        Assert.Contains("Bash", options.DisallowedTools);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsSystemPrompt()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:SystemPrompt"] = "You are helpful."
        });

        var options = config.GetClaudeAgentOptions();

        Assert.True(options.SystemPrompt.HasValue);
        Assert.True(options.SystemPrompt.Value.IsT0);
        Assert.Equal("You are helpful.", options.SystemPrompt.Value.AsT0);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsCwd()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Cwd"] = "/project/path"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("/project/path", options.Cwd);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsEnv()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Env:DEBUG"] = "true",
            ["Claude:Env:LOG_LEVEL"] = "info"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.NotNull(options.Env);
        Assert.Equal(2, options.Env.Count);
        Assert.Equal("true", options.Env["DEBUG"]);
        Assert.Equal("info", options.Env["LOG_LEVEL"]);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsAddDirs()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:AddDirs:0"] = "/dir1",
            ["Claude:AddDirs:1"] = "/dir2"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.NotNull(options.AddDirs);
        Assert.Equal(2, options.AddDirs.Count);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsBetas()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Betas:0"] = "feature1",
            ["Claude:Betas:1"] = "feature2"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.NotNull(options.Betas);
        Assert.Equal(2, options.Betas.Count);
        Assert.Contains("feature1", options.Betas);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsMaxThinkingTokens()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:MaxThinkingTokens"] = "2000"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal(2000, options.MaxThinkingTokens);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsEnableFileCheckpointing()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:EnableFileCheckpointing"] = "true"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.True(options.EnableFileCheckpointing);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsUser()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:User"] = "user-123"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("user-123", options.User);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsMcpServersConfig()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:McpServersConfig"] = "/path/to/config.json"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.True(options.McpServers.HasValue);
        Assert.True(options.McpServers.Value.IsT1);
        Assert.Equal("/path/to/config.json", options.McpServers.Value.AsT1);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsResume()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Resume"] = "session-123",
            ["Claude:ForkSession"] = "true"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("session-123", options.Resume);
        Assert.True(options.ForkSession);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsSettings()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Settings"] = "/path/to/settings.json"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("/path/to/settings.json", options.Settings);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsCustomSection()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MyCustomSection:Model"] = "claude-opus-4",
            ["MyCustomSection:MaxTurns"] = "5"
        });

        var options = config.GetClaudeAgentOptions("MyCustomSection");

        Assert.Equal("claude-opus-4", options.Model);
        Assert.Equal(5, options.MaxTurns);
    }

    [Fact]
    public void GetClaudeAgentOptions_ReadsMultipleProperties()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Model"] = "claude-sonnet-4-20250514",
            ["Claude:MaxTurns"] = "10",
            ["Claude:MaxBudgetUsd"] = "5.0",
            ["Claude:PermissionMode"] = "AcceptEdits",
            ["Claude:Cwd"] = "/project"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Equal("claude-sonnet-4-20250514", options.Model);
        Assert.Equal(10, options.MaxTurns);
        Assert.Equal(5.0, options.MaxBudgetUsd);
        Assert.Equal(PermissionMode.AcceptEdits, options.PermissionMode);
        Assert.Equal("/project", options.Cwd);
    }

    [Fact]
    public void GetClaudeAgentOptionsBuilder_AllowsCustomization()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:Model"] = "claude-sonnet-4-20250514",
            ["Claude:MaxTurns"] = "5"
        });

        var options = config.GetClaudeAgentOptionsBuilder()
            .WithMaxTurns(10)
            .AddAllowedTool("Read")
            .Build();

        Assert.Equal("claude-sonnet-4-20250514", options.Model);
        Assert.Equal(10, options.MaxTurns);
        Assert.NotNull(options.AllowedTools);
        Assert.Contains("Read", options.AllowedTools);
    }

    [Fact]
    public void GetClaudeAgentOptions_IgnoresInvalidPermissionMode()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Claude:PermissionMode"] = "InvalidMode"
        });

        var options = config.GetClaudeAgentOptions();

        Assert.Null(options.PermissionMode);
    }
}
