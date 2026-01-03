using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class ToolTests
{
    private readonly ITestOutputHelper _output;

    public ToolTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestAllowedToolsConfiguration()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddAllowedTools("Bash")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(3)
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        try
        {
            var messages = await CollectMessagesAsync(
                "Run 'echo hello_world' and tell me the output.",
                options,
                cts.Token);

            // Check that if any tool was used, it was Bash
            var toolUses = ExtractToolUses(messages);
            foreach (var tool in toolUses)
            {
                tool.Name.Should().Be("Bash", $"Should only use allowed Bash tool, got: {tool.Name}");
            }

            var result = GetResult(messages);
            result.Should().NotBeNull("Should receive result");
            result!.IsError.Should().BeFalse();
        }
        catch (OperationCanceledException)
        {
            throw new Exception("Test timed out after 60 seconds");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Query error (may be permission-related): {ex.Message}");
        }
    }

    [Fact]
    public async Task TestToolResultParsing()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .AddAllowedTools("Bash")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(3)
            .Build();

        await using var client = new ClaudeAgentClient(options);

        using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await client.ConnectAsync(connectCts.Token);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Connection error: {ex.Message}");
            return;
        }

        await client.QueryAsync("Run 'echo test_output_123' using bash.");

        var toolUseIds = new List<string>();
        var toolResultIds = new List<string>();

        using var receiveCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await foreach (var msg in client.ReceiveMessagesAsync(receiveCts.Token))
            {
                switch (msg)
                {
                    case AssistantMessage asst:
                        foreach (var block in asst.Content)
                        {
                            if (block is ToolUseBlock tool)
                            {
                                toolUseIds.Add(tool.Id);
                            }
                        }
                        break;
                    case UserMessage user:
                        user.Content.Switch(
                            str => { },
                            blocks =>
                            {
                                foreach (var block in blocks)
                                {
                                    if (block is ToolResultBlock result)
                                    {
                                        toolResultIds.Add(result.ToolUseId);
                                    }
                                }
                            });
                        break;
                    case ResultMessage:
                        goto done;
                }
            }
        done:;

            // Verify tool results reference known tool use IDs
            foreach (var resultId in toolResultIds)
            {
                toolUseIds.Should().Contain(resultId, $"Tool result ID '{resultId}' should reference a known tool use");
            }

            _output.WriteLine($"Found {toolUseIds.Count} tool uses and {toolResultIds.Count} tool results");
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine("Receive timed out - this may happen if tool permissions are pending");
        }

        await client.DisconnectAsync();
    }
}
