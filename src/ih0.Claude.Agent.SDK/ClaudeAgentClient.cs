using System.Runtime.CompilerServices;
using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// A stateful client for interacting with Claude Code over multiple turns.
/// </summary>
/// <remarks>
/// Use this class when you need fine-grained control over the conversation lifecycle,
/// including sending multiple queries, interrupting responses, changing models mid-conversation,
/// or managing permission modes. For simple one-shot queries, use <see cref="ClaudeAgent"/> instead.
/// </remarks>
/// <example>
/// <code>
/// await using var client = new ClaudeAgentClient(new ClaudeAgentOptions { MaxTurns = 5 });
/// await client.ConnectAsync();
/// await client.QueryAsync("Hello, Claude!");
///
/// await foreach (var message in client.ReceiveResponseAsync())
/// {
///     if (message is AssistantMessage am)
///         Console.WriteLine(am.Content);
/// }
/// </code>
/// </example>
public sealed class ClaudeAgentClient : IAsyncDisposable
{
    private readonly ClaudeAgentOptions _options;
    private readonly ITransport? _customTransport;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger _logger;

    private ITransport? _transport;
    private QueryHandler? _queryHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentClient"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the client. If null, default options are used.</param>
    /// <param name="transport">Custom transport implementation. If null, a subprocess transport is created.</param>
    /// <param name="loggerFactory">Logger factory for diagnostic logging. If null, no logging occurs.</param>
    public ClaudeAgentClient(
        ClaudeAgentOptions? options = null,
        ITransport? transport = null,
        ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? new ClaudeAgentOptions();
        _customTransport = transport;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<ClaudeAgentClient>() ?? NullLogger<ClaudeAgentClient>.Instance;
        Environment.SetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT", "sdk-csharp-client");
    }

