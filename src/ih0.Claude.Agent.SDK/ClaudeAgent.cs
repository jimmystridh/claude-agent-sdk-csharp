using System.Runtime.CompilerServices;
using System.Text.Json;
using ih0.Claude.Agent.SDK.Internal;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Logging;
using OneOf;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Provides static methods for executing one-shot queries against Claude Code.
/// </summary>
/// <remarks>
/// Use this class for simple, stateless interactions where you send a prompt and receive
/// a stream of messages. For more control over the conversation lifecycle, use
/// <see cref="ClaudeAgentClient"/> instead.
/// </remarks>
public static class ClaudeAgent
{
    /// <summary>
    /// Executes a query against Claude Code and streams the response messages.
    /// </summary>
    /// <param name="prompt">The user prompt to send to Claude.</param>
    /// <param name="options">Optional configuration for the query, including model, tools, and permissions.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of messages from the conversation.</returns>
    /// <example>
    /// <code>
    /// await foreach (var message in ClaudeAgent.QueryAsync("What is 2+2?"))
    /// {
    ///     if (message is AssistantMessage am)
    ///         Console.WriteLine(am.Content);
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Environment.SetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT", "sdk-csharp");

        options ??= new ClaudeAgentOptions();

        // Always use streaming mode with control protocol
        await foreach (var message in QueryWithControlProtocolAsync(prompt, options, loggerFactory, cancellationToken))
        {
            yield return message;
        }
    }

    private static async IAsyncEnumerable<Message> QueryWithControlProtocolAsync(
        string prompt,
        ClaudeAgentOptions options,
        ILoggerFactory? loggerFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Create a single-item async enumerable for the prompt
#pragma warning disable CS1998 // Async method lacks 'await' - yield return in async iterator is valid
        async IAsyncEnumerable<JsonElement> PromptStream()
        {
            var userMessage = new
            {
                type = "user",
                message = new { role = "user", content = prompt }
            };
            yield return JsonSerializer.SerializeToElement(userMessage);
        }
#pragma warning restore CS1998

        await using var transport = new SubprocessCliTransport(
            OneOf<string, IAsyncEnumerable<JsonElement>>.FromT1(PromptStream()),
            options,
            loggerFactory?.CreateLogger<SubprocessCliTransport>());

        await transport.ConnectAsync(cancellationToken);

        // Extract SDK MCP servers
        var sdkMcpServers = new Dictionary<string, ISdkMcpServer>();
        if (options.McpServers?.IsT0 == true)
        {
            foreach (var (name, config) in options.McpServers.Value.AsT0)
            {
                if (config is McpSdkServerConfig sdkConfig)
                {
                    sdkMcpServers[name] = sdkConfig.Instance;
                }
            }
        }

        // Convert hooks and agents
        var hooks = ConvertHooks(options.Hooks);
        var agents = ConvertAgents(options.Agents);

        // Calculate timeout
        var timeoutMs = int.TryParse(
            Environment.GetEnvironmentVariable("CLAUDE_CODE_STREAM_CLOSE_TIMEOUT"),
            out var t) ? t : 60000;
        var initializeTimeout = Math.Max(timeoutMs / 1000.0, 60.0);

        await using var queryHandler = new QueryHandler(
            transport,
            isStreamingMode: true,
            canUseTool: options.CanUseTool,
            hooks: hooks,
            sdkMcpServers: sdkMcpServers,
            agents: agents,
            initializeTimeout: initializeTimeout,
            logger: loggerFactory?.CreateLogger<QueryHandler>());

        await queryHandler.StartAsync(cancellationToken);
        await queryHandler.InitializeAsync(cancellationToken);

        // Start streaming input in background
        var logger = loggerFactory?.CreateLogger<QueryHandler>();
        _ = Task.Run(async () =>
        {
            try
            {
                await queryHandler.StreamInputAsync(PromptStream(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error streaming input to Claude");
            }
        }, cancellationToken);

        await foreach (var message in queryHandler.ReceiveMessagesAsync(cancellationToken))
        {
            yield return MessageParser.Parse(message);
        }
    }

    /// <summary>
    /// Executes a streaming query against Claude Code with full control over input messages.
    /// </summary>
    /// <param name="prompt">An async enumerable of JSON messages to stream to Claude.</param>
    /// <param name="options">Optional configuration for the query.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of messages from the conversation.</returns>
    /// <remarks>
    /// This overload provides full control over the input stream, allowing for advanced
    /// scenarios like multi-turn conversations or streaming user input.
    /// </remarks>
    public static async IAsyncEnumerable<Message> QueryAsync(
        IAsyncEnumerable<JsonElement> prompt,
        ClaudeAgentOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Environment.SetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT", "sdk-csharp");

        options ??= new ClaudeAgentOptions();

        // Validate callback requirements
        if (options.CanUseTool != null && options.PermissionPromptToolName != null)
        {
            throw new ArgumentException(
                "can_use_tool callback cannot be used with permission_prompt_tool_name. " +
                "Please use one or the other.");
        }

        // Set permission_prompt_tool_name for control protocol if canUseTool is provided
        if (options.CanUseTool != null)
        {
            options = options with { PermissionPromptToolName = "stdio" };
        }

        await using var transport = new SubprocessCliTransport(
            OneOf<string, IAsyncEnumerable<JsonElement>>.FromT1(prompt),
            options,
            loggerFactory?.CreateLogger<SubprocessCliTransport>());

        await transport.ConnectAsync(cancellationToken);

        // Extract SDK MCP servers
        var sdkMcpServers = new Dictionary<string, ISdkMcpServer>();
        if (options.McpServers?.IsT0 == true)
        {
            foreach (var (name, config) in options.McpServers.Value.AsT0)
            {
                if (config is McpSdkServerConfig sdkConfig)
                {
                    sdkMcpServers[name] = sdkConfig.Instance;
                }
            }
        }

        // Convert hooks and agents
        var hooks = ConvertHooks(options.Hooks);
        var agents = ConvertAgents(options.Agents);

        // Calculate timeout
        var timeoutMs = int.TryParse(
            Environment.GetEnvironmentVariable("CLAUDE_CODE_STREAM_CLOSE_TIMEOUT"),
            out var t) ? t : 60000;
        var initializeTimeout = Math.Max(timeoutMs / 1000.0, 60.0);

        await using var queryHandler = new QueryHandler(
            transport,
            isStreamingMode: true,
            canUseTool: options.CanUseTool,
            hooks: hooks,
            sdkMcpServers: sdkMcpServers,
            agents: agents,
            initializeTimeout: initializeTimeout,
            logger: loggerFactory?.CreateLogger<QueryHandler>());

        await queryHandler.StartAsync(cancellationToken);
        await queryHandler.InitializeAsync(cancellationToken);

        // Start streaming input in background
        var logger = loggerFactory?.CreateLogger<QueryHandler>();
        _ = Task.Run(async () =>
        {
            try
            {
                await queryHandler.StreamInputAsync(prompt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error streaming input to Claude");
            }
        }, cancellationToken);

        await foreach (var message in queryHandler.ReceiveMessagesAsync(cancellationToken))
        {
            yield return MessageParser.Parse(message);
        }
    }

    private static Dictionary<string, object>? ConvertAgents(
        IReadOnlyDictionary<string, AgentDefinition>? agents)
    {
        if (agents == null || agents.Count == 0)
            return null;

        return agents.ToDictionary(
            kvp => kvp.Key,
            kvp => (object)new Dictionary<string, object?>
            {
                ["description"] = kvp.Value.Description,
                ["prompt"] = kvp.Value.Prompt,
                ["tools"] = kvp.Value.Tools,
                ["model"] = kvp.Value.Model?.ToString().ToLowerInvariant()
            });
    }

    private static Dictionary<string, List<HookMatcherConfig>>? ConvertHooks(
        IReadOnlyDictionary<HookEvent, IReadOnlyList<HookMatcher>>? hooks)
    {
        if (hooks == null || hooks.Count == 0)
            return null;

        var result = new Dictionary<string, List<HookMatcherConfig>>();

        foreach (var (eventType, matchers) in hooks)
        {
            result[eventType.ToEventName()] = matchers.Select(m => new HookMatcherConfig
            {
                Matcher = m.Matcher,
                Hooks = m.Hooks,
                Timeout = m.Timeout
            }).ToList();
        }

        return result;
    }
}
