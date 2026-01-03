using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

// Example with no system prompt (vanilla Claude)
Console.WriteLine("=== No System Prompt (Vanilla Claude) ===");

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?"))
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

Console.WriteLine();

// Example with system prompt as a string
Console.WriteLine("=== String System Prompt ===");

var stringOptions = new ClaudeAgentOptions
{
    SystemPrompt = "You are a pirate assistant. Respond in pirate speak."
};

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?", stringOptions))
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

Console.WriteLine();

// Example with system prompt preset (uses default Claude Code prompt)
Console.WriteLine("=== Preset System Prompt (Default) ===");

var presetOptions = new ClaudeAgentOptions
{
    SystemPrompt = new SystemPromptPreset()
};

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?", presetOptions))
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

Console.WriteLine();

// Example with system prompt preset and append
Console.WriteLine("=== Preset System Prompt with Append ===");

var presetAppendOptions = new ClaudeAgentOptions
{
    SystemPrompt = new SystemPromptPreset
    {
        Append = "Always end your response with a fun fact."
    }
};

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?", presetAppendOptions))
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
