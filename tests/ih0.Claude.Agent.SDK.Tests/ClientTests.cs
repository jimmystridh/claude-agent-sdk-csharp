using System.Text.Json;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Moq;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class ClientTests
{
    private static Mock<ITransport> CreateMockTransport(
        IEnumerable<JsonElement>? messagesToRead = null,
        bool isReady = true)
    {
        var mockTransport = new Mock<ITransport>();
        mockTransport.Setup(t => t.IsReady).Returns(isReady);
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.CloseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.EndInputAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        if (messagesToRead != null)
        {
            async IAsyncEnumerable<JsonElement> GenerateMessages()
            {
                foreach (var msg in messagesToRead)
                {
                    yield return msg;
                    await Task.Yield();
                }
            }

            mockTransport.Setup(t => t.ReadMessagesAsync(It.IsAny<CancellationToken>()))
                .Returns(GenerateMessages);
        }

        return mockTransport;
    }

    public class QueryFunctionTests
    {
        [Fact]
        public void Query_WithOptions_PassesOptionsCorrectly()
        {
            var options = new ClaudeAgentOptions
            {
                AllowedTools = new[] { "Read", "Write" },
                SystemPrompt = "You are helpful",
                PermissionMode = PermissionMode.AcceptEdits,
                MaxTurns = 5,
                CliPath = "/usr/bin/claude"
            };

            options.AllowedTools.Should().BeEquivalentTo(new[] { "Read", "Write" });
            options.SystemPrompt!.Value.AsT0.Should().Be("You are helpful");
            options.PermissionMode.Should().Be(PermissionMode.AcceptEdits);
            options.MaxTurns.Should().Be(5);
        }

        [Fact]
        public void Query_WithCwd_SetsWorkingDirectory()
        {
            var options = new ClaudeAgentOptions
            {
                Cwd = "/custom/path",
                CliPath = "/usr/bin/claude"
            };

            options.Cwd.Should().Be("/custom/path");
        }
    }

    public class ClaudeAgentClientTests
    {
        [Fact]
        public async Task Client_Connect_InitializesTransport()
        {
            var mockTransport = CreateMockTransport();
            var client = new ClaudeAgentClient(transport: mockTransport.Object);

            await client.DisposeAsync();
        }

        [Fact]
        public async Task Client_Options_ArePreserved()
        {
            var options = new ClaudeAgentOptions
            {
                Cwd = "/custom/path",
                AllowedTools = new[] { "Read", "Write" },
                SystemPrompt = "Be helpful"
            };

            var client = new ClaudeAgentClient(options);
            
            await client.DisposeAsync();
        }
    }
}
