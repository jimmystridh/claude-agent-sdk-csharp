using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using OneOf;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Fluent builder for creating <see cref="ClaudeAgentOptions"/> instances.
/// </summary>
/// <example>
/// <code>
/// var options = new ClaudeAgentOptionsBuilder()
///     .WithModel("claude-sonnet-4-20250514")
///     .WithMaxTurns(5)
///     .WithMaxBudgetUsd(1.0)
///     .AddAllowedTool("Read")
///     .AddAllowedTool("Bash")
///     .WithSystemPrompt("You are a helpful coding assistant.")
///     .Build();
/// </code>
/// </example>
public sealed class ClaudeAgentOptionsBuilder
{
    private OneOf<IReadOnlyList<string>, ToolsPreset>? _tools;
    private List<string>? _allowedTools;
    private List<string>? _disallowedTools;
    private PermissionMode? _permissionMode;
    private OneOf<string, SystemPromptPreset>? _systemPrompt;
    private string? _model;
    private string? _fallbackModel;
    private bool? _continueConversation;
    private string? _resume;
    private bool? _forkSession;
    private int? _maxTurns;
    private double? _maxBudgetUsd;
    private OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>? _mcpServers;
    private Dictionary<string, McpServerConfig>? _mcpServerDict;
    private CanUseToolCallback? _canUseTool;
    private Dictionary<HookEvent, List<HookMatcher>>? _hooks;
    private Action<string>? _stderr;
    private string? _cwd;
    private string? _cliPath;
    private Dictionary<string, string>? _env;
    private List<string>? _addDirs;
    private string? _settings;
    private string? _permissionPromptToolName;
    private List<string>? _betas;
    private int? _maxThinkingTokens;
    private bool? _enableFileCheckpointing;
    private SandboxSettings? _sandbox;
    private JsonElement? _outputFormat;
    private bool? _includePartialMessages;
    private int? _maxBufferSize;
    private string? _user;
    private Dictionary<string, AgentDefinition>? _agents;
    private List<SettingSource>? _settingSources;
    private List<SdkPluginConfig>? _plugins;
    private Dictionary<string, string?>? _extraArgs;

    /// <summary>
    /// Sets the tools available to Claude.
    /// </summary>
    /// <param name="tools">List of tool names.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithTools(IReadOnlyList<string> tools)
    {
        _tools = OneOf<IReadOnlyList<string>, ToolsPreset>.FromT0(tools);
        return this;
    }

    /// <summary>
    /// Sets the tools using a preset.
    /// </summary>
    /// <param name="preset">The tools preset.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithTools(ToolsPreset preset)
    {
        _tools = OneOf<IReadOnlyList<string>, ToolsPreset>.FromT1(preset);
        return this;
    }

    /// <summary>
    /// Adds a tool to the allowed tools list.
    /// </summary>
    /// <param name="tool">The tool name to allow.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddAllowedTool(string tool)
    {
        _allowedTools ??= new List<string>();
        _allowedTools.Add(tool);
        return this;
    }

    /// <summary>
    /// Adds multiple tools to the allowed tools list.
    /// </summary>
    /// <param name="tools">The tool names to allow.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddAllowedTools(params string[] tools)
    {
        _allowedTools ??= new List<string>();
        _allowedTools.AddRange(tools);
        return this;
    }

    /// <summary>
    /// Adds a tool to the disallowed tools list.
    /// </summary>
    /// <param name="tool">The tool name to disallow.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddDisallowedTool(string tool)
    {
        _disallowedTools ??= new List<string>();
        _disallowedTools.Add(tool);
        return this;
    }

    /// <summary>
    /// Adds multiple tools to the disallowed tools list.
    /// </summary>
    /// <param name="tools">The tool names to disallow.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddDisallowedTools(params string[] tools)
    {
        _disallowedTools ??= new List<string>();
        _disallowedTools.AddRange(tools);
        return this;
    }

    /// <summary>
    /// Sets the permission mode.
    /// </summary>
    /// <param name="mode">The permission mode.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithPermissionMode(PermissionMode mode)
    {
        _permissionMode = mode;
        return this;
    }

    /// <summary>
    /// Sets a custom system prompt.
    /// </summary>
    /// <param name="prompt">The system prompt text.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithSystemPrompt(string prompt)
    {
        _systemPrompt = OneOf<string, SystemPromptPreset>.FromT0(prompt);
        return this;
    }

    /// <summary>
    /// Sets the system prompt using a preset.
    /// </summary>
    /// <param name="preset">The system prompt preset.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithSystemPrompt(SystemPromptPreset preset)
    {
        _systemPrompt = OneOf<string, SystemPromptPreset>.FromT1(preset);
        return this;
    }

