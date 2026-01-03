using System.Runtime.CompilerServices;
using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Logging;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Default implementation of <see cref="IClaudeAgentService"/> for dependency injection.
/// </summary>
/// <remarks>
/// This class wraps the static <see cref="ClaudeAgent"/> methods to provide a DI-friendly interface.
/// Use the <c>AddClaudeAgent</c> extension methods to register this service.
/// </remarks>
/// <example>
/// <code>
/// // Registration with builder options
/// services.AddClaudeAgent(builder => builder
///     .WithModel("claude-sonnet-4-20250514")
///     .WithMaxTurns(10));
///
/// // Or registration from configuration
/// services.AddClaudeAgent(configuration);
/// </code>
/// </example>
public sealed class ClaudeAgentService : IClaudeAgentService
{
    private readonly ClaudeAgentOptions _defaultOptions;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaudeAgentService"/> with the specified options.
    /// </summary>
    /// <param name="options">The default options to use for queries.</param>
    /// <param name="loggerFactory">Optional logger factory for logging.</param>
    public ClaudeAgentService(
        ClaudeAgentOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        _defaultOptions = options ?? new ClaudeAgentOptions();
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ClaudeAgentService"/> with default options.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for logging.</param>
    public ClaudeAgentService(ILoggerFactory? loggerFactory = null)
        : this(new ClaudeAgentOptions(), loggerFactory)
    {
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveOptions = MergeOptions(options);

        await foreach (var message in ClaudeAgent.QueryAsync(
            prompt,
            effectiveOptions,
            _loggerFactory,
            cancellationToken))
        {
            yield return message;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Message> QueryAsync(
        IAsyncEnumerable<JsonElement> prompt,
        ClaudeAgentOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveOptions = MergeOptions(options);

        await foreach (var message in ClaudeAgent.QueryAsync(
            prompt,
            effectiveOptions,
            _loggerFactory,
            cancellationToken))
        {
            yield return message;
        }
    }

    private ClaudeAgentOptions MergeOptions(ClaudeAgentOptions? overrides)
    {
        if (overrides == null)
        {
            return _defaultOptions;
        }

        // Use ToBuilder to start from defaults, then apply overrides
        var builder = _defaultOptions.ToBuilder();

        // Apply non-null overrides
        if (overrides.Model != null) builder.WithModel(overrides.Model);
        if (overrides.FallbackModel != null) builder.WithFallbackModel(overrides.FallbackModel);
        if (overrides.MaxTurns != null) builder.WithMaxTurns(overrides.MaxTurns.Value);
        if (overrides.MaxBudgetUsd != null) builder.WithMaxBudgetUsd(overrides.MaxBudgetUsd.Value);
        if (overrides.PermissionMode != null) builder.WithPermissionMode(overrides.PermissionMode.Value);
        if (overrides.AllowedTools != null)
        {
            foreach (var tool in overrides.AllowedTools)
            {
                builder.AddAllowedTool(tool);
            }
        }
        if (overrides.DisallowedTools != null)
        {
            foreach (var tool in overrides.DisallowedTools)
            {
                builder.AddDisallowedTool(tool);
            }
        }
        if (overrides.SystemPrompt.HasValue)
        {
            overrides.SystemPrompt.Value.Switch(
                str => builder.WithSystemPrompt(str),
                preset => builder.WithSystemPrompt(preset));
        }
        if (overrides.Cwd != null) builder.WithCwd(overrides.Cwd);
        if (overrides.Env != null)
        {
            foreach (var (key, value) in overrides.Env)
            {
                builder.AddEnv(key, value);
            }
        }
        if (overrides.AddDirs != null)
        {
            foreach (var dir in overrides.AddDirs)
            {
                builder.AddDir(dir);
            }
        }
        if (overrides.McpServers.HasValue)
        {
            overrides.McpServers.Value.Switch(
                dict =>
                {
                    foreach (var (name, config) in dict)
                    {
                        builder.AddMcpServer(name, config);
                    }
                },
                configPath => builder.WithMcpServersConfig(configPath));
        }
        if (overrides.Agents != null)
        {
            foreach (var (name, agent) in overrides.Agents)
            {
                builder.AddAgent(name, agent);
            }
        }
        if (overrides.Hooks != null)
        {
            foreach (var (evt, matchers) in overrides.Hooks)
            {
                foreach (var matcher in matchers)
                {
                    if (matcher.Hooks != null)
                    {
                        foreach (var hook in matcher.Hooks)
                        {
                            builder.AddHook(evt, hook, matcher.Matcher, matcher.Timeout);
                        }
                    }
                }
            }
        }
        if (overrides.MaxThinkingTokens != null) builder.WithMaxThinkingTokens(overrides.MaxThinkingTokens.Value);
        if (overrides.EnableFileCheckpointing != null) builder.WithFileCheckpointing(overrides.EnableFileCheckpointing.Value);
        if (overrides.IncludePartialMessages != null) builder.WithIncludePartialMessages(overrides.IncludePartialMessages.Value);
        if (overrides.User != null) builder.WithUser(overrides.User);
        if (overrides.Resume != null) builder.WithResume(overrides.Resume);
        if (overrides.ForkSession != null) builder.WithForkSession(overrides.ForkSession.Value);
        if (overrides.Settings != null) builder.WithSettings(overrides.Settings);

        return builder.Build();
    }
}
