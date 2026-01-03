namespace ih0.Claude.Agent.SDK.Exceptions;

/// <summary>
/// Base exception for all Claude Agent SDK errors.
/// </summary>
public class ClaudeAgentException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class.
    /// </summary>
    public ClaudeAgentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ClaudeAgentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeAgentException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ClaudeAgentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when connection to Claude Code fails.
/// </summary>
public class CliConnectionException : ClaudeAgentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class.
    /// </summary>
    public CliConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CliConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliConnectionException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CliConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when the Claude CLI executable is not found.
/// </summary>
public class CliNotFoundException : CliConnectionException
{
    /// <summary>
    /// The path that was searched for the CLI.
    /// </summary>
    public string? CliPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="cliPath">The path that was searched.</param>
    public CliNotFoundException(string message = "Claude Code not found", string? cliPath = null)
        : base(cliPath != null ? $"{message}: {cliPath}" : message)
    {
        CliPath = cliPath;
    }
}

/// <summary>
/// Exception thrown when a subprocess fails.
/// </summary>
public class ProcessException : ClaudeAgentException
{
    /// <summary>
    /// The exit code of the failed process.
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// The stderr output from the failed process.
    /// </summary>
    public string? Stderr { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="stderr">The stderr output.</param>
    public ProcessException(string message, int? exitCode = null, string? stderr = null)
        : base(FormatMessage(message, exitCode, stderr))
    {
        ExitCode = exitCode;
        Stderr = stderr;
    }

    private static string FormatMessage(string message, int? exitCode, string? stderr)
    {
        if (exitCode.HasValue)
        {
            message = $"{message} (exit code: {exitCode})";
        }
        if (!string.IsNullOrEmpty(stderr))
        {
            message = $"{message}\nError output: {stderr}";
        }
        return message;
    }
}

/// <summary>
/// Exception thrown when JSON parsing fails.
/// </summary>
public class JsonDecodeException : ClaudeAgentException
{
    /// <summary>
    /// The raw line that failed to parse.
    /// </summary>
    public string Line { get; }

    /// <summary>
    /// The original parsing exception.
    /// </summary>
    public Exception OriginalError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDecodeException"/> class.
    /// </summary>
    /// <param name="line">The line that failed to parse.</param>
    /// <param name="originalError">The original parsing exception.</param>
    public JsonDecodeException(string line, Exception originalError)
        : base($"Failed to decode JSON: {(line.Length > 100 ? line[..100] + "..." : line)}", originalError)
    {
        Line = line;
        OriginalError = originalError;
    }
}

/// <summary>
/// Exception thrown when message parsing fails.
/// </summary>
public class MessageParseException : ClaudeAgentException
{
    /// <summary>
    /// The raw message data that failed to parse.
    /// </summary>
    public System.Text.Json.JsonElement? MessageData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="data">The raw message data.</param>
    public MessageParseException(string message, System.Text.Json.JsonElement? data = null) : base(message)
    {
        MessageData = data;
    }
}
