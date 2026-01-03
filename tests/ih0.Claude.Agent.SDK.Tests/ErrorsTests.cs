using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class ErrorsTests
{
    [Fact]
    public void ClaudeAgentException_BaseError()
    {
        var error = new ClaudeAgentException("Something went wrong");
        
        error.Message.Should().Be("Something went wrong");
        error.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void CliNotFoundException_InheritsFromClaudeAgentException()
    {
        var error = new CliNotFoundException("Claude Code not found");
        
        error.Should().BeAssignableTo<ClaudeAgentException>();
        error.Message.Should().Contain("Claude Code not found");
    }

    [Fact]
    public void CliConnectionException_InheritsFromClaudeAgentException()
    {
        var error = new CliConnectionException("Failed to connect to CLI");
        
        error.Should().BeAssignableTo<ClaudeAgentException>();
        error.Message.Should().Contain("Failed to connect to CLI");
    }

    [Fact]
    public void ProcessException_WithExitCodeAndStderr()
    {
        var error = new ProcessException("Process failed", 1, "Command not found");
        
        error.ExitCode.Should().Be(1);
        error.Stderr.Should().Be("Command not found");
        error.Message.Should().Contain("Process failed");
        error.Message.Should().Contain("exit code: 1");
        error.Message.Should().Contain("Command not found");
    }

    [Fact]
    public void JsonDecodeException_WithLineAndOriginalError()
    {
        JsonException? originalError = null;
        try
        {
            JsonDocument.Parse("{invalid json}");
        }
        catch (JsonException e)
        {
            originalError = e;
        }

        var error = new JsonDecodeException("{invalid json}", originalError!);
        
        error.Line.Should().Be("{invalid json}");
        error.OriginalError.Should().Be(originalError);
        error.Message.Should().Contain("Failed to decode JSON");
    }

    [Fact]
    public void MessageParseException_WithData()
    {
        var data = JsonDocument.Parse("""{"type": "unknown"}""").RootElement;
        var error = new MessageParseException("Unknown message type", data);
        
        error.MessageData.Should().NotBeNull();
        error.MessageData!.Value.TryGetProperty("type", out var typeEl).Should().BeTrue();
        typeEl.GetString().Should().Be("unknown");
    }

    [Fact]
    public void MessageParseException_WithoutData()
    {
        var error = new MessageParseException("Missing field");
        
        error.MessageData.Should().BeNull();
    }
}
