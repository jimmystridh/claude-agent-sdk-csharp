using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Hooks Example ===");

// begin-snippet: Hooks
HookCallback preToolUseHook = (input, toolUseId, context) =>
{
    if (input is PreToolUseHookInput preToolUse)
    {
        Console.WriteLine($"Tool: {preToolUse.ToolName}");

        // Block dangerous commands
        if (preToolUse.ToolName == "Bash")
        {
            var command = preToolUse.ToolInput.GetProperty("command").GetString();
            if (command?.Contains("rm -rf") == true)
                return Task.FromResult(new HookOutput { Decision = "deny" });
        }
    }
    return Task.FromResult(new HookOutput { Continue = true });
};

var options = new ClaudeAgentOptions
{
    Hooks = new Dictionary<HookEvent, IReadOnlyList<HookMatcher>>
    {
        [HookEvent.PreToolUse] = new List<HookMatcher>
        {
            new HookMatcher { Matcher = "tool:Bash", Hooks = new[] { preToolUseHook } }
        }
    }
};
// end-snippet

HookCallback postToolUseHook = (input, toolUseId, context) =>
{
    if (input is PostToolUseHookInput postToolUse)
    {
        Console.WriteLine($"[Hook] Tool {postToolUse.ToolName} completed");
    }

    return Task.FromResult(new HookOutput { Continue = true });
};

options = new ClaudeAgentOptions
{
    AllowedTools = new[] { "Bash" },
    Hooks = new Dictionary<HookEvent, IReadOnlyList<HookMatcher>>
    {
        [HookEvent.PreToolUse] = new List<HookMatcher>
        {
            new HookMatcher
            {
                Matcher = "tool:Bash",
                Hooks = new List<HookCallback> { preToolUseHook }
            }
        },
        [HookEvent.PostToolUse] = new List<HookMatcher>
        {
            new HookMatcher
            {
                Matcher = "tool:Bash",
                Hooks = new List<HookCallback> { postToolUseHook }
            }
        }
    }
};

await foreach (var message in ClaudeAgent.QueryAsync("List files in the current directory", options))
{
    if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Claude: {textBlock.Text}");
            }
        }
    }
}
