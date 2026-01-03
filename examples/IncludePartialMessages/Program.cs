using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("Partial Message Streaming Example");
Console.WriteLine(new string('=', 50));

var options = new ClaudeAgentOptions
{
    IncludePartialMessages = true,
    Model = "claude-sonnet-4-5",
    MaxTurns = 2,
    Env = new Dictionary<string, string>
    {
        ["MAX_THINKING_TOKENS"] = "8000"
    }
};

await using var client = new ClaudeAgentClient(options);
await client.ConnectAsync();

var prompt = "Think of three jokes, then tell one";
Console.WriteLine($"Prompt: {prompt}");
Console.WriteLine(new string('=', 50));

await client.QueryAsync(prompt);

await foreach (var message in client.ReceiveResponseAsync())
{
    switch (message)
    {
        case StreamEvent streamEvent:
            Console.WriteLine($"[StreamEvent] uuid={streamEvent.Uuid[..8]}... event type={streamEvent.Event.GetProperty("type").GetString()}");
            break;
        case AssistantMessage assistantMsg:
            Console.WriteLine("[AssistantMessage]");
            foreach (var block in assistantMsg.Content)
            {
                if (block is TextBlock textBlock)
                {
                    Console.WriteLine($"  Text: {textBlock.Text}");
                }
                else if (block is ThinkingBlock thinkingBlock)
                {
                    var thinkingPreview = thinkingBlock.Thinking.Length > 100
                        ? thinkingBlock.Thinking[..100] + "..."
                        : thinkingBlock.Thinking;
                    Console.WriteLine($"  Thinking: {thinkingPreview}");
                }
            }
            break;
        case UserMessage userMsg:
            Console.WriteLine($"[UserMessage] content type={userMsg.Content.GetType().Name}");
            break;
        case SystemMessage systemMsg:
            Console.WriteLine($"[SystemMessage] subtype={systemMsg.Subtype}");
            break;
        case ResultMessage result:
            Console.WriteLine($"[ResultMessage] subtype={result.Subtype}, cost=${result.TotalCostUsd ?? 0:F4}");
            break;
        default:
            Console.WriteLine($"[{message.GetType().Name}]");
            break;
    }
}