    /// <summary>
    /// Connects to Claude Code without sending an initial prompt.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the connection.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    /// <remarks>
    /// After connecting, use <see cref="QueryAsync(string, string, CancellationToken)"/> to send prompts.
    /// </remarks>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await ConnectInternalAsync(null, null, cancellationToken);
    }

    /// <summary>
    /// Connects to Claude Code with an initial string prompt.
    /// </summary>
    /// <param name="prompt">The initial prompt to send to Claude.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the connection.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    public async Task ConnectAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        await ConnectInternalAsync(prompt, null, cancellationToken);
    }

    /// <summary>
    /// Connects to Claude Code with a streaming prompt source.
    /// </summary>
    /// <param name="prompt">An async enumerable of JSON messages to stream to Claude.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the connection.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    /// <remarks>
    /// This overload is required when using the <c>CanUseTool</c> callback for permission control.
    /// </remarks>
    public async Task ConnectAsync(
        IAsyncEnumerable<JsonElement> prompt,
        CancellationToken cancellationToken = default)
    {
        await ConnectInternalAsync(null, prompt, cancellationToken);
    }

    private async Task ConnectInternalAsync(
        string? stringPrompt,
        IAsyncEnumerable<JsonElement>? streamPrompt,
        CancellationToken cancellationToken)
    {
        var options = _options;

        // Validate and configure permission settings
        if (options.CanUseTool != null)
        {
            if (stringPrompt != null)
            {
                throw new ArgumentException(
                    "can_use_tool callback requires streaming mode. " +
                    "Please provide prompt as an IAsyncEnumerable instead of a string.");
            }

            if (!string.IsNullOrEmpty(options.PermissionPromptToolName))
            {
                throw new ArgumentException(
                    "can_use_tool callback cannot be used with permission_prompt_tool_name. " +
                    "Please use one or the other.");
            }

            options = options with { PermissionPromptToolName = "stdio" };
        }

        // Determine actual prompt
        OneOf<string, IAsyncEnumerable<JsonElement>> actualPrompt;
        if (stringPrompt != null)
        {
            actualPrompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT0(stringPrompt);
        }
        else if (streamPrompt != null)
        {
            actualPrompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT1(streamPrompt);
        }
        else
        {
            actualPrompt = OneOf<string, IAsyncEnumerable<JsonElement>>.FromT1(EmptyStreamAsync());
        }

        if (_customTransport != null)
        {
            _transport = _customTransport;
        }
        else
        {
            _transport = new SubprocessCliTransport(
                actualPrompt,
                options,
                _loggerFactory?.CreateLogger<SubprocessCliTransport>());
        }

        await _transport.ConnectAsync(cancellationToken);

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

        // Convert hooks
        var hooks = ConvertHooks(options.Hooks);

        // Calculate timeout
        var timeoutMs = int.TryParse(
            Environment.GetEnvironmentVariable("CLAUDE_CODE_STREAM_CLOSE_TIMEOUT"),
            out var t) ? t : 60000;
        var initializeTimeout = Math.Max(timeoutMs / 1000.0, 60.0);

        _queryHandler = new QueryHandler(
            _transport,
            isStreamingMode: true,
            canUseTool: options.CanUseTool,
            hooks: hooks,
            sdkMcpServers: sdkMcpServers,
            initializeTimeout: initializeTimeout,
            logger: _loggerFactory?.CreateLogger<QueryHandler>());

        await _queryHandler.StartAsync(cancellationToken);
        await _queryHandler.InitializeAsync(cancellationToken);

        // If we have an initial prompt stream, start streaming it
        if (streamPrompt != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _queryHandler.StreamInputAsync(streamPrompt, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected during cancellation
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error streaming input to Claude");
                }
            }, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<JsonElement> EmptyStreamAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Receives all messages from the conversation stream.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of all messages from the conversation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    /// <remarks>
    /// This method returns all messages including intermediate ones. Use <see cref="ReceiveResponseAsync"/>
    /// to receive messages until a <see cref="ResultMessage"/> is received.
    /// </remarks>
    public async IAsyncEnumerable<Message> ReceiveMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        await foreach (var message in _queryHandler.ReceiveMessagesAsync(cancellationToken))
        {
            yield return MessageParser.Parse(message);
        }
    }

    /// <summary>
    /// Receives messages until a response is complete.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of messages, stopping after a <see cref="ResultMessage"/>.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    /// <remarks>
    /// This is typically used after <see cref="QueryAsync(string, string, CancellationToken)"/> to receive
    /// all messages for that query. The enumeration stops automatically when a result message is received.
    /// </remarks>
    public async IAsyncEnumerable<Message> ReceiveResponseAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in ReceiveMessagesAsync(cancellationToken))
        {
            yield return message;

            if (message is ResultMessage)
                yield break;
        }
    }

    /// <summary>
    /// Sends a user prompt to Claude.
    /// </summary>
    /// <param name="prompt">The user message to send.</param>
    /// <param name="sessionId">The session identifier for multi-session support.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    /// <remarks>
    /// After calling this method, use <see cref="ReceiveResponseAsync"/> to receive Claude's response.
    /// </remarks>
    public async Task QueryAsync(
        string prompt,
        string sessionId = "default",
        CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null || _transport == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        var message = new
        {
            type = "user",
            message = new { role = "user", content = prompt },
            parent_tool_use_id = (string?)null,
            session_id = sessionId
        };

        var json = JsonSerializer.Serialize(message);
        await _transport.WriteAsync(json + "\n", cancellationToken);
    }

    /// <summary>
    /// Sends a stream of messages to Claude.
    /// </summary>
    /// <param name="prompt">An async enumerable of JSON messages to send.</param>
    /// <param name="sessionId">The session identifier for multi-session support.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task QueryAsync(
        IAsyncEnumerable<JsonElement> prompt,
        string sessionId = "default",
        CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null || _transport == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        await foreach (var msg in prompt.WithCancellation(cancellationToken))
        {
            // Clone the message and add session_id if not present
            using var doc = JsonDocument.Parse(msg.GetRawText());
            var dict = new Dictionary<string, JsonElement>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }

            if (!dict.ContainsKey("session_id"))
            {
                var withSessionId = new Dictionary<string, object?>();
                foreach (var kvp in dict)
                {
                    withSessionId[kvp.Key] = kvp.Value;
                }
                withSessionId["session_id"] = sessionId;

                var json = JsonSerializer.Serialize(withSessionId);
                await _transport.WriteAsync(json + "\n", cancellationToken);
            }
            else
            {
                await _transport.WriteAsync(msg.GetRawText() + "\n", cancellationToken);
            }
        }
    }

    /// <summary>
    /// Interrupts the current Claude response.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous interrupt operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task InterruptAsync(CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        await _queryHandler.InterruptAsync(cancellationToken);
    }

    /// <summary>
    /// Sets the permission mode for tool usage.
    /// </summary>
    /// <param name="mode">The permission mode to set.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task SetPermissionModeAsync(PermissionMode mode, CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        var modeStr = mode switch
        {
            Types.PermissionMode.Default => "default",
            Types.PermissionMode.AcceptEdits => "acceptEdits",
            Types.PermissionMode.Plan => "plan",
            Types.PermissionMode.BypassPermissions => "bypassPermissions",
            _ => "default"
        };

        await _queryHandler.SetPermissionModeAsync(modeStr, cancellationToken);
    }

    /// <summary>
    /// Changes the model used for subsequent queries.
    /// </summary>
    /// <param name="model">The model identifier, or null to use the default.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task SetModelAsync(string? model, CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        await _queryHandler.SetModelAsync(model, cancellationToken);
    }

    /// <summary>
    /// Rewinds file changes to a specific user message.
    /// </summary>
    /// <param name="userMessageId">The ID of the user message to rewind to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task RewindFilesAsync(string userMessageId, CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        await _queryHandler.RewindFilesAsync(userMessageId, cancellationToken);
    }

    /// <summary>
    /// Get current MCP server connection status (streaming mode only).
    /// </summary>
    /// <exception cref="CliConnectionException">Thrown when not connected.</exception>
    public async Task<JsonElement> GetMcpStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_queryHandler == null)
            throw new CliConnectionException("Not connected. Call ConnectAsync() first.");

        return await _queryHandler.GetMcpStatusAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the server initialization information.
    /// </summary>
    /// <returns>The server info as a JSON element, or null if not connected or not yet initialized.</returns>
    public JsonElement? GetServerInfo()
    {
        return _queryHandler?.InitializationResult;
    }

    /// <summary>
    /// Disconnects from Claude Code and releases resources.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous disconnect operation.</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_queryHandler != null)
        {
            await _queryHandler.CloseAsync(cancellationToken);
            _queryHandler = null;
        }

        _transport = null;
    }

    /// <summary>
    /// Asynchronously disposes the client, disconnecting if connected.
    /// </summary>
    /// <returns>A value task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
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
