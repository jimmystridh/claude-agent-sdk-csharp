using System.Text.Json;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Moq;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class StreamingClientTests
{
    private static Mock<ITransport> CreateMockTransport(
        Func<IAsyncEnumerable<JsonElement>>? messageGenerator = null,
        bool isReady = true)
    {
        var mockTransport = new Mock<ITransport>();
        var writtenMessages = new List<string>();

        mockTransport.Setup(t => t.IsReady).Returns(isReady);
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.CloseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.EndInputAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((data, _) => writtenMessages.Add(data))
            .Returns(Task.CompletedTask);

        if (messageGenerator != null)
        {
            mockTransport.Setup(t => t.ReadMessagesAsync(It.IsAny<CancellationToken>()))
                .Returns(messageGenerator);
        }

        return mockTransport;
    }

    public class ConnectionTests
    {
        [Fact]
        public async Task DisconnectWithoutConnect_DoesNotThrow()
        {
            var client = new ClaudeAgentClient();

            // Should not throw
            await client.DisconnectAsync();
        }

        [Fact]
        public async Task QueryNotConnected_ThrowsCliConnectionException()
        {
            var client = new ClaudeAgentClient();

            var action = async () => await client.QueryAsync("Test");

            await action.Should().ThrowAsync<CliConnectionException>()
                .WithMessage("*Not connected*");
        }

        [Fact]
        public async Task InterruptNotConnected_ThrowsCliConnectionException()
        {
            var client = new ClaudeAgentClient();

            var action = async () => await client.InterruptAsync();

            await action.Should().ThrowAsync<CliConnectionException>()
                .WithMessage("*Not connected*");
        }

        [Fact]
        public async Task ReceiveMessagesNotConnected_ThrowsCliConnectionException()
        {
            var client = new ClaudeAgentClient();

            var action = async () =>
            {
                await foreach (var _ in client.ReceiveMessagesAsync())
                {
                }
            };

            await action.Should().ThrowAsync<CliConnectionException>()
                .WithMessage("*Not connected*");
        }

        [Fact]
        public async Task ReceiveResponseNotConnected_ThrowsCliConnectionException()
        {
            var client = new ClaudeAgentClient();

            var action = async () =>
            {
                await foreach (var _ in client.ReceiveResponseAsync())
                {
                }
            };

            await action.Should().ThrowAsync<CliConnectionException>()
                .WithMessage("*Not connected*");
        }
    }

    public class MessageParsingTests
    {
        [Fact]
        public void AssistantMessage_CorrectlyParsed()
        {
            var json = """
            {
                "type": "assistant",
                "message": {
                    "content": [{"type": "text", "text": "Hello!"}],
                    "model": "claude-opus-4-1-20250805"
                }
            }
            """;
            var data = JsonDocument.Parse(json).RootElement;
            var message = Claude.Agent.SDK.Internal.MessageParser.Parse(data);

            message.Should().BeOfType<AssistantMessage>();
            var assistantMessage = (AssistantMessage)message;
            assistantMessage.Content.Should().HaveCount(1);
            assistantMessage.Content[0].Should().BeOfType<TextBlock>();
            ((TextBlock)assistantMessage.Content[0]).Text.Should().Be("Hello!");
        }

        [Fact]
        public void UserMessage_CorrectlyParsed()
        {
            var json = """
            {
                "type": "user",
                "message": {"content": "Hi there"}
            }
            """;
            var data = JsonDocument.Parse(json).RootElement;
            var message = Claude.Agent.SDK.Internal.MessageParser.Parse(data);

            message.Should().BeOfType<UserMessage>();
            var userMessage = (UserMessage)message;
            userMessage.Content.IsT0.Should().BeTrue();
            userMessage.Content.AsT0.Should().Be("Hi there");
        }

        [Fact]
        public void ResultMessage_CorrectlyParsed()
        {
            var json = """
            {
                "type": "result",
                "subtype": "success",
                "duration_ms": 1000,
                "duration_api_ms": 800,
                "is_error": false,
                "num_turns": 1,
                "session_id": "test",
                "total_cost_usd": 0.001
            }
            """;
            var data = JsonDocument.Parse(json).RootElement;
            var message = Claude.Agent.SDK.Internal.MessageParser.Parse(data);

            message.Should().BeOfType<ResultMessage>();
            var resultMessage = (ResultMessage)message;
            resultMessage.Subtype.Should().Be("success");
            resultMessage.TotalCostUsd.Should().Be(0.001);
        }
    }

    public class ClientOptionsTests
    {
        [Fact]
        public void ClientWithOptions_PreservesSettings()
        {
            var options = new ClaudeAgentOptions
            {
                Cwd = "/custom/path",
                AllowedTools = new[] { "Read", "Write" },
                SystemPrompt = "Be helpful"
            };

            var client = new ClaudeAgentClient(options);

            // Client should be created successfully with options
            client.Should().NotBeNull();
        }

        [Fact]
        public void ClientWithCustomTransport_UsesProvidedTransport()
        {
            var mockTransport = CreateMockTransport();
            var client = new ClaudeAgentClient(transport: mockTransport.Object);

            client.Should().NotBeNull();
        }
    }

    public class PermissionModeTests
    {
        [Fact]
        public void PermissionMode_AllValuesAreDefined()
        {
            Enum.GetValues<PermissionMode>().Should().HaveCount(4);
            Enum.IsDefined(PermissionMode.Default).Should().BeTrue();
            Enum.IsDefined(PermissionMode.AcceptEdits).Should().BeTrue();
            Enum.IsDefined(PermissionMode.Plan).Should().BeTrue();
            Enum.IsDefined(PermissionMode.BypassPermissions).Should().BeTrue();
        }
    }

    public class ServerInfoTests
    {
        [Fact]
        public void GetServerInfo_WhenNotConnected_ReturnsNull()
        {
            var client = new ClaudeAgentClient();

            var info = client.GetServerInfo();

            info.Should().BeNull();
        }
    }
}
