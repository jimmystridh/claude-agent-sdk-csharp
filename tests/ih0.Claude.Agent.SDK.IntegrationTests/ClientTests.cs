using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class ClientTests
{
    [Fact]
    public async Task TestClientBuilderApi()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMaxTurns(2)
            .WithPermissionMode(PermissionMode.Default)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        await client.QueryAsync("What is 3+3? Answer with just the number.");

        var response = new List<Message>();
        await foreach (var message in client.ReceiveResponseAsync())
        {
            response.Add(message);
        }

        var result = GetResult(response);
        result.Should().NotBeNull();
        result!.IsError.Should().BeFalse();

        var text = ExtractAssistantText(response);
        text.Should().Contain("6", "Response should contain '6'");

        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestConnectDisconnectNoQueries()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();
        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestReceiveMessagesStreaming()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        await client.QueryAsync("Count from 1 to 3.");

        var messageTypes = new List<string>();
        await foreach (var msg in client.ReceiveMessagesAsync())
        {
            messageTypes.Add(msg switch
            {
                SystemMessage => "system",
                AssistantMessage => "assistant",
                UserMessage => "user",
                ResultMessage => "result",
                StreamEvent => "stream_event",
                _ => "unknown"
            });

            if (msg is ResultMessage)
                break;
        }

        messageTypes.Should().Contain("assistant", "Should receive assistant messages");
        messageTypes.Should().Contain("result", "Should receive result message");

        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestPartialMessagesOption()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithIncludePartialMessages(true)
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        await client.QueryAsync("Count from 1 to 5.");

        var streamEventCount = 0;
        var gotResult = false;

        await foreach (var msg in client.ReceiveMessagesAsync())
        {
            switch (msg)
            {
                case StreamEvent:
                    streamEventCount++;
                    break;
                case ResultMessage result:
                    gotResult = true;
                    result.IsError.Should().BeFalse();
                    break;
            }

            if (gotResult) break;
        }

        gotResult.Should().BeTrue("Should receive result");

        if (streamEventCount > 0)
        {
            Console.Error.WriteLine($"Received {streamEventCount} stream events");
        }

        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestModelSelection()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-sonnet-4-5")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        var messages = await CollectMessagesAsync("Say 'model test'.", options);

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");
        result!.IsError.Should().BeFalse("Query with explicit model should succeed");
    }

    [Fact]
    public async Task TestMaxTurnsLimit()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        var messages = await CollectMessagesAsync("What is 2+2?", options);

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");
        result!.NumTurns.Should().BeLessThanOrEqualTo(1, $"Should respect max_turns=1, got {result.NumTurns} turns");
    }
}
