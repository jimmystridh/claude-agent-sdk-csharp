using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ih0.Claude.Agent.SDK.Internal;

public sealed class QueryHandler : IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly bool _isStreamingMode;
    private readonly CanUseToolCallback? _canUseTool;
    private readonly Dictionary<string, List<HookMatcherConfig>> _hooks;
    private readonly Dictionary<string, ISdkMcpServer> _sdkMcpServers;
    private readonly double _initializeTimeout;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingResponses = new();
    private readonly ConcurrentDictionary<string, HookCallback> _hookCallbacks = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Channel<JsonElement> _messageChannel;
    private readonly CancellationTokenSource _cts = new();

    private int _nextCallbackId;
    private int _requestCounter;
    private Task? _readTask;
    private volatile bool _closed;
    private JsonElement? _initializationResult;
    private readonly TaskCompletionSource _firstResultEvent = new();
    private readonly double _streamCloseTimeout;

    public QueryHandler(
        ITransport transport,
        bool isStreamingMode,
        CanUseToolCallback? canUseTool = null,
        Dictionary<string, List<HookMatcherConfig>>? hooks = null,
        Dictionary<string, ISdkMcpServer>? sdkMcpServers = null,
        double initializeTimeout = 60.0,
        ILogger<QueryHandler>? logger = null)
    {
        _transport = transport;
        _isStreamingMode = isStreamingMode;
        _canUseTool = canUseTool;
        _hooks = hooks ?? new Dictionary<string, List<HookMatcherConfig>>();
        _sdkMcpServers = sdkMcpServers ?? new Dictionary<string, ISdkMcpServer>();
        _initializeTimeout = initializeTimeout;
        _logger = logger ?? NullLogger<QueryHandler>.Instance;

        _messageChannel = Channel.CreateBounded<JsonElement>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        var timeoutEnv = Environment.GetEnvironmentVariable("CLAUDE_CODE_STREAM_CLOSE_TIMEOUT");
        _streamCloseTimeout = double.TryParse(timeoutEnv, out var timeout) ? timeout / 1000.0 : 60.0;
    }

    public JsonElement? InitializationResult => _initializationResult;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_readTask != null)
            return Task.CompletedTask;

        _readTask = Task.Run(async () => await ReadMessagesLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task<JsonElement?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStreamingMode)
            return null;

        var hooksConfig = new Dictionary<string, object>();

        foreach (var (eventName, matchers) in _hooks)
        {
            var matcherList = new List<object>();
            foreach (var matcher in matchers)
            {
                var callbackIds = new List<string>();
                foreach (var callback in matcher.Hooks)
                {
                    var callbackId = $"hook_{Interlocked.Increment(ref _nextCallbackId)}";
                    _hookCallbacks[callbackId] = callback;
                    callbackIds.Add(callbackId);
                }

                var matcherConfig = new Dictionary<string, object?>
                {
                    ["matcher"] = matcher.Matcher,
                    ["hookCallbackIds"] = callbackIds
                };

                if (matcher.Timeout.HasValue)
                {
                    matcherConfig["timeout"] = matcher.Timeout.Value;
                }

                matcherList.Add(matcherConfig);
            }
            hooksConfig[eventName] = matcherList;
        }

        var request = new Dictionary<string, object?>
        {
            ["subtype"] = "initialize",
            ["hooks"] = hooksConfig.Count > 0 ? hooksConfig : null
        };

        var response = await SendControlRequestAsync(request, _initializeTimeout, cancellationToken);
        _initializationResult = response;
        return response;
    }

    private async Task ReadMessagesLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _transport.ReadMessagesAsync(cancellationToken))
            {
                if (_closed)
                    break;

                if (!message.TryGetProperty("type", out var typeEl))
                {
                    await _messageChannel.Writer.WriteAsync(message, cancellationToken);
                    continue;
                }

                var msgType = typeEl.GetString();

                if (msgType == "control_response")
                {
                    HandleControlResponse(message);
                    continue;
                }

                if (msgType == "control_request")
                {
                    _ = Task.Run(async () => await HandleControlRequestAsync(message, cancellationToken), cancellationToken);
                    continue;
                }

                if (msgType == "control_cancel_request")
                {
                    // TODO: Implement cancellation support
                    continue;
                }

                if (msgType == "result")
                {
                    _firstResultEvent.TrySetResult();
                }

                await _messageChannel.Writer.WriteAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing messages from transport");

            foreach (var pending in _pendingResponses.Values)
            {
                pending.TrySetException(ex);
            }

            var errorMessage = JsonSerializer.SerializeToElement(new { type = "error", error = ex.Message });
            await _messageChannel.Writer.WriteAsync(errorMessage, cancellationToken);
        }
        finally
        {
            var endMessage = JsonSerializer.SerializeToElement(new { type = "end" });
            await _messageChannel.Writer.WriteAsync(endMessage, CancellationToken.None);
            _messageChannel.Writer.Complete();
        }
    }

    private void HandleControlResponse(JsonElement message)
    {
        if (!message.TryGetProperty("response", out var responseEl))
            return;

        if (!responseEl.TryGetProperty("request_id", out var requestIdEl))
            return;

        var requestId = requestIdEl.GetString();
        if (string.IsNullOrEmpty(requestId))
            return;

        if (_pendingResponses.TryRemove(requestId, out var tcs))
        {
            if (responseEl.TryGetProperty("subtype", out var subtypeEl) && subtypeEl.GetString() == "error")
            {
                var error = responseEl.TryGetProperty("error", out var errorEl)
                    ? errorEl.GetString() ?? "Unknown error"
                    : "Unknown error";
                tcs.TrySetException(new ClaudeAgentException(error));
            }
            else
            {
                tcs.TrySetResult(responseEl);
            }
        }
    }

    private async Task HandleControlRequestAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var requestId = message.GetProperty("request_id").GetString() ?? "";
        var request = message.GetProperty("request");
        var subtype = request.GetProperty("subtype").GetString();

        try
        {
            JsonElement responseData;

            switch (subtype)
            {
                case "can_use_tool":
                    responseData = await HandleCanUseToolAsync(request, cancellationToken);
                    break;

                case "hook_callback":
                    responseData = await HandleHookCallbackAsync(request, cancellationToken);
                    break;

                case "mcp_message":
                    responseData = await HandleMcpMessageAsync(request, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported control request subtype: {subtype}");
            }

            var successResponse = new
            {
                type = "control_response",
                response = new
                {
                    subtype = "success",
                    request_id = requestId,
                    response = responseData
                }
            };

            var json = JsonSerializer.Serialize(successResponse);
            await WriteAsync(json + "\n", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling control request {RequestId}", requestId);

            var errorResponse = new
            {
                type = "control_response",
                response = new
                {
                    subtype = "error",
                    request_id = requestId,
                    error = ex.Message
                }
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await WriteAsync(json + "\n", cancellationToken);
        }
    }

    private async Task<JsonElement> HandleCanUseToolAsync(JsonElement request, CancellationToken cancellationToken)
    {
        if (_canUseTool == null)
            throw new InvalidOperationException("canUseTool callback is not provided");

        var toolName = request.GetProperty("tool_name").GetString() ?? "";
        var input = request.GetProperty("input");
        var originalInput = input.Clone();

        var suggestions = new List<PermissionUpdate>();
        if (request.TryGetProperty("permission_suggestions", out var suggestionsEl) &&
            suggestionsEl.ValueKind == JsonValueKind.Array)
        {
            // Parse suggestions if needed
        }

        var context = new ToolPermissionContext
        {
            Signal = null,
            Suggestions = suggestions
        };

        var result = await _canUseTool(toolName, input, context);

        if (result is PermissionAllow allow)
        {
            var responseDict = new Dictionary<string, object?>
            {
                ["behavior"] = "allow",
                ["updatedInput"] = allow.UpdatedInput?.ValueKind != JsonValueKind.Undefined
                    ? allow.UpdatedInput
                    : originalInput
            };

            if (allow.UpdatedPermissions != null)
            {
                responseDict["updatedPermissions"] = allow.UpdatedPermissions.Select(p => p.ToDictionary()).ToList();
            }

            return JsonSerializer.SerializeToElement(responseDict);
        }
        else if (result is PermissionDeny deny)
        {
            var responseDict = new Dictionary<string, object?>
            {
                ["behavior"] = "deny",
                ["message"] = deny.Message
            };

            if (deny.Interrupt)
            {
                responseDict["interrupt"] = true;
            }

            return JsonSerializer.SerializeToElement(responseDict);
        }

        throw new InvalidOperationException($"Unknown permission result type: {result.GetType()}");
    }

    private async Task<JsonElement> HandleHookCallbackAsync(JsonElement request, CancellationToken cancellationToken)
    {
        var callbackId = request.GetProperty("callback_id").GetString() ?? "";

        if (!_hookCallbacks.TryGetValue(callbackId, out var callback))
        {
            throw new InvalidOperationException($"No hook callback found for ID: {callbackId}");
        }

        var inputEl = request.GetProperty("input");
        var toolUseId = request.TryGetProperty("tool_use_id", out var tEl) && tEl.ValueKind == JsonValueKind.String
            ? tEl.GetString()
            : null;

        // Parse hook input based on hook_event_name
        HookInput hookInput;
        var eventName = inputEl.GetProperty("hook_event_name").GetString();

        switch (eventName)
        {
            case "PreToolUse":
                hookInput = new PreToolUseHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm) ? pm.GetString() : null,
                    ToolName = inputEl.GetProperty("tool_name").GetString() ?? "",
                    ToolInput = inputEl.GetProperty("tool_input")
                };
                break;

            case "PostToolUse":
                hookInput = new PostToolUseHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm2) ? pm2.GetString() : null,
                    ToolName = inputEl.GetProperty("tool_name").GetString() ?? "",
                    ToolInput = inputEl.GetProperty("tool_input"),
                    ToolResponse = inputEl.GetProperty("tool_response")
                };
                break;

            case "UserPromptSubmit":
                hookInput = new UserPromptSubmitHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm3) ? pm3.GetString() : null,
                    Prompt = inputEl.GetProperty("prompt").GetString() ?? ""
                };
                break;

            case "Stop":
                hookInput = new StopHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm4) ? pm4.GetString() : null,
                    StopHookActive = inputEl.GetProperty("stop_hook_active").GetBoolean()
                };
                break;

            case "SubagentStop":
                hookInput = new SubagentStopHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm5) ? pm5.GetString() : null,
                    StopHookActive = inputEl.GetProperty("stop_hook_active").GetBoolean()
                };
                break;

            case "PreCompact":
                hookInput = new PreCompactHookInput
                {
                    SessionId = inputEl.GetProperty("session_id").GetString() ?? "",
                    TranscriptPath = inputEl.GetProperty("transcript_path").GetString() ?? "",
                    Cwd = inputEl.GetProperty("cwd").GetString() ?? "",
                    PermissionMode = inputEl.TryGetProperty("permission_mode", out var pm6) ? pm6.GetString() : null,
                    Trigger = inputEl.GetProperty("trigger").GetString() ?? "",
                    CustomInstructions = inputEl.TryGetProperty("custom_instructions", out var ci) ? ci.GetString() : null
                };
                break;

            default:
                throw new InvalidOperationException($"Unknown hook event: {eventName}");
        }

        var context = new HookContext { Signal = null };
        var output = await callback(hookInput, toolUseId, context);

        // Serialize output (already has correct JSON property names)
        return JsonSerializer.SerializeToElement(output);
    }

    private async Task<JsonElement> HandleMcpMessageAsync(JsonElement request, CancellationToken cancellationToken)
    {
        var serverName = request.GetProperty("server_name").GetString() ?? "";
        var message = request.GetProperty("message");

        if (!_sdkMcpServers.TryGetValue(serverName, out var server))
        {
            return JsonSerializer.SerializeToElement(new
            {
                mcp_response = new
                {
                    jsonrpc = "2.0",
                    id = message.TryGetProperty("id", out var idEl) ? idEl : default,
                    error = new { code = -32601, message = $"Server '{serverName}' not found" }
                }
            });
        }

        var method = message.GetProperty("method").GetString();
        var messageId = message.TryGetProperty("id", out var msgIdEl) ? msgIdEl : default;

        try
        {
            object? result;

            switch (method)
            {
                case "initialize":
                    result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new { tools = new { } },
                        serverInfo = new { name = server.Name, version = server.Version }
                    };
                    break;

                case "tools/list":
                    var tools = await server.ListToolsAsync(cancellationToken);
                    result = new
                    {
                        tools = tools.Select(t => new
                        {
                            name = t.Name,
                            description = t.Description,
                            inputSchema = t.InputSchema
                        }).ToList()
                    };
                    break;

                case "tools/call":
                    var @params = message.GetProperty("params");
                    var toolName = @params.GetProperty("name").GetString() ?? "";
                    var arguments = @params.TryGetProperty("arguments", out var argsEl)
                        ? argsEl
                        : JsonSerializer.SerializeToElement(new { });

                    var toolResult = await server.CallToolAsync(toolName, arguments, cancellationToken);

                    var content = toolResult.Content.Select<SdkMcpContent, object>(c => c switch
                    {
                        SdkMcpTextContent text => new { type = "text", text = text.Text },
                        SdkMcpImageContent img => new { type = "image", data = img.Data, mimeType = img.MimeType },
                        _ => new { type = "text", text = "" }
                    }).ToList();

                    result = new { content, is_error = toolResult.IsError };
                    break;

                case "notifications/initialized":
                    result = new { };
                    break;

                default:
                    return JsonSerializer.SerializeToElement(new
                    {
                        mcp_response = new
                        {
                            jsonrpc = "2.0",
                            id = messageId,
                            error = new { code = -32601, message = $"Method '{method}' not found" }
                        }
                    });
            }

            return JsonSerializer.SerializeToElement(new
            {
                mcp_response = new
                {
                    jsonrpc = "2.0",
                    id = messageId,
                    result
                }
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.SerializeToElement(new
            {
                mcp_response = new
                {
                    jsonrpc = "2.0",
                    id = messageId,
                    error = new { code = -32603, message = ex.Message }
                }
            });
        }
    }

    private async Task<JsonElement> SendControlRequestAsync(
        Dictionary<string, object?> request,
        double timeout,
        CancellationToken cancellationToken)
    {
        if (!_isStreamingMode)
            throw new InvalidOperationException("Control requests require streaming mode");

        var requestId = $"req_{Interlocked.Increment(ref _requestCounter)}_{Guid.NewGuid():N}";
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pendingResponses[requestId] = tcs;

        var controlRequest = new
        {
            type = "control_request",
            request_id = requestId,
            request
        };

        var json = JsonSerializer.Serialize(controlRequest);
        await WriteAsync(json + "\n", cancellationToken);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            return await tcs.Task.WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingResponses.TryRemove(requestId, out _);
            throw new ClaudeAgentException($"Control request timeout: {request.GetValueOrDefault("subtype")}");
        }
    }

    private async Task WriteAsync(string data, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _transport.WriteAsync(data, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task InterruptAsync(CancellationToken cancellationToken = default)
    {
        await SendControlRequestAsync(new Dictionary<string, object?> { ["subtype"] = "interrupt" }, 60, cancellationToken);
    }

    public async Task SetPermissionModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        await SendControlRequestAsync(
            new Dictionary<string, object?>
            {
                ["subtype"] = "set_permission_mode",
                ["mode"] = mode
            },
            60,
            cancellationToken);
    }

    public async Task SetModelAsync(string? model, CancellationToken cancellationToken = default)
    {
        await SendControlRequestAsync(
            new Dictionary<string, object?>
            {
                ["subtype"] = "set_model",
                ["model"] = model
            },
            60,
            cancellationToken);
    }

    public async Task RewindFilesAsync(string userMessageId, CancellationToken cancellationToken = default)
    {
        await SendControlRequestAsync(
            new Dictionary<string, object?>
            {
                ["subtype"] = "rewind_files",
                ["user_message_id"] = userMessageId
            },
            60,
            cancellationToken);
    }

    public async Task StreamInputAsync(IAsyncEnumerable<JsonElement> stream, CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var message in stream.WithCancellation(cancellationToken))
            {
                if (_closed)
                    break;

                var json = message.GetRawText();
                await WriteAsync(json + "\n", cancellationToken);
            }

            // Wait for first result if we have MCP servers or hooks
            if (_sdkMcpServers.Count > 0 || _hooks.Count > 0)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_streamCloseTimeout));
                    await _firstResultEvent.Task.WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Timeout waiting for result, continue with closing
                }
            }

            await _transport.EndInputAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            DiagnosticHelper.LogIgnoredException(ex);
        }
    }

    public async IAsyncEnumerable<JsonElement> ReceiveMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if (message.TryGetProperty("type", out var typeEl))
            {
                var type = typeEl.GetString();
                if (type == "end")
                    break;
                if (type == "error")
                {
                    var error = message.TryGetProperty("error", out var errorEl)
                        ? errorEl.GetString() ?? "Unknown error"
                        : "Unknown error";
                    throw new ClaudeAgentException(error);
                }
            }

            yield return message;
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _closed = true;
        _cts.Cancel();

        if (_readTask != null)
        {
            try
            {
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (Exception ex)
            {
                DiagnosticHelper.LogIgnoredException(ex);
            }
        }

        await _transport.CloseAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _cts.Dispose();
        _writeLock.Dispose();
    }
}

public sealed record HookMatcherConfig
{
    public string? Matcher { get; init; }
    public IReadOnlyList<HookCallback> Hooks { get; init; } = [];
    public double? Timeout { get; init; }
}
