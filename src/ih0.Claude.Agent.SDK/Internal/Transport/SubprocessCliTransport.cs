using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf;

namespace ih0.Claude.Agent.SDK.Internal.Transport;

public sealed class SubprocessCliTransport : ITransport
{
    private const int DefaultMaxBufferSize = 1024 * 1024; // 1MB
    private const string MinimumClaudeCodeVersion = "2.0.0";

    private readonly OneOf<string, IAsyncEnumerable<JsonElement>> _prompt;
    private readonly ClaudeAgentOptions _options;
    private readonly string _cliPath;
    private readonly string? _cwd;
    private readonly int _maxBufferSize;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger _logger;

    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private Task? _stderrTask;
    private volatile bool _isReady;
    private Exception? _exitError;

    public bool IsReady => _isReady;

    public SubprocessCliTransport(
        OneOf<string, IAsyncEnumerable<JsonElement>> prompt,
        ClaudeAgentOptions options,
        ILogger<SubprocessCliTransport>? logger = null)
    {
        _prompt = prompt;
        _options = options;
        _cliPath = options.CliPath ?? FindCli();
        _cwd = options.Cwd;
        _maxBufferSize = options.MaxBufferSize ?? DefaultMaxBufferSize;
        _logger = logger ?? NullLogger<SubprocessCliTransport>.Instance;
    }

