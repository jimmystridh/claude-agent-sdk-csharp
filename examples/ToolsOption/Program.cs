using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

// Example with tools as array of specific tool names
Console.WriteLine("=== Tools Array Example ===");
Console.WriteLine("Setting tools=[\"Read\", \"Glob\", \"Grep\"]");
Console.WriteLine();

var arrayOptions = new ClaudeAgentOptions
{
    Tools = new[] { "Read", "Glob", "Grep" },
    MaxTurns = 1
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "What tools do you have available? Just list them briefly.",
    arrayOptions))
{
    if (message is SystemMessage systemMsg && systemMsg.Subtype == "init")
    {
        var tools = ExtractTools(systemMsg.Data);
        Console.WriteLine($"Tools from system message: [{string.Join(", ", tools)}]");
        Console.WriteLine();
    }
    else if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
        }
    }
    else if (message is ResultMessage result && result.TotalCostUsd > 0)
    {
        Console.WriteLine($"\nCost: ${result.TotalCostUsd:F4}");
    }
}

Console.WriteLine();

// Example with tools as empty array (disables all built-in tools)
Console.WriteLine("=== Tools Empty Array Example ===");
Console.WriteLine("Setting tools=[] (disables all built-in tools)");
Console.WriteLine();

var emptyOptions = new ClaudeAgentOptions
{
    Tools = Array.Empty<string>(),
    MaxTurns = 1
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "What tools do you have available? Just list them briefly.",
    emptyOptions))
{
    if (message is SystemMessage systemMsg && systemMsg.Subtype == "init")
    {
        var tools = ExtractTools(systemMsg.Data);
        Console.WriteLine($"Tools from system message: [{string.Join(", ", tools)}]");
        Console.WriteLine();
    }
    else if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
        }
    }
    else if (message is ResultMessage result && result.TotalCostUsd > 0)
    {
        Console.WriteLine($"\nCost: ${result.TotalCostUsd:F4}");
    }
}

Console.WriteLine();

// Example with tools preset (all default Claude Code tools)
Console.WriteLine("=== Tools Preset Example ===");
Console.WriteLine("Setting tools = ToolsPreset { Preset = \"claude_code\" }");
Console.WriteLine();

var presetOptions = new ClaudeAgentOptions
{
    Tools = new ToolsPreset(),
    MaxTurns = 1
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "What tools do you have available? Just list them briefly.",
    presetOptions))
{
    if (message is SystemMessage systemMsg && systemMsg.Subtype == "init")
    {
        var tools = ExtractTools(systemMsg.Data);
        var preview = tools.Count > 5 ? string.Join(", ", tools.Take(5)) + "..." : string.Join(", ", tools);
        Console.WriteLine($"Tools from system message ({tools.Count} tools): [{preview}]");
        Console.WriteLine();
    }
    else if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
        }
    }
    else if (message is ResultMessage result && result.TotalCostUsd > 0)
    {
        Console.WriteLine($"\nCost: ${result.TotalCostUsd:F4}");
    }
}

static List<string> ExtractTools(JsonElement data)
{
    var result = new List<string>();
    if (data.TryGetProperty("tools", out var toolsEl) && toolsEl.ValueKind == JsonValueKind.Array)
    {
        foreach (var tool in toolsEl.EnumerateArray())
        {
            if (tool.ValueKind == JsonValueKind.String)
            {
                result.Add(tool.GetString() ?? "");
            }
        }
    }
    return result;
}
