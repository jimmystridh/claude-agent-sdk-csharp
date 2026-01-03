using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class CoreTests
{
    [Fact]
    public async Task TestOneshotQueryEndToEnd()
    {
        var messages = await CollectMessagesAsync(
            "What is 2+2? Answer with just the number.",
            DefaultOptions());

        AssertMessageTypes(messages, "system", "assistant", "result");

        var response = ExtractAssistantText(messages);
        response.Should().Contain("4", "Response should contain '4'");

        var result = GetResult(messages);
        result.Should().NotBeNull();
        result!.IsError.Should().BeFalse("Query should not have errored");
        result.NumTurns.Should().BeGreaterOrEqualTo(1, "Should have at least 1 turn");
    }

    [Fact]
    public async Task TestStreamingClientMultiTurn()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(3)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        // First query
        await client.QueryAsync("What is 2+2? Answer with just the number.");

        var response1 = new List<Message>();
        await foreach (var message in client.ReceiveResponseAsync())
        {
            response1.Add(message);
        }

        var text1 = ExtractAssistantText(response1);
        text1.Should().Contain("4", "First response should contain '4'");

        var result1 = GetResult(response1);
        result1.Should().NotBeNull();
        result1!.IsError.Should().BeFalse();

        // Second query (follow-up using conversation context)
        await client.QueryAsync("Multiply that by 10. Answer with just the number.");

        var response2 = new List<Message>();
        await foreach (var message in client.ReceiveResponseAsync())
        {
            response2.Add(message);
        }

        var text2 = ExtractAssistantText(response2);
        text2.Should().Contain("40", "Second response should use context and contain '40'");

        var result2 = GetResult(response2);
        result2.Should().NotBeNull();
        result2!.IsError.Should().BeFalse();

        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestStreamingUserMessageFormat()
    {
        var options = DefaultOptions();
        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        await client.QueryAsync("Say 'hello' and nothing else.");

        var response = new List<Message>();
        await foreach (var message in client.ReceiveResponseAsync())
        {
            response.Add(message);
        }

        var result = GetResult(response);
        result.Should().NotBeNull();
        result!.IsError.Should().BeFalse("Query should succeed");

        var text = ExtractAssistantText(response);
        text.ToLower().Should().Contain("hello", "Response should contain 'hello'");

        await client.DisconnectAsync();
    }

    [Fact]
    public async Task TestStreamClosesAfterCompletion()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var count = 0;
        await foreach (var message in ClaudeAgent.QueryAsync("Say 'done'.", DefaultOptions(), cancellationToken: cts.Token))
        {
            count++;
        }

        count.Should().BeGreaterOrEqualTo(3, "Should receive at least 3 messages (system, assistant, result)");
    }

    [Fact]
    public async Task TestNonstreamingStdinHandling()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var messages = await CollectMessagesAsync("Say 'ok'.", DefaultOptions(), cts.Token);

        GetResult(messages).Should().NotBeNull("Should receive result message");
    }
}
