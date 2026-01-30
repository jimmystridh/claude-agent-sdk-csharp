using System.Text.Json;
using FluentAssertions;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using Xunit;
using Xunit.Abstractions;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public class McpStatusTests
{
    private readonly ITestOutputHelper _output;

    public McpStatusTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestGetMcpStatusDoesNotThrow()
    {
        // This test is intentionally permissive:
        // - It ensures the control request wiring works and doesn't hang.
        // - It does NOT require MCP servers to be configured.

        var options = new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

        await using var client = new ClaudeAgentClient(options);
        await client.ConnectAsync();

        JsonElement status = await client.GetMcpStatusAsync();

        // We expect a JSON object; if no MCP servers are configured it may be empty
        // or contain an empty mcpServers list.
        status.ValueKind.Should().Be(JsonValueKind.Object);

        if (status.TryGetProperty("mcpServers", out var servers))
        {
            _output.WriteLine($"mcpServers: {servers.GetRawText()}");
        }
        else
        {
            _output.WriteLine($"MCP status response: {status.GetRawText()}");
        }
    }
}
