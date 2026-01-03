using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Code Reviewer Agent Example ===");

var codeReviewerOptions = new ClaudeAgentOptions
{
    Agents = new Dictionary<string, AgentDefinition>
    {
        ["code-reviewer"] = new AgentDefinition
        {
            Description = "Reviews code for best practices and potential issues",
            Prompt = "You are a code reviewer. Analyze code for bugs, performance issues, " +
                     "security vulnerabilities, and adherence to best practices. " +
                     "Provide constructive feedback.",
            Tools = new[] { "Read", "Grep" },
            Model = AgentModel.Sonnet
        }
    }
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "Use the code-reviewer agent to review any code file you can find and give brief feedback",
    codeReviewerOptions))
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

Console.WriteLine();
Console.WriteLine("=== Documentation Writer Agent Example ===");

var docWriterOptions = new ClaudeAgentOptions
{
    Agents = new Dictionary<string, AgentDefinition>
    {
        ["doc-writer"] = new AgentDefinition
        {
            Description = "Writes comprehensive documentation",
            Prompt = "You are a technical documentation expert. Write clear, comprehensive " +
                     "documentation with examples. Focus on clarity and completeness.",
            Tools = new[] { "Read", "Write", "Edit" },
            Model = AgentModel.Sonnet
        }
    }
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "Use the doc-writer agent to briefly explain what custom agents are used for",
    docWriterOptions))
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

Console.WriteLine();
Console.WriteLine("=== Multiple Agents Example ===");

var multiAgentOptions = new ClaudeAgentOptions
{
    Agents = new Dictionary<string, AgentDefinition>
    {
        ["analyzer"] = new AgentDefinition
        {
            Description = "Analyzes code structure and patterns",
            Prompt = "You are a code analyzer. Examine code structure, patterns, and architecture.",
            Tools = new[] { "Read", "Grep", "Glob" }
        },
        ["tester"] = new AgentDefinition
        {
            Description = "Creates and runs tests",
            Prompt = "You are a testing expert. Write comprehensive tests and ensure code quality.",
            Tools = new[] { "Read", "Write", "Bash" },
            Model = AgentModel.Sonnet
        }
    },
    SettingSources = new[] { SettingSource.User, SettingSource.Project }
};

await foreach (var message in ClaudeAgent.QueryAsync(
    "Use the analyzer agent to find all C# files in the current directory",
    multiAgentOptions))
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
