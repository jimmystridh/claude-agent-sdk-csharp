using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Plugin Example ===\n");

// Get the path to the demo plugin from the Python SDK examples
var pluginPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "claude-agent-sdk-python", "examples", "plugins", "demo-plugin"));

var options = new ClaudeAgentOptions
{
    Plugins = new[]
    {
        new SdkPluginConfig
        {
            PluginType = "local",
            Path = pluginPath
        }
    },
    MaxTurns = 1
};

Console.WriteLine($"Loading plugin from: {pluginPath}\n");

var foundPlugins = false;

await foreach (var message in ClaudeAgent.QueryAsync("Hello!", options))
{
    if (message is SystemMessage systemMsg && systemMsg.Subtype == "init")
    {
        Console.WriteLine("System initialized!");

        var keys = new List<string>();
        foreach (var prop in systemMsg.Data.EnumerateObject())
        {
            keys.Add(prop.Name);
        }
        Console.WriteLine($"System message data keys: [{string.Join(", ", keys)}]\n");

        if (systemMsg.Data.TryGetProperty("plugins", out var pluginsEl) &&
            pluginsEl.ValueKind == JsonValueKind.Array)
        {
            Console.WriteLine("Plugins loaded:");
            foreach (var plugin in pluginsEl.EnumerateArray())
            {
                var name = plugin.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "unknown";
                var path = plugin.TryGetProperty("path", out var pathEl) ? pathEl.GetString() : "unknown";
                Console.WriteLine($"  - {name} (path: {path})");
            }
            foundPlugins = true;
        }
        else
        {
            Console.WriteLine("Note: Plugin was passed via CLI but may not appear in system message.");
            Console.WriteLine($"Plugin path configured: {pluginPath}");
            foundPlugins = true;
        }
    }
    else if (message is AssistantMessage assistantMsg)
    {
        foreach (var block in assistantMsg.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
        }
    }
}

if (foundPlugins)
{
    Console.WriteLine("\nPlugin successfully configured!");
}
