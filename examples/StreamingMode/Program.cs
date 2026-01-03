using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Streaming Mode Example ===");

await using var client = new ClaudeAgentClient();
await client.ConnectAsync();

await client.QueryAsync("What are three interesting facts about space?");

await foreach (var message in client.ReceiveResponseAsync())
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
    else if (message is ResultMessage result)
    {
        Console.WriteLine($"\nSession: {result.SessionId}");
        Console.WriteLine($"Turns: {result.NumTurns}");
        if (result.TotalCostUsd.HasValue)
        {
            Console.WriteLine($"Cost: ${result.TotalCostUsd:F4}");
        }
    }
}

Console.WriteLine("\n=== Multi-turn Conversation ===");

await client.QueryAsync("What's the most surprising one?");

await foreach (var message in client.ReceiveResponseAsync())
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
