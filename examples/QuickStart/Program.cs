// begin-snippet: QuickStart
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?"))
{
    if (message is AssistantMessage assistant)
    {
        foreach (var block in assistant.Content)
        {
            if (block is TextBlock text)
            {
                Console.WriteLine($"Claude: {text.Text}");
            }
        }
    }
}
// end-snippet

Console.WriteLine("=== Basic Example ===");

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
Console.WriteLine("=== With Options Example ===");

var options = new ClaudeAgentOptions
{
    SystemPrompt = "You are a helpful assistant that explains things simply.",
    MaxTurns = 1
};

await foreach (var message in ClaudeAgent.QueryAsync("Explain what C# is in one sentence.", options))
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
    else if (message is ResultMessage result && result.TotalCostUsd > 0)
    {
        Console.WriteLine($"\nCost: ${result.TotalCostUsd:F4}");
    }
}