    /// <summary>
    /// Sets the system prompt using a preset name.
    /// </summary>
    /// <param name="presetName">The preset name (e.g., "claude_code").</param>
    /// <param name="append">Optional text to append to the preset prompt.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithSystemPromptPreset(string presetName, string? append = null)
    {
        _systemPrompt = OneOf<string, SystemPromptPreset>.FromT1(new SystemPromptPreset
        {
            Preset = presetName,
            Append = append
        });
        return this;
    }

    /// <summary>
    /// Sets the model to use.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithModel(string model)
    {
        _model = model;
        return this;
    }

    /// <summary>
    /// Sets the fallback model.
    /// </summary>
    /// <param name="model">The fallback model identifier.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithFallbackModel(string model)
    {
        _fallbackModel = model;
        return this;
    }

    /// <summary>
    /// Sets whether to continue an existing conversation.
    /// </summary>
    /// <param name="continueConversation">True to continue conversation.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithContinueConversation(bool continueConversation = true)
    {
        _continueConversation = continueConversation;
        return this;
    }

    /// <summary>
    /// Sets the session ID to resume.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithResume(string sessionId)
    {
        _resume = sessionId;
        return this;
    }

    /// <summary>
    /// Sets whether to fork the session when resuming.
    /// </summary>
    /// <param name="forkSession">True to fork session.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithForkSession(bool forkSession = true)
    {
        _forkSession = forkSession;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of turns.
    /// </summary>
    /// <param name="maxTurns">The maximum turns.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithMaxTurns(int maxTurns)
    {
        _maxTurns = maxTurns;
        return this;
    }

    /// <summary>
    /// Sets the maximum budget in USD.
    /// </summary>
    /// <param name="maxBudgetUsd">The maximum budget.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithMaxBudgetUsd(double maxBudgetUsd)
    {
        _maxBudgetUsd = maxBudgetUsd;
        return this;
    }

    /// <summary>
    /// Sets MCP servers from a configuration file path.
    /// </summary>
    /// <param name="configPath">Path to the MCP servers configuration.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithMcpServersConfig(string configPath)
    {
        _mcpServers = OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>.FromT1(configPath);
        _mcpServerDict = null;
        return this;
    }

    /// <summary>
    /// Adds an MCP server.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <param name="config">The server configuration.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddMcpServer(string name, McpServerConfig config)
    {
        _mcpServerDict ??= new Dictionary<string, McpServerConfig>();
        _mcpServerDict[name] = config;
        _mcpServers = null;
        return this;
    }

    /// <summary>
    /// Sets the tool permission callback.
    /// </summary>
    /// <param name="callback">The callback function.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithCanUseTool(CanUseToolCallback callback)
    {
        _canUseTool = callback;
        return this;
    }

    /// <summary>
    /// Adds a hook for a specific event.
    /// </summary>
    /// <param name="eventType">The hook event type.</param>
    /// <param name="callback">The hook callback.</param>
    /// <param name="matcher">Optional matcher pattern.</param>
    /// <param name="timeout">Optional timeout in seconds.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddHook(
        HookEvent eventType,
        HookCallback callback,
        string? matcher = null,
        double? timeout = null)
    {
        _hooks ??= new Dictionary<HookEvent, List<HookMatcher>>();

        if (!_hooks.TryGetValue(eventType, out var matchers))
        {
            matchers = new List<HookMatcher>();
            _hooks[eventType] = matchers;
        }

        matchers.Add(new HookMatcher
        {
            Matcher = matcher,
            Hooks = new[] { callback },
            Timeout = timeout
        });

        return this;
    }

    /// <summary>
    /// Sets the stderr callback.
    /// </summary>
    /// <param name="callback">The callback for stderr output.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithStderr(Action<string> callback)
    {
        _stderr = callback;
        return this;
    }

    /// <summary>
    /// Sets the working directory.
    /// </summary>
    /// <param name="cwd">The working directory path.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithCwd(string cwd)
    {
        _cwd = cwd;
        return this;
    }

    /// <summary>
    /// Sets the CLI path.
    /// </summary>
    /// <param name="cliPath">The path to the Claude CLI.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithCliPath(string cliPath)
    {
        _cliPath = cliPath;
        return this;
    }