    private static string GetSdkVersion()
    {
        var assembly = typeof(SubprocessCliTransport).Assembly;
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    private static string FindCli()
    {
        var bundledCli = FindBundledCli();
        if (bundledCli != null)
            return bundledCli;

        if (TryFindInPath("claude", out var pathCli))
            return pathCli!;

        var locations = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".npm-global", "bin", "claude"),
            "/usr/local/bin/claude",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "node_modules", ".bin", "claude"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".yarn", "bin", "claude"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "local", "claude")
        };

        foreach (var location in locations)
        {
            if (File.Exists(location))
                return location;
        }

        throw new CliNotFoundException(
            "Claude Code not found. Install with:\n" +
            "  npm install -g @anthropic-ai/claude-code\n" +
            "\nIf already installed locally, try:\n" +
            "  export PATH=\"$HOME/node_modules/.bin:$PATH\"\n" +
            "\nOr provide the path via ClaudeAgentOptions:\n" +
            "  new ClaudeAgentOptions { CliPath = \"/path/to/claude\" }");
    }

    private static string? FindBundledCli()
    {
        var cliName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "claude.exe" : "claude";
        var assemblyDir = Path.GetDirectoryName(typeof(SubprocessCliTransport).Assembly.Location);
        if (assemblyDir == null) return null;

        var bundledPath = Path.Combine(assemblyDir, "_bundled", cliName);
        return File.Exists(bundledPath) ? bundledPath : null;
    }

    private static bool TryFindInPath(string executable, out string? path)
    {
        path = null;
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return false;

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { ".exe", ".cmd", ".bat", "" }
            : new[] { "" };

        foreach (var dir in pathEnv.Split(separator))
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(dir, executable + ext);
                if (File.Exists(fullPath))
                {
                    path = fullPath;
                    return true;
                }
            }
        }

        return false;
    }

    internal List<string> BuildCommand()
    {
        var cmd = new List<string> { _cliPath, "--output-format", "stream-json", "--verbose" };

        if (_options.SystemPrompt == null)
        {
            cmd.AddRange(new[] { "--system-prompt", "" });
        }
        else
        {
            _options.SystemPrompt.Value.Switch(
                str => cmd.AddRange(new[] { "--system-prompt", str }),
                preset =>
                {
                    if (preset.Append != null)
                    {
                        cmd.AddRange(new[] { "--append-system-prompt", preset.Append });
                    }
                });
        }

        if (_options.Tools != null)
        {
            _options.Tools.Value.Switch(
                tools =>
                {
                    if (tools.Count == 0)
                        cmd.AddRange(new[] { "--tools", "" });
                    else
                        cmd.AddRange(new[] { "--tools", string.Join(",", tools) });
                },
                _ => cmd.AddRange(new[] { "--tools", "default" }));
        }

        if (_options.AllowedTools?.Count > 0)
            cmd.AddRange(new[] { "--allowedTools", string.Join(",", _options.AllowedTools) });

        if (_options.MaxTurns.HasValue)
            cmd.AddRange(new[] { "--max-turns", _options.MaxTurns.Value.ToString() });

        if (_options.MaxBudgetUsd.HasValue)
            cmd.AddRange(new[] { "--max-budget-usd", _options.MaxBudgetUsd.Value.ToString() });

        if (_options.DisallowedTools?.Count > 0)
            cmd.AddRange(new[] { "--disallowedTools", string.Join(",", _options.DisallowedTools) });

        if (!string.IsNullOrEmpty(_options.Model))
            cmd.AddRange(new[] { "--model", _options.Model });

        if (!string.IsNullOrEmpty(_options.FallbackModel))
            cmd.AddRange(new[] { "--fallback-model", _options.FallbackModel });

        if (_options.Betas?.Count > 0)
            cmd.AddRange(new[] { "--betas", string.Join(",", _options.Betas) });

        if (!string.IsNullOrEmpty(_options.PermissionPromptToolName))
            cmd.AddRange(new[] { "--permission-prompt-tool", _options.PermissionPromptToolName });

        if (_options.PermissionMode.HasValue)
        {
            var mode = _options.PermissionMode.Value switch
            {
                Types.PermissionMode.Default => "default",
                Types.PermissionMode.AcceptEdits => "acceptEdits",
                Types.PermissionMode.Plan => "plan",
                Types.PermissionMode.BypassPermissions => "bypassPermissions",
                _ => "default"
            };
            cmd.AddRange(new[] { "--permission-mode", mode });
        }

        if (_options.ContinueConversation == true)
            cmd.Add("--continue");

        if (!string.IsNullOrEmpty(_options.Resume))
            cmd.AddRange(new[] { "--resume", _options.Resume });

        var settingsValue = BuildSettingsValue();
        if (settingsValue != null)
            cmd.AddRange(new[] { "--settings", settingsValue });

        if (_options.AddDirs?.Count > 0)
        {
            foreach (var dir in _options.AddDirs)
                cmd.AddRange(new[] { "--add-dir", dir });
        }

        if (_options.McpServers != null)
        {
            _options.McpServers.Value.Switch(
                servers =>
                {
                    var serversForCli = new Dictionary<string, object>();
                    foreach (var (name, config) in servers)
                    {
                        if (config is McpSdkServerConfig sdkConfig)
                        {
                            serversForCli[name] = new { type = "sdk", name = sdkConfig.Name };
                        }
                        else
                        {
                            serversForCli[name] = config;
                        }
                    }
                    if (serversForCli.Count > 0)
                    {
                        var mcpConfig = new { mcpServers = serversForCli };
                        cmd.AddRange(new[] { "--mcp-config", JsonSerializer.Serialize(mcpConfig) });
                    }
                },
                path => cmd.AddRange(new[] { "--mcp-config", path }));
        }

        if (_options.IncludePartialMessages == true)
            cmd.Add("--include-partial-messages");

        if (_options.ForkSession == true)
            cmd.Add("--fork-session");

        if (_options.SettingSources != null && _options.SettingSources.Count > 0)
        {
            var sourcesValue = string.Join(",", _options.SettingSources.Select(s => s switch
            {
                SettingSource.User => "user",
                SettingSource.Project => "project",
                SettingSource.Local => "local",
                _ => "user"
            }));
            cmd.AddRange(new[] { "--setting-sources", sourcesValue });
        }

        if (_options.Plugins?.Count > 0)
        {
            foreach (var plugin in _options.Plugins)
            {
                if (plugin.PluginType == "local")
                    cmd.AddRange(new[] { "--plugin-dir", plugin.Path });
            }
        }

        if (_options.ExtraArgs != null)
        {
            foreach (var (flag, value) in _options.ExtraArgs)
            {
                if (value == null)
                    cmd.Add($"--{flag}");
                else
                    cmd.AddRange(new[] { $"--{flag}", value });
            }
        }

        if (_options.MaxThinkingTokens.HasValue)
            cmd.AddRange(new[] { "--max-thinking-tokens", _options.MaxThinkingTokens.Value.ToString() });

        if (_options.OutputFormat.HasValue)
        {
            var format = _options.OutputFormat.Value;
            if (format.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == "json_schema" &&
                format.TryGetProperty("schema", out var schema))
            {
                cmd.AddRange(new[] { "--json-schema", schema.GetRawText() });
            }
        }

        cmd.AddRange(new[] { "--input-format", "stream-json" });

        return cmd;
    }

    private string? BuildSettingsValue()
    {
        var hasSettings = !string.IsNullOrEmpty(_options.Settings);
        var hasSandbox = _options.Sandbox != null;

        if (!hasSettings && !hasSandbox)
            return null;

        if (hasSettings && !hasSandbox)
            return _options.Settings;

        var settingsObj = new Dictionary<string, object>();

        if (hasSettings)
        {
            var settingsStr = _options.Settings!.Trim();
            if (settingsStr.StartsWith("{") && settingsStr.EndsWith("}"))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(settingsStr);
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                            settingsObj[kvp.Key] = kvp.Value;
                    }
                }
                catch
                {
                    // Treat as file path
                    if (File.Exists(settingsStr))
                    {
                        var content = File.ReadAllText(settingsStr);
                        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                        if (parsed != null)
                        {
                            foreach (var kvp in parsed)
                                settingsObj[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            else if (File.Exists(settingsStr))
            {
                var content = File.ReadAllText(settingsStr);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                if (parsed != null)
                {
                    foreach (var kvp in parsed)
                        settingsObj[kvp.Key] = kvp.Value;
                }
            }
        }

        if (hasSandbox)
        {
            settingsObj["sandbox"] = _options.Sandbox!;
        }

        return JsonSerializer.Serialize(settingsObj);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_process != null)
            return;

        if (Environment.GetEnvironmentVariable("CLAUDE_AGENT_SDK_SKIP_VERSION_CHECK") == null)
        {
            await CheckClaudeVersionAsync(cancellationToken);
        }

        var cmd = BuildCommand();
        var args = string.Join(" ", cmd.Skip(1).Select(arg =>
            string.IsNullOrEmpty(arg) || arg.Contains(' ') || arg.Contains('"')
                ? $"\"{arg.Replace("\"", "\\\"")}\""
                : arg));

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            env[entry.Key?.ToString() ?? ""] = entry.Value?.ToString() ?? "";
        }

        if (_options.Env != null)
        {
            foreach (var kvp in _options.Env)
                env[kvp.Key] = kvp.Value;
        }

        env["CLAUDE_CODE_ENTRYPOINT"] = "sdk-csharp";
        env["CLAUDE_AGENT_SDK_VERSION"] = GetSdkVersion();

        if (_options.EnableFileCheckpointing == true)
            env["CLAUDE_CODE_ENABLE_SDK_FILE_CHECKPOINTING"] = "true";

        if (_cwd != null)
            env["PWD"] = _cwd;

        var redirectStderr = _options.Stderr != null || _options.ExtraArgs?.ContainsKey("debug-to-stderr") == true;

        var startInfo = new ProcessStartInfo
        {
            FileName = cmd[0],
            Arguments = args,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = redirectStderr,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _cwd ?? Environment.CurrentDirectory,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        if (redirectStderr)
            startInfo.StandardErrorEncoding = Encoding.UTF8;

        foreach (var kvp in env)
            startInfo.Environment[kvp.Key] = kvp.Value;

        try
        {
            _process = Process.Start(startInfo);
            if (_process == null)
                throw new CliConnectionException("Failed to start Claude Code process");

            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;

            if (startInfo.RedirectStandardError)
            {
                _stderrTask = Task.Run(async () =>
                {
                    var stderr = _process.StandardError;
                    string? line;
                    while ((line = await stderr.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                            _options.Stderr?.Invoke(line);
                    }
                }, CancellationToken.None);
            }

            _isReady = true;
        }
        catch (Exception ex) when (ex is not ClaudeAgentException)
        {
            _exitError = new CliConnectionException($"Failed to start Claude Code: {ex.Message}", ex);
            throw _exitError;
        }
    }

    private async Task CheckClaudeVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var startInfo = new ProcessStartInfo
            {
                FileName = _cliPath,
                Arguments = "-v",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return;

            var output = await process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            await process.WaitForExitAsync(linkedCts.Token);

            var match = System.Text.RegularExpressions.Regex.Match(output.Trim(), @"^(\d+\.\d+\.\d+)");
            if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
            {
                var minVersion = Version.Parse(MinimumClaudeCodeVersion);

                if (version < minVersion)
                {
                    _logger.LogWarning(
                        "Claude Code version {Version} is unsupported in the Agent SDK. " +
                        "Minimum required version is {MinimumVersion}. Some features may not work correctly.",
                        version, MinimumClaudeCodeVersion);
                }
            }
        }
        catch (Exception ex)
        {
            DiagnosticHelper.LogIgnoredException(ex);
        }
    }

    public async IAsyncEnumerable<JsonElement> ReadMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_stdout == null)
            throw new CliConnectionException("Not connected");

        var jsonBuffer = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await _stdout.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // End of stream
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            jsonBuffer.Append(line.Trim());

            if (jsonBuffer.Length > _maxBufferSize)
            {
                var length = jsonBuffer.Length;
                jsonBuffer.Clear();
                throw new JsonDecodeException(
                    $"JSON message exceeded maximum buffer size of {_maxBufferSize} bytes",
                    new InvalidOperationException($"Buffer size {length} exceeds limit {_maxBufferSize}"));
            }

            JsonElement element;
            try
            {
                using var doc = JsonDocument.Parse(jsonBuffer.ToString());
                element = doc.RootElement.Clone();
                jsonBuffer.Clear();
            }
            catch (JsonException)
            {
                // Incomplete JSON, continue accumulating
                continue;
            }

            yield return element;
        }

        if (_process != null && _process.HasExited)
        {
            if (_process.ExitCode != 0)
            {
                throw new ProcessException(
                    "Command failed",
                    _process.ExitCode,
                    "Check stderr output for details");
            }
        }
    }

    public async Task WriteAsync(string data, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isReady || _stdin == null)
                throw new CliConnectionException("ProcessTransport is not ready for writing");

            if (_process?.HasExited == true)
                throw new CliConnectionException($"Cannot write to terminated process (exit code: {_process.ExitCode})");

            if (_exitError != null)
                throw new CliConnectionException($"Cannot write to process that exited with error: {_exitError.Message}", _exitError);

            try
            {
                await _stdin.WriteAsync(data.AsMemory(), cancellationToken);
                await _stdin.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _isReady = false;
                _exitError = new CliConnectionException($"Failed to write to process stdin: {ex.Message}", ex);
                throw _exitError;
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task EndInputAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (_stdin != null)
            {
                try
                {
                    _stdin.Close();
                }
                catch
                {
                    // Ignore
                }
                _stdin = null;
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_process == null)
        {
            _isReady = false;
            return;
        }

        if (_stderrTask != null)
        {
            try { await _stderrTask.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken); }
            catch (Exception ex) { DiagnosticHelper.LogIgnoredException(ex); }
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            _isReady = false;
            if (_stdin != null)
            {
                try { _stdin.Close(); } catch (Exception ex) { DiagnosticHelper.LogIgnoredException(ex); }
                _stdin = null;
            }
        }
        finally
        {
            _writeLock.Release();
        }

        if (!_process.HasExited)
        {
            try
            {
                _process.Kill();
                await _process.WaitForExitAsync(cancellationToken);
            }
            catch (Exception ex) { DiagnosticHelper.LogIgnoredException(ex); }
        }

        _process.Dispose();
        _process = null;
        _stdout = null;
        _exitError = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _writeLock.Dispose();
    }
}
