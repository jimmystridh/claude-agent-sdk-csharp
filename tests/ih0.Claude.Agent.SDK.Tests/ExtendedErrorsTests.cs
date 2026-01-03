using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Extended error handling tests matching Rust SDK test_errors.rs
/// </summary>
public class ExtendedErrorsTests
{
    public class CliNotFoundErrorTests
    {
        [Fact]
        public void CliNotFoundError_Creation()
        {
            var err = new CliNotFoundException("Claude not in PATH");
            err.Message.Should().Contain("Claude not in PATH");
        }

        [Fact]
        public void CliNotFoundError_WithPath()
        {
            var err = new CliNotFoundException("CLI not found", "/usr/bin/claude");
            err.Message.Should().Contain("/usr/bin/claude");
            err.CliPath.Should().Be("/usr/bin/claude");
        }
    }

    public class CliConnectionErrorTests
    {
        [Fact]
        public void CliConnectionError_Creation()
        {
            var err = new CliConnectionException("Connection refused");
            err.Message.Should().Contain("Connection refused");
        }

        [Fact]
        public void CliConnectionError_InheritsFromClaudeAgentException()
        {
            var err = new CliConnectionException("test");
            err.Should().BeAssignableTo<ClaudeAgentException>();
        }
    }

    public class ProcessErrorTests
    {
        [Fact]
        public void ProcessError_WithExitCode()
        {
            var err = new ProcessException("Command failed", exitCode: 1);
            err.Message.Should().Contain("exit code");
            err.Message.Should().Contain("1");
            err.ExitCode.Should().Be(1);
        }

        [Fact]
        public void ProcessError_WithStderr()
        {
            var err = new ProcessException("Command failed", exitCode: 2, stderr: "Error: invalid argument");
            err.Stderr.Should().Be("Error: invalid argument");
            err.ExitCode.Should().Be(2);
        }

        [Fact]
        public void ProcessError_WithoutExitCode()
        {
            var err = new ProcessException("Process terminated unexpectedly");
            err.ExitCode.Should().BeNull();
            err.Stderr.Should().BeNull();
        }
    }

    public class JsonDecodeErrorTests
    {
        [Fact]
        public void JsonDecodeError_Creation()
        {
            var originalError = new JsonException("Invalid JSON");
            var err = new JsonDecodeException("Invalid JSON", originalError);
            err.Message.Should().Contain("Invalid JSON");
            err.OriginalError.Should().Be(originalError);
        }

        [Fact]
        public void JsonDecodeError_TruncatesLongLines()
        {
            var longLine = new string('x', 200);
            var originalError = new JsonException("parse error");
            var err = new JsonDecodeException(longLine, originalError);

            // Should truncate to around 100 characters plus "..."
            err.Message.Should().Contain("...");
            err.Line.Should().Be(longLine);
        }
    }

    public class MessageParseErrorTests
    {
        [Fact]
        public void MessageParseError_Creation()
        {
            var err = new MessageParseException("Unknown message type");
            err.Message.Should().Contain("Unknown message type");
        }

        [Fact]
        public void MessageParseError_WithRawMessage()
        {
            var raw = JsonDocument.Parse("""{"type": "unknown"}""").RootElement;
            var err = new MessageParseException("Unknown type", raw);
            err.MessageData.Should().NotBeNull();
            err.MessageData!.Value.GetProperty("type").GetString().Should().Be("unknown");
        }
    }

    public class ExceptionHierarchyTests
    {
        [Fact]
        public void AllExceptions_InheritFromClaudeAgentException()
        {
            new CliConnectionException().Should().BeAssignableTo<ClaudeAgentException>();
            new CliNotFoundException().Should().BeAssignableTo<ClaudeAgentException>();
            new ProcessException("test").Should().BeAssignableTo<ClaudeAgentException>();
            new MessageParseException("test").Should().BeAssignableTo<ClaudeAgentException>();
        }

        [Fact]
        public void CliNotFoundException_InheritsFromCliConnectionException()
        {
            new CliNotFoundException().Should().BeAssignableTo<CliConnectionException>();
        }
    }

    public class ExceptionDisplayTests
    {
        [Fact]
        public void AllExceptions_HaveValidDisplayMessages()
        {
            var exceptions = new ClaudeAgentException[]
            {
                new ClaudeAgentException("base error"),
                new CliConnectionException("connection failed"),
                new CliNotFoundException("not found"),
                new ProcessException("process failed", 1),
                new MessageParseException("parse error"),
                new JsonDecodeException("bad json", new JsonException())
            };

            foreach (var ex in exceptions)
            {
                var message = ex.ToString();
                message.Should().NotBeNullOrEmpty();
            }
        }
    }

    public class ExceptionChainTests
    {
        [Fact]
        public void ClaudeAgentException_WithInnerException()
        {
            var inner = new InvalidOperationException("inner error");
            var err = new ClaudeAgentException("outer error", inner);

            err.InnerException.Should().Be(inner);
            err.Message.Should().Be("outer error");
        }

        [Fact]
        public void CliConnectionException_WithInnerException()
        {
            var inner = new System.Net.Sockets.SocketException();
            var err = new CliConnectionException("connection failed", inner);

            err.InnerException.Should().Be(inner);
        }
    }

    public class ErrorIdentificationTests
    {
        [Fact]
        public void CliNotFoundException_IsCliError()
        {
            var err = new CliNotFoundException("not found");
            err.Should().BeAssignableTo<CliConnectionException>();
        }

        [Fact]
        public void ProcessException_HasExitCode()
        {
            var err = new ProcessException("failed", exitCode: 127);
            err.ExitCode.Should().Be(127);
        }
    }
}
