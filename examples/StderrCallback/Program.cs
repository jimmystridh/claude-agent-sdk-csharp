using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Stderr Callback Example ===");

var stderrMessages = new List<string>();

void StderrCallback(string message)
{
    stderrMessages.Add(message);
    if (message.Contains("[ERROR]"))
    {
        Console.WriteLine($"Error detected: {message}");
    }
}

var options = new ClaudeAgentOptions
{
    Stderr = StderrCallback,
    ExtraArgs = new Dictionary<string, string?>
    {
        ["debug-to-stderr"] = null // Enable debug output (boolean flag)
    }
};

Console.WriteLine("Running query with stderr capture...");

await foreach (var message in ClaudeAgent.QueryAsync("What is 2+2?", options))
{
    if (message is AssistantMessage assistantMessage)
    {
        foreach (var block in assistantMessage.Content)
        {
            if (block is TextBlock textBlock)
            {
                Console.WriteLine($"Response: {textBlock.Text}");
            }
        }
    }
    else if (message is ResultMessage result)
    {
        Console.WriteLine($"Status: {result.Subtype}");
    }
}

Console.WriteLine($"\nCaptured {stderrMessages.Count} stderr lines");
if (stderrMessages.Count > 0)
{
    var firstLine = stderrMessages[0];
    var preview = firstLine.Length > 100 ? firstLine[..100] + "..." : firstLine;
    Console.WriteLine($"First stderr line: {preview}");
}
