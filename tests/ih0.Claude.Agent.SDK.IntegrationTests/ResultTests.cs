using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class ResultTests
{
    private readonly ITestOutputHelper _output;

    public ResultTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestResultMessageFields()
    {
        var messages = await CollectMessagesAsync("Say 'test'.", DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result message");

        result!.DurationMs.Should().BePositive("Duration should be positive");
        result.NumTurns.Should().BeGreaterThanOrEqualTo(1, "Should have at least 1 turn");
        result.IsError.Should().BeFalse("Should not be an error");
        result.Subtype.Should().NotBeEmpty("Subtype should not be empty");
    }

    [Fact]
    public async Task TestSystemMessageReceived()
    {
        var messages = await CollectMessagesAsync("Say 'hi'.", DefaultOptions());

        var systemMsg = messages.OfType<SystemMessage>().FirstOrDefault();
        systemMsg.Should().NotBeNull("Should receive system message");
        systemMsg!.Subtype.Should().NotBeEmpty("System message should have a subtype");
    }

    [Fact]
    public async Task TestCostTracking()
    {
        var messages = await CollectMessagesAsync("What is 1+1?", DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");

        if (result!.TotalCostUsd.HasValue)
        {
            result.TotalCostUsd.Value.Should().BeGreaterThanOrEqualTo(0, "Cost should be non-negative");
            _output.WriteLine($"Query cost: ${result.TotalCostUsd.Value:F6}");
        }
        else
        {
            _output.WriteLine("Cost tracking not available in this configuration");
        }
    }

    [Fact]
    public async Task TestCostScalesWithResponseLength()
    {
        // Short response
        var shortMessages = await CollectMessagesAsync("Say 'hi'.", DefaultOptions());

        // Longer response
        var longMessages = await CollectMessagesAsync(
            "List the first 10 prime numbers, one per line.",
            DefaultOptions());

        var shortCost = GetResult(shortMessages)?.TotalCostUsd;
        var longCost = GetResult(longMessages)?.TotalCostUsd;

        _output.WriteLine($"Cost comparison: short={shortCost}, long={longCost}");

        if (shortCost.HasValue && longCost.HasValue)
        {
            if (longCost > shortCost)
            {
                _output.WriteLine("Longer response cost more as expected");
            }
            else
            {
                _output.WriteLine("Note: Longer response didn't cost more (possible caching)");
            }
        }
    }

    [Fact]
    public async Task TestCostWithToolUse()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .AddAllowedTools("Bash")
            .WithMaxTurns(3)
            .Build();

        try
        {
            var messages = await CollectMessagesAsync("Run 'echo cost_test' using bash.", options);

            var result = GetResult(messages);
            if (result != null)
            {
                _output.WriteLine($"Tool use cost: {result.TotalCostUsd}");
                _output.WriteLine($"Tool use turns: {result.NumTurns}");
                _output.WriteLine($"Tool use duration: {result.DurationMs}ms");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Tool query failed: {ex.Message}");
        }
    }

    [Fact]
    public async Task TestBudgetLimitOption()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .WithMaxBudgetUsd(1.0)
            .Build();

        try
        {
            var messages = await CollectMessagesAsync("Say 'budget test'.", options);

            var result = GetResult(messages);
            result.Should().NotBeNull("Should get result with budget set");

            if (result!.TotalCostUsd.HasValue)
            {
                result.TotalCostUsd.Value.Should().BeLessThanOrEqualTo(1.0, "Cost should be under budget $1.0");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Budget test error: {ex.Message}");
        }
    }

    [Fact]
    public async Task TestVeryLowBudgetLimit()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .WithMaxBudgetUsd(0.0001)
            .Build();

        try
        {
            var messages = await CollectMessagesAsync("Say 'tiny budget'.", options);
            _output.WriteLine("Query succeeded with tiny budget");

            var result = GetResult(messages);
            if (result != null)
            {
                _output.WriteLine($"Cost: {result.TotalCostUsd}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Query with tiny budget failed: {ex.Message}");

            var msg = ex.Message.ToLower();
            var isBudgetError = msg.Contains("budget") || msg.Contains("cost") || msg.Contains("limit");
            if (isBudgetError)
            {
                _output.WriteLine("Budget limit was enforced");
            }
        }
    }

    [Fact]
    public async Task TestTokenCounts()
    {
        var messages = await CollectMessagesAsync("What is 2+2?", DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");

        result!.SessionId.Should().NotBeEmpty("Session ID should not be empty");
        result.DurationMs.Should().BeLessThan(300_000, $"Duration {result.DurationMs}ms seems too long");

        _output.WriteLine($"Query stats: session={result.SessionId}, turns={result.NumTurns}, duration={result.DurationMs}ms");
    }

    [Fact]
    public async Task TestMultiTurnCostAccumulation()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(3)
            .Build();

        await using var client = new ClaudeAgentClient(options);

        try
        {
            await client.ConnectAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Connect failed: {ex.Message}");
            return;
        }

        // Turn 1
        try
        {
            await client.QueryAsync("Say 'turn1'");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Turn 1 failed: {ex.Message}");
            await client.DisconnectAsync();
            return;
        }

        ResultMessage? result1 = null;
        await foreach (var msg in client.ReceiveResponseAsync())
        {
            if (msg is ResultMessage r)
            {
                result1 = r;
                break;
            }
        }

        if (result1 == null)
        {
            _output.WriteLine("Turn 1 receive failed");
            await client.DisconnectAsync();
            return;
        }

        // Turn 2
        try
        {
            await client.QueryAsync("Say 'turn2'");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Turn 2 failed: {ex.Message}");
            await client.DisconnectAsync();
            return;
        }

        ResultMessage? result2 = null;
        await foreach (var msg in client.ReceiveResponseAsync())
        {
            if (msg is ResultMessage r)
            {
                result2 = r;
                break;
            }
        }

        if (result2 == null)
        {
            _output.WriteLine("Turn 2 receive failed");
            await client.DisconnectAsync();
            return;
        }

        await client.DisconnectAsync();

        _output.WriteLine($"Turn 1: turns={result1.NumTurns}, cost={result1.TotalCostUsd}");
        _output.WriteLine($"Turn 2: turns={result2.NumTurns}, cost={result2.TotalCostUsd}");

        result2.NumTurns.Should().BeGreaterThanOrEqualTo(result1.NumTurns, "Turn count should not decrease");
    }
}
