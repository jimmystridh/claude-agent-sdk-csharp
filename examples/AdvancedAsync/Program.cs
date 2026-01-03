using System.Runtime.CompilerServices;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Advanced Async Patterns Example ===\n");

// 1. Parallel Queries with Task.WhenAll
await ParallelQueriesExample();

// 2. Cancellation Token Example
await CancellationExample();

// 3. First Response Wins (Task.WhenAny pattern)
await FirstResponseWinsExample();

// 4. Streaming with Timeout
await StreamingWithTimeoutExample();

// 5. Async enumerable transformation
await AsyncEnumerableTransformExample();

Console.WriteLine("\n=== All examples completed ===");

// ============================================================================
// Example 1: Run multiple independent queries in parallel
// ============================================================================
async Task ParallelQueriesExample()
{
    Console.WriteLine("--- 1. Parallel Queries (Task.WhenAll) ---\n");

    var questions = new[]
    {
        "What is 10 + 5? Answer with just the number.",
        "What is 20 * 3? Answer with just the number.",
        "What is 100 / 4? Answer with just the number."
    };

    var options = new ClaudeAgentOptions { MaxTurns = 1 };

    // Launch all queries in parallel
    var tasks = questions.Select(async question =>
    {
        var result = new List<string>();
        await foreach (var message in ClaudeAgent.QueryAsync(question, options))
        {
            if (message is AssistantMessage am)
            {
                foreach (var block in am.Content)
                {
                    if (block is TextBlock tb)
                        result.Add(tb.Text);
                }
            }
        }
        return (Question: question, Answer: string.Join(" ", result));
    }).ToArray();

    // Wait for all to complete
    var results = await Task.WhenAll(tasks);

    foreach (var (question, answer) in results)
    {
        Console.WriteLine($"Q: {question}");
        Console.WriteLine($"A: {answer}\n");
    }
}

// ============================================================================
// Example 2: Cancellation token support
// ============================================================================
async Task CancellationExample()
{
    Console.WriteLine("--- 2. Cancellation Token Example ---\n");

    using var cts = new CancellationTokenSource();

    // Cancel after 2 seconds
    cts.CancelAfter(TimeSpan.FromSeconds(2));

    var options = new ClaudeAgentOptions { MaxTurns = 1 };

    try
    {
        Console.WriteLine("Starting query with 2-second timeout...");
        await foreach (var message in ClaudeAgent.QueryAsync(
            "What is 2 + 2? Answer briefly.",
            options,
            loggerFactory: null,
            cancellationToken: cts.Token))
        {
            if (message is AssistantMessage am)
            {
                foreach (var block in am.Content)
                {
                    if (block is TextBlock tb)
                        Console.WriteLine($"Response: {tb.Text}");
                }
            }
        }
        Console.WriteLine("Query completed before timeout.\n");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Query was cancelled due to timeout.\n");
    }
}

// ============================================================================
// Example 3: First response wins pattern (useful for redundant queries)
// ============================================================================
async Task FirstResponseWinsExample()
{
    Console.WriteLine("--- 3. First Response Wins (Task.WhenAny) ---\n");

    var options = new ClaudeAgentOptions { MaxTurns = 1 };

    // Helper to get first text response
    async Task<string> GetFirstResponse(string prompt)
    {
        await foreach (var message in ClaudeAgent.QueryAsync(prompt, options))
        {
            if (message is AssistantMessage am)
            {
                foreach (var block in am.Content)
                {
                    if (block is TextBlock tb)
                        return tb.Text;
                }
            }
        }
        return "";
    }

    // Race two similar queries
    var task1 = GetFirstResponse("Say 'Hello from query 1' and nothing else");
    var task2 = GetFirstResponse("Say 'Hello from query 2' and nothing else");

    var winner = await Task.WhenAny(task1, task2);
    var result = await winner;

    Console.WriteLine($"First response received: {result}\n");
}

// ============================================================================
// Example 4: Streaming with per-message timeout
// ============================================================================
async Task StreamingWithTimeoutExample()
{
    Console.WriteLine("--- 4. Streaming with Timeout ---\n");

    await using var client = new ClaudeAgentClient(new ClaudeAgentOptions
    {
        MaxTurns = 1
    });

    await client.ConnectAsync();
    await client.QueryAsync("Count from 1 to 5, one number per line.");

    using var cts = new CancellationTokenSource();

    Console.WriteLine("Receiving messages (5 second overall timeout):");
    cts.CancelAfter(TimeSpan.FromSeconds(5));

    try
    {
        await foreach (var message in client.ReceiveResponseAsync()
            .WithCancellation(cts.Token))
        {
            switch (message)
            {
                case AssistantMessage am:
                    foreach (var block in am.Content)
                    {
                        if (block is TextBlock tb)
                            Console.WriteLine($"  {tb.Text}");
                    }
                    break;
                case ResultMessage rm:
                    Console.WriteLine($"  [Completed: {rm.Subtype}]");
                    break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  [Timed out]");
    }

    Console.WriteLine();
}

// ============================================================================
// Example 5: Transform async enumerable (filter/project pattern)
// ============================================================================
async Task AsyncEnumerableTransformExample()
{
    Console.WriteLine("--- 5. Async Enumerable Transformation ---\n");

    var options = new ClaudeAgentOptions { MaxTurns = 1 };

    // Transform: Extract only TextBlocks from AssistantMessages
    var textBlocks = ExtractTextBlocks(
        ClaudeAgent.QueryAsync("List 3 colors, one per line.", options));

    Console.WriteLine("Extracted text blocks:");
    await foreach (var text in textBlocks)
    {
        Console.WriteLine($"  -> {text}");
    }
    Console.WriteLine();
}

// Helper: Transform IAsyncEnumerable<Message> to IAsyncEnumerable<string>
static async IAsyncEnumerable<string> ExtractTextBlocks(
    IAsyncEnumerable<Message> messages,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var message in messages.WithCancellation(ct))
    {
        if (message is AssistantMessage am)
        {
            foreach (var block in am.Content)
            {
                if (block is TextBlock tb)
                    yield return tb.Text;
            }
        }
    }
}