    /// <summary>
    /// Adds an environment variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddEnv(string name, string value)
    {
        _env ??= new Dictionary<string, string>();
        _env[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a directory to the context.
    /// </summary>
    /// <param name="dir">The directory path.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddDir(string dir)
    {
        _addDirs ??= new List<string>();
        _addDirs.Add(dir);
        return this;
    }

    /// <summary>
    /// Sets the settings path or JSON.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithSettings(string settings)
    {
        _settings = settings;
        return this;
    }

    /// <summary>
    /// Sets the permission prompt tool name.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithPermissionPromptToolName(string toolName)
    {
        _permissionPromptToolName = toolName;
        return this;
    }

    /// <summary>
    /// Sets the output format.
    /// </summary>
    /// <param name="format">The output format as JSON.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithOutputFormat(JsonElement format)
    {
        _outputFormat = format;
        return this;
    }

    /// <summary>
    /// Adds a beta feature.
    /// </summary>
    /// <param name="beta">The beta feature name.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddBeta(string beta)
    {
        _betas ??= new List<string>();
        _betas.Add(beta);
        return this;
    }

    /// <summary>
    /// Sets the maximum thinking tokens.
    /// </summary>
    /// <param name="tokens">The maximum tokens.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithMaxThinkingTokens(int tokens)
    {
        _maxThinkingTokens = tokens;
        return this;
    }

    /// <summary>
    /// Enables or disables file checkpointing.
    /// </summary>
    /// <param name="enabled">True to enable.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithFileCheckpointing(bool enabled = true)
    {
        _enableFileCheckpointing = enabled;
        return this;
    }

    /// <summary>
    /// Sets the sandbox settings.
    /// </summary>
    /// <param name="settings">The sandbox settings.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithSandbox(SandboxSettings settings)
    {
        _sandbox = settings;
        return this;
    }

    /// <summary>
    /// Enables or disables partial messages.
    /// </summary>
    /// <param name="enabled">True to include partial messages.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithIncludePartialMessages(bool enabled = true)
    {
        _includePartialMessages = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum buffer size.
    /// </summary>
    /// <param name="size">The buffer size.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithMaxBufferSize(int size)
    {
        _maxBufferSize = size;
        return this;
    }

    /// <summary>
    /// Sets the user identifier.
    /// </summary>
    /// <param name="user">The user ID.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder WithUser(string user)
    {
        _user = user;
        return this;
    }

    /// <summary>
    /// Adds an agent definition.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="definition">The agent definition.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddAgent(string name, AgentDefinition definition)
    {
        _agents ??= new Dictionary<string, AgentDefinition>();
        _agents[name] = definition;
        return this;
    }

    /// <summary>
    /// Adds a setting source.
    /// </summary>
    /// <param name="source">The setting source.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddSettingSource(SettingSource source)
    {
        _settingSources ??= new List<SettingSource>();
        _settingSources.Add(source);
        return this;
    }

    /// <summary>
    /// Adds a plugin.
    /// </summary>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddPlugin(SdkPluginConfig plugin)
    {
        _plugins ??= new List<SdkPluginConfig>();
        _plugins.Add(plugin);
        return this;
    }

    /// <summary>
    /// Adds an extra command-line argument.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>This builder for chaining.</returns>
    public ClaudeAgentOptionsBuilder AddExtraArg(string name, string? value = null)
    {
        _extraArgs ??= new Dictionary<string, string?>();
        _extraArgs[name] = value;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ClaudeAgentOptions"/> instance.
    /// </summary>
    /// <returns>The configured options.</returns>
    public ClaudeAgentOptions Build()
    {
        var mcpServers = _mcpServers;
        if (mcpServers == null && _mcpServerDict != null)
        {
            mcpServers = OneOf<IReadOnlyDictionary<string, McpServerConfig>, string>.FromT0(_mcpServerDict);
        }

        return new ClaudeAgentOptions
        {
            Tools = _tools,
            AllowedTools = _allowedTools?.ToArray(),
            DisallowedTools = _disallowedTools?.ToArray(),
            PermissionMode = _permissionMode,
            SystemPrompt = _systemPrompt,
            Model = _model,
            FallbackModel = _fallbackModel,
            ContinueConversation = _continueConversation,
            Resume = _resume,
            ForkSession = _forkSession,
            MaxTurns = _maxTurns,
            MaxBudgetUsd = _maxBudgetUsd,
            McpServers = mcpServers,
            CanUseTool = _canUseTool,
            Hooks = _hooks?.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<HookMatcher>)kvp.Value.ToArray()),
            Stderr = _stderr,
            Cwd = _cwd,
            CliPath = _cliPath,
            Env = _env,
            AddDirs = _addDirs?.ToArray(),
            Settings = _settings,
            PermissionPromptToolName = _permissionPromptToolName,
            Betas = _betas?.ToArray(),
            MaxThinkingTokens = _maxThinkingTokens,
            EnableFileCheckpointing = _enableFileCheckpointing,
            Sandbox = _sandbox,
            OutputFormat = _outputFormat,
            IncludePartialMessages = _includePartialMessages,
            MaxBufferSize = _maxBufferSize,
            User = _user,
            Agents = _agents,
            SettingSources = _settingSources?.ToArray(),
            Plugins = _plugins?.ToArray(),
            ExtraArgs = _extraArgs
        };
    }
}

/// <summary>
/// Extension methods for <see cref="ClaudeAgentOptions"/>.
/// </summary>
public static class ClaudeAgentOptionsExtensions
{
    /// <summary>
    /// Creates a builder initialized with values from this options instance.
    /// </summary>
    /// <param name="options">The options to copy from.</param>
    /// <returns>A new builder with the copied values.</returns>
    public static ClaudeAgentOptionsBuilder ToBuilder(this ClaudeAgentOptions options)
    {
        var builder = new ClaudeAgentOptionsBuilder();

        if (options.Tools.HasValue)
        {
            options.Tools.Value.Switch(
                list => builder.WithTools(list),
                preset => builder.WithTools(preset));
        }

        if (options.AllowedTools != null)
            builder.AddAllowedTools(options.AllowedTools.ToArray());

        if (options.DisallowedTools != null)
            builder.AddDisallowedTools(options.DisallowedTools.ToArray());

        if (options.PermissionMode.HasValue)
            builder.WithPermissionMode(options.PermissionMode.Value);

        if (options.SystemPrompt.HasValue)
        {
            options.SystemPrompt.Value.Switch(
                str => builder.WithSystemPrompt(str),
                preset => builder.WithSystemPrompt(preset));
        }

        if (options.Model != null)
            builder.WithModel(options.Model);

        if (options.FallbackModel != null)
            builder.WithFallbackModel(options.FallbackModel);

        if (options.ContinueConversation.HasValue)
            builder.WithContinueConversation(options.ContinueConversation.Value);

        if (options.Resume != null)
            builder.WithResume(options.Resume);

        if (options.ForkSession.HasValue)
            builder.WithForkSession(options.ForkSession.Value);

        if (options.MaxTurns.HasValue)
            builder.WithMaxTurns(options.MaxTurns.Value);

        if (options.MaxBudgetUsd.HasValue)
            builder.WithMaxBudgetUsd(options.MaxBudgetUsd.Value);

        if (options.McpServers.HasValue)
        {
            options.McpServers.Value.Switch(
                dict =>
                {
                    foreach (var (name, config) in dict)
                        builder.AddMcpServer(name, config);
                },
                path => builder.WithMcpServersConfig(path));
        }

        if (options.CanUseTool != null)
            builder.WithCanUseTool(options.CanUseTool);

        if (options.Stderr != null)
            builder.WithStderr(options.Stderr);

        if (options.Cwd != null)
            builder.WithCwd(options.Cwd);

        if (options.CliPath != null)
            builder.WithCliPath(options.CliPath);

        if (options.Env != null)
        {
            foreach (var (name, value) in options.Env)
                builder.AddEnv(name, value);
        }

        if (options.AddDirs != null)
        {
            foreach (var dir in options.AddDirs)
                builder.AddDir(dir);
        }

        if (options.Settings != null)
            builder.WithSettings(options.Settings);

        if (options.Betas != null)
        {
            foreach (var beta in options.Betas)
                builder.AddBeta(beta);
        }

        if (options.MaxThinkingTokens.HasValue)
            builder.WithMaxThinkingTokens(options.MaxThinkingTokens.Value);

        if (options.EnableFileCheckpointing.HasValue)
            builder.WithFileCheckpointing(options.EnableFileCheckpointing.Value);

        if (options.Sandbox != null)
            builder.WithSandbox(options.Sandbox);

        if (options.IncludePartialMessages.HasValue)
            builder.WithIncludePartialMessages(options.IncludePartialMessages.Value);

        if (options.MaxBufferSize.HasValue)
            builder.WithMaxBufferSize(options.MaxBufferSize.Value);

        if (options.User != null)
            builder.WithUser(options.User);

        if (options.Agents != null)
        {
            foreach (var (name, def) in options.Agents)
                builder.AddAgent(name, def);
        }

        if (options.SettingSources != null)
        {
            foreach (var source in options.SettingSources)
                builder.AddSettingSource(source);
        }

        if (options.Plugins != null)
        {
            foreach (var plugin in options.Plugins)
                builder.AddPlugin(plugin);
        }

        if (options.ExtraArgs != null)
        {
            foreach (var (name, value) in options.ExtraArgs)
                builder.AddExtraArg(name, value);
        }

        return builder;
    }
}
