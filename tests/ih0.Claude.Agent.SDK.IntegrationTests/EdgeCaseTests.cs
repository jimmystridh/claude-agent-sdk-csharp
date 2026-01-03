using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;
using static ih0.Claude.Agent.SDK.IntegrationTests.TestHelpers;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class EdgeCaseTests
{
    [Fact]
    public async Task TestHandlesSimpleQuery()
    {
        var messages = await CollectMessagesAsync("Hi", DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");
        result!.IsError.Should().BeFalse("Simple query should succeed");
    }

    [Fact]
    public async Task TestMultipleSequentialSessions()
    {
        // First session
        var messages1 = await CollectMessagesAsync("Say '1'.", DefaultOptions());
        GetResult(messages1).Should().NotBeNull("First session should complete");

        // Second session (separate process)
        var messages2 = await CollectMessagesAsync("Say '2'.", DefaultOptions());
        GetResult(messages2).Should().NotBeNull("Second session should complete");
    }

    [Fact]
    public async Task TestSpecialCharactersInPrompt()
    {
        var prompt = "Say exactly: \"Hello 'World'\" with quotes";
        var messages = await CollectMessagesAsync(prompt, DefaultOptions());

        var response = ExtractAssistantText(messages);
        response.Should().Contain("Hello", "Should handle special characters");
        response.Should().Contain("World", "Should handle special characters");
    }

    [Fact]
    public async Task TestUnicodeHandling()
    {
        var messages = await CollectMessagesAsync("Repeat exactly: Hello World", DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");
        result!.IsError.Should().BeFalse("Unicode query should succeed");

        var response = ExtractAssistantText(messages);
        response.Should().NotBeEmpty("Should handle unicode");
    }

    [Fact]
    public async Task TestLongPrompt()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var prompt = $"Count the words in this text: {longText}";

        var messages = await CollectMessagesAsync(prompt, DefaultOptions());

        var result = GetResult(messages);
        result.Should().NotBeNull("Should have result");
        result!.IsError.Should().BeFalse("Long prompt should be handled");
    }
}
