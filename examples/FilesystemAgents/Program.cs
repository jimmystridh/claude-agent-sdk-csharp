using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Filesystem Agents Example ===");
Console.WriteLine("Testing: SettingSources=[\"project\"] with .claude/agents/ files");
Console.WriteLine();

var sdkDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var options = new ClaudeAgentOptions
{
    SettingSources = new[] { SettingSource.Project },
    Cwd = sdkDir
};

var messageTypes = new List<string>();
var agentsFound = new List<string>();

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();

await client.QueryAsync("Say hello in exactly 3 words");

await foreach (var msg in client.ReceiveResponseAsync())
{
    messageTypes.Add(msg.GetType().Name);

    if (msg is SystemMessage systemMsg && systemMsg.Subtype == "init")
    {
        agentsFound = ExtractAgents(systemMsg.Data);
        Console.WriteLine($"Init message received. Agents loaded: [{string.Join(", ", agentsFound)}]");
    }
    else if (msg is AssistantMessage assistantMsg)
    {
        foreach (var block in assistantMsg.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Assistant: {textBlock.Text}");
            }
        }
    }
    else if (msg is ResultMessage result)
    {
        Console.WriteLine($"Result: subtype={result.Subtype}, cost=${result.TotalCostUsd ?? 0:F4}");
    }
}

Console.WriteLine();
Console.WriteLine("=== Summary ===");
Console.WriteLine($"Message types received: [{string.Join(", ", messageTypes)}]");
Console.WriteLine($"Total messages: {messageTypes.Count}");

var hasInit = messageTypes.Contains("SystemMessage");
var hasAssistant = messageTypes.Contains("AssistantMessage");
var hasResult = messageTypes.Contains("ResultMessage");

Console.WriteLine();
if (hasInit && hasAssistant && hasResult)
{
    Console.WriteLine("SUCCESS: Received full response (init, assistant, result)");
}
else
{
    Console.WriteLine("FAILURE: Did not receive full response");
    Console.WriteLine($"  - Init: {hasInit}");
    Console.WriteLine($"  - Assistant: {hasAssistant}");
    Console.WriteLine($"  - Result: {hasResult}");
}

static List<string> ExtractAgents(JsonElement data)
{
    var result = new List<string>();
    if (data.TryGetProperty("agents", out var agentsEl) && agentsEl.ValueKind == JsonValueKind.Array)
    {
        foreach (var agent in agentsEl.EnumerateArray())
        {
            if (agent.ValueKind == JsonValueKind.String)
            {
                result.Add(agent.GetString() ?? "");
            }
            else if (agent.ValueKind == JsonValueKind.Object && agent.TryGetProperty("name", out var nameEl))
            {
                result.Add(nameEl.GetString() ?? "");
            }
        }
    }
    return result;
}
