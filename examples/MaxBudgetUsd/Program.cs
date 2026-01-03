using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("This example demonstrates using MaxBudgetUsd to control API costs.\n");

// Example without budget limit
Console.WriteLine("=== Without Budget Limit ===");

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
    else if (message is ResultMessage result)
    {
        if (result.TotalCostUsd.HasValue)
        {
            Console.WriteLine($"Total cost: ${result.TotalCostUsd:F4}");
        }
        Console.WriteLine($"Status: {result.Subtype}");
    }
}

Console.WriteLine();

// Example with reasonable budget
Console.WriteLine("=== With Reasonable Budget ($0.10) ===");

var reasonableOptions = new ClaudeAgentOptions
{
    MaxBudgetUsd = 0.10 // 10 cents - plenty for a simple query
};

await foreach (var message in ClaudeAgent.QueryAsync("What is 2 + 2?", reasonableOptions))
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
        if (result.TotalCostUsd.HasValue)
        {
            Console.WriteLine($"Total cost: ${result.TotalCostUsd:F4}");
        }
        Console.WriteLine($"Status: {result.Subtype}");
    }
}

Console.WriteLine();

// Example with tight budget
Console.WriteLine("=== With Tight Budget ($0.0001) ===");

var tightOptions = new ClaudeAgentOptions
{
    MaxBudgetUsd = 0.0001 // Very small budget - will be exceeded quickly
};

await foreach (var message in ClaudeAgent.QueryAsync("Read the README.md file and summarize it", tightOptions))
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
        if (result.TotalCostUsd.HasValue)
        {
            Console.WriteLine($"Total cost: ${result.TotalCostUsd:F4}");
        }
        Console.WriteLine($"Status: {result.Subtype}");

        if (result.Subtype == "error_max_budget_usd")
        {
            Console.WriteLine("Warning: Budget limit exceeded!");
            Console.WriteLine("Note: The cost may exceed the budget by up to one API call's worth");
        }
    }
}

Console.WriteLine();
Console.WriteLine("Note: Budget checking happens after each API call completes,");
Console.WriteLine("so the final cost may slightly exceed the specified budget.");
