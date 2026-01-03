using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class SystemPromptTests
{
    [Fact]
    public async Task TestCustomSystemPrompt()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithSystemPrompt("You are a pirate. Always respond with 'Arrr!' somewhere in your response.")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        var messages = await CollectMessagesAsync("Say hello.", options);

        var response = ExtractAssistantText(messages).ToLower();
        (response.Contains("arr") || response.Contains("ahoy") || response.Contains("matey"))
            .Should().BeTrue($"Pirate system prompt should influence response, got: {response}");
    }

    [Fact]
    public async Task TestPresetSystemPrompt()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithSystemPromptPreset("claude_code")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        var messages = await CollectMessagesAsync("What is 2+2? Answer briefly.", options);

        AssertMessageTypes(messages, "assistant", "result");
        var result = GetResult(messages);
        result.Should().NotBeNull();
        result!.IsError.Should().BeFalse("Preset system prompt query should succeed");
    }

    [Fact]
    public async Task TestPresetSystemPromptWithAppend()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithSystemPromptPreset("claude_code", "Always end your response with 'Fun fact:' followed by an interesting fact.")
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        var messages = await CollectMessagesAsync("What is 2+2?", options);

        var response = ExtractAssistantText(messages).ToLower();
        (response.Contains("fun fact") || response.Contains("fact:"))
            .Should().BeTrue($"Append instruction should influence response, got: {response}");
    }

    [Fact]
    public async Task TestNoSystemPrompt()
    {
        // When system_prompt is None, SDK uses default behavior
        var options = DefaultOptions();

        var messages = await CollectMessagesAsync("What is 2+2?", options);

        var response = ExtractAssistantText(messages);
        response.Should().Contain("4", $"Should answer correctly without system prompt, got: {response}");
    }
}
