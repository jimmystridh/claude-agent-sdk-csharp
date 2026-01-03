using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Tool Permissions Example ===");

var options = new ClaudeAgentOptions
{
    AllowedTools = new[] { "Bash", "Write" },
    CanUseTool = ToolPermissionCallback
};

await foreach (var message in ClaudeAgent.QueryAsync("List files and create a test.txt file", options))
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
                Console.WriteLine($"Tool: {toolUse.Name}");
            }
        }
    }
}

static Task<PermissionResult> ToolPermissionCallback(
    string toolName,
    JsonElement input,
    ToolPermissionContext context)
{
    Console.WriteLine($"[Permission] Tool: {toolName}");
    Console.WriteLine($"[Permission] Input: {input}");

    // Allow Bash with "ls" commands, deny dangerous commands
    if (toolName == "Bash")
    {
        if (input.TryGetProperty("command", out var cmdEl))
        {
            var command = cmdEl.GetString() ?? "";
            if (command.Contains("rm ") || command.Contains("sudo"))
            {
                Console.WriteLine("[Permission] Denied - dangerous command");
                return Task.FromResult<PermissionResult>(new PermissionDeny { Message = "Dangerous commands not allowed" });
            }
        }
    }

    Console.WriteLine("[Permission] Allowed");
    return Task.FromResult<PermissionResult>(new PermissionAllow());
}
