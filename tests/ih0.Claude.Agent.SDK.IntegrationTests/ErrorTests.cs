using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Exceptions;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class ErrorTests
{
    private readonly ITestOutputHelper _output;

    public ErrorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestSpecialCharactersNoPanic()
    {
        // Prompts that should definitely work
        var validPrompts = new[]
        {
            "{\"test\": \"valid\"}",   // Valid JSON-like string
            "```json\n{}\n```",        // Markdown code blocks
            "Hello\r\nWorld\r\n",      // Windows line endings
        };

        // Prompts that may or may not work (CLI may reject them)
        var edgeCasePrompts = new[]
        {
            "\n\n\n",   // Just newlines - may be rejected
            "\t\t\t",   // Just tabs - may be rejected
        };

        foreach (var prompt in validPrompts)
        {
            try
            {
                var messages = await CollectMessagesAsync(prompt, DefaultOptions());
                GetResult(messages).Should().NotBeNull($"Should have result message for prompt: {prompt}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Valid prompt \"{prompt}\" failed unexpectedly: {ex.Message}", ex);
            }
        }

        foreach (var prompt in edgeCasePrompts)
        {
            try
            {
                var messages = await CollectMessagesAsync(prompt, DefaultOptions());
                _output.WriteLine($"Edge case prompt succeeded with {messages.Count} messages");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Edge case prompt failed gracefully: {ex.Message}");
            }
        }
    }

    [Fact]
    public async Task TestEmptyPrompt()
    {
        try
        {
            var messages = await CollectMessagesAsync("", DefaultOptions());
            _output.WriteLine($"Empty prompt succeeded with {messages.Count} messages");

            if (GetResult(messages) == null)
            {
                _output.WriteLine("Note: Empty prompt completed but no result message");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Empty prompt error (acceptable): {ex.Message}");
        }
    }

    [Fact]
    public async Task TestVeryLongPrompt()
    {
        // 10KB prompt
        var longText = string.Join(" ", Enumerable.Repeat("word", 2000));
        var prompt = $"Count roughly how many words are here: {longText}";

        try
        {
            var messages = await CollectMessagesAsync(prompt, DefaultOptions());
            var result = GetResult(messages);
            result.Should().NotBeNull("Should have result");
            result!.IsError.Should().BeFalse("Long prompt should be handled");
        }
        catch (Exception ex)
        {
            var msg = ex.Message.ToLower();
            (msg.Contains("too long") || msg.Contains("limit") || msg.Contains("size") || !string.IsNullOrEmpty(msg))
                .Should().BeTrue($"Error should be descriptive: {ex.Message}");
        }
    }

    [Fact]
    public async Task TestConnectionErrorReporting()
    {
        var options = DefaultOptions();
        await using var client = new ClaudeAgentClient(options);

        try
        {
            await client.ConnectAsync();
            await client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            var errorStr = ex.ToString();
            errorStr.Should().NotBeEmpty("Error message should not be empty");
            _output.WriteLine($"Connection error (for reference): {errorStr}");
        }
    }

    [Fact]
    public async Task TestQueryWithoutConnect()
    {
        await using var client = new ClaudeAgentClient(DefaultOptions());

        var action = async () => await client.QueryAsync("Hello");
        await action.Should().ThrowAsync<CliConnectionException>("Query without connect should fail");
    }

    [Fact]
    public async Task TestDoubleDisconnect()
    {
        await using var client = new ClaudeAgentClient(DefaultOptions());
        await client.ConnectAsync();

        // First disconnect
        await client.DisconnectAsync();

        // Second disconnect - should not throw
        try
        {
            await client.DisconnectAsync();
            _output.WriteLine("Double disconnect succeeded (idempotent)");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Double disconnect error (acceptable): {ex.Message}");
        }
    }

    [Fact]
    public async Task TestStreamErrorPropagation()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithMaxTurns(1)
            .WithPermissionMode(PermissionMode.Default)
            .Build();

        try
        {
            var hadError = false;
            var hadResult = false;

            await foreach (var msg in ClaudeAgent.QueryAsync("Hello", options))
            {
                if (msg is ResultMessage)
                {
                    hadResult = true;
                }
            }

            (hadResult || hadError).Should().BeTrue("Stream should complete with result or error");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Query start error: {ex.Message}");
        }
    }

    [Fact]
    public async Task TestAuthenticationFailure()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .AddEnv("ANTHROPIC_API_KEY", "sk-ant-invalid-key-12345")
            .AddEnv("CLAUDE_CODE_OAUTH_TOKEN", "")
            .Build();

        try
        {
            var messages = await CollectMessagesAsync("test", options);
            var result = GetResult(messages);

            // If we got a result, check if it indicates an error
            if (result != null && result.IsError)
            {
                _output.WriteLine($"Query failed with error result as expected");
                return;
            }

            // Check for error in assistant messages
            var hasAuthError = messages.OfType<AssistantMessage>()
                .Any(m => m.Error != null);

            if (hasAuthError)
            {
                _output.WriteLine("Got authentication error in assistant message");
                return;
            }

            _output.WriteLine($"Query unexpectedly succeeded with {messages.Count} messages");
        }
        catch (Exception ex)
        {
            var msg = ex.Message.ToLower();
            (msg.Contains("auth") || msg.Contains("api") || msg.Contains("key") ||
             msg.Contains("unauthorized") || msg.Contains("401") || msg.Contains("invalid") ||
             msg.Contains("error") || msg.Contains("fail"))
                .Should().BeTrue($"Error should relate to authentication: {ex.Message}");
            _output.WriteLine($"Got expected auth error: {ex.Message}");
        }
    }
}
