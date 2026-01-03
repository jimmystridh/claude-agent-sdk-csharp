using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== MCP Calculator Example ===");

var calculator = new CalculatorServer();

// begin-snippet: McpServer
var options = new ClaudeAgentOptions
{
    McpServers = new Dictionary<string, McpServerConfig>
    {
        ["calculator"] = new McpSdkServerConfig
        {
            Name = "calculator",
            Instance = calculator  // ISdkMcpServer implementation
        }
    },
    SystemPrompt = "You have access to a calculator.",
    MaxTurns = 3
};
// end-snippet

await foreach (var message in ClaudeAgent.QueryAsync("What is 42 * 17 + 123?", options))
{
    if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
            else if (block is ToolUseBlock toolUse)
            {
                Console.WriteLine($"Using tool: {toolUse.Name}");
            }
        }
    }
    else if (message is ResultMessage result)
    {
        Console.WriteLine($"\nResult: {result.Subtype}");
    }
}

public class CalculatorServer : ISdkMcpServer
{
    public string Name => "calculator";
    public string Version => "1.0.0";

    public Task<IReadOnlyList<SdkMcpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = new List<SdkMcpToolDefinition>
        {
            new SdkMcpToolDefinition
            {
                Name = "add",
                Description = "Add two numbers",
                InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "a": {"type": "number"},
                        "b": {"type": "number"}
                    },
                    "required": ["a", "b"]
                }
                """).RootElement
            },
            new SdkMcpToolDefinition
            {
                Name = "multiply",
                Description = "Multiply two numbers",
                InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "a": {"type": "number"},
                        "b": {"type": "number"}
                    },
                    "required": ["a", "b"]
                }
                """).RootElement
            }
        };

        return Task.FromResult<IReadOnlyList<SdkMcpToolDefinition>>(tools);
    }

    public Task<SdkMcpToolResult> CallToolAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        var a = arguments.GetProperty("a").GetDouble();
        var b = arguments.GetProperty("b").GetDouble();

        var result = name switch
        {
            "add" => a + b,
            "multiply" => a * b,
            _ => throw new ArgumentException($"Unknown tool: {name}")
        };

        return Task.FromResult(new SdkMcpToolResult
        {
            Content = new SdkMcpContent[] { new SdkMcpTextContent { Text = result.ToString() } }
        });
    }
}
