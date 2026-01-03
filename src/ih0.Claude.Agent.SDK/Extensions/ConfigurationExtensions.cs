using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Configuration;

namespace ih0.Claude.Agent.SDK.Extensions;

/// <summary>
/// Extension methods for loading <see cref="ClaudeAgentOptions"/> from <see cref="IConfiguration"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Creates a <see cref="ClaudeAgentOptions"/> instance from an <see cref="IConfiguration"/> section.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Claude".</param>
    /// <returns>A new <see cref="ClaudeAgentOptions"/> with values from configuration.</returns>
    /// <example>
    /// <code>
    /// // appsettings.json:
    /// // {
    /// //   "Claude": {
    /// //     "Model": "claude-sonnet-4-20250514",
    /// //     "MaxTurns": 10,
    /// //     "MaxBudgetUsd": 5.0,
    /// //     "PermissionMode": "AcceptEdits",
    /// //     "AllowedTools": ["Read", "Write", "Bash"],
    /// //     "Cwd": "/project",
    /// //     "Env": {
    /// //       "DEBUG": "true"
    /// //     }
    /// //   }
    /// // }
    ///
    /// var options = configuration.GetClaudeAgentOptions();
    /// </code>
    /// </example>
    public static ClaudeAgentOptions GetClaudeAgentOptions(
        this IConfiguration configuration,
        string sectionName = "Claude")
    {
        var section = configuration.GetSection(sectionName);
        var configOptions = new ConfigurationOptions();
        section.Bind(configOptions);

        return configOptions.ToClaudeAgentOptions();
    }

    /// <summary>
    /// Creates a <see cref="ClaudeAgentOptionsBuilder"/> populated from an <see cref="IConfiguration"/> section.
    /// Use this when you need to further customize the options after loading from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Claude".</param>
    /// <returns>A <see cref="ClaudeAgentOptionsBuilder"/> with values from configuration.</returns>
    /// <example>
    /// <code>
    /// var options = configuration
    ///     .GetClaudeAgentOptionsBuilder()
    ///     .WithMaxTurns(20)  // Override config value
    ///     .AddAllowedTool("Edit")  // Add to config value
    ///     .Build();
    /// </code>
    /// </example>
    public static ClaudeAgentOptionsBuilder GetClaudeAgentOptionsBuilder(
        this IConfiguration configuration,
        string sectionName = "Claude")
    {
        var options = configuration.GetClaudeAgentOptions(sectionName);
        return options.ToBuilder();
    }
}

/// <summary>
/// Internal class for binding configuration values before converting to ClaudeAgentOptions.
/// Supports JSON configuration format with proper type mapping.
/// </summary>
internal class ConfigurationOptions
{
    public string? Model { get; set; }
    public string? FallbackModel { get; set; }
    public int? MaxTurns { get; set; }
    public double? MaxBudgetUsd { get; set; }
    public string? PermissionMode { get; set; }
    public List<string>? AllowedTools { get; set; }
    public List<string>? DisallowedTools { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Cwd { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public List<string>? AddDirs { get; set; }
    public List<string>? Betas { get; set; }
    public int? MaxThinkingTokens { get; set; }
    public bool? EnableFileCheckpointing { get; set; }
    public bool? IncludePartialMessages { get; set; }
    public string? User { get; set; }
    public string? McpServersConfig { get; set; }
    public string? Resume { get; set; }
    public bool? ForkSession { get; set; }
    public string? Settings { get; set; }

    public ClaudeAgentOptions ToClaudeAgentOptions()
    {
        var builder = new ClaudeAgentOptionsBuilder();

        if (Model != null) builder.WithModel(Model);
        if (FallbackModel != null) builder.WithFallbackModel(FallbackModel);
        if (MaxTurns != null) builder.WithMaxTurns(MaxTurns.Value);
        if (MaxBudgetUsd != null) builder.WithMaxBudgetUsd(MaxBudgetUsd.Value);

        if (PermissionMode != null && Enum.TryParse<PermissionMode>(PermissionMode, ignoreCase: true, out var mode))
        {
            builder.WithPermissionMode(mode);
        }

        if (AllowedTools != null)
        {
            foreach (var tool in AllowedTools)
            {
                builder.AddAllowedTool(tool);
            }
        }

        if (DisallowedTools != null)
        {
            foreach (var tool in DisallowedTools)
            {
                builder.AddDisallowedTool(tool);
            }
        }

        if (SystemPrompt != null) builder.WithSystemPrompt(SystemPrompt);
        if (Cwd != null) builder.WithCwd(Cwd);

        if (Env != null)
        {
            foreach (var (key, value) in Env)
            {
                builder.AddEnv(key, value);
            }
        }

        if (AddDirs != null)
        {
            foreach (var dir in AddDirs)
            {
                builder.AddDir(dir);
            }
        }

        if (Betas != null)
        {
            foreach (var beta in Betas)
            {
                builder.AddBeta(beta);
            }
        }

        if (MaxThinkingTokens != null) builder.WithMaxThinkingTokens(MaxThinkingTokens.Value);
        if (EnableFileCheckpointing != null) builder.WithFileCheckpointing(EnableFileCheckpointing.Value);
        if (IncludePartialMessages != null) builder.WithIncludePartialMessages(IncludePartialMessages.Value);
        if (User != null) builder.WithUser(User);
        if (McpServersConfig != null) builder.WithMcpServersConfig(McpServersConfig);
        if (Resume != null) builder.WithResume(Resume);
        if (ForkSession != null) builder.WithForkSession(ForkSession.Value);
        if (Settings != null) builder.WithSettings(Settings);

        return builder.Build();
    }
}
