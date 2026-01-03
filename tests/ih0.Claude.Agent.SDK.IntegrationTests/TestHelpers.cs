using System.Diagnostics;
using System.Text;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;
using OneOf;

namespace ih0.Claude.Agent.SDK.IntegrationTests;

public static class TestHelpers
{
    public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

    public static ClaudeAgentOptions DefaultOptions() =>
        new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

    public static ClaudeAgentOptions QuickOptions() =>
        new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.Default)
            .WithMaxTurns(1)
            .Build();

    public static ClaudeAgentOptions ToolTestOptions(params string[] allowedTools) =>
        new ClaudeAgentOptionsBuilder()
            .WithPermissionMode(PermissionMode.BypassPermissions)
            .AddAllowedTools(allowedTools)
            .WithMaxTurns(3)
            .Build();

    public static async Task<List<Message>> CollectMessagesAsync(
        string prompt,
        ClaudeAgentOptions options,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TestTimeout);

        var messages = new List<Message>();
        await foreach (var message in ClaudeAgent.QueryAsync(prompt, options, cancellationToken: cts.Token))
        {
            messages.Add(message);
        }
        return messages;
    }

    public static async Task<List<Message>> CollectMessagesVerboseAsync(
        string prompt,
        ClaudeAgentOptions options,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TestTimeout);

        var messages = new List<Message>();
        try
        {
            await foreach (var message in ClaudeAgent.QueryAsync(prompt, options, cancellationToken: cts.Token))
            {
                messages.Add(message);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Stream error after {messages.Count} messages: {ex}");
            throw;
        }

        return messages;
    }

    public static string ExtractAssistantText(IEnumerable<Message> messages)
    {
        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            if (message is AssistantMessage assistant)
            {
                foreach (var block in assistant.Content)
                {
                    if (block is TextBlock text)
                    {
                        sb.Append(text.Text);
                    }
                }
            }
        }
        return sb.ToString();
    }

    public static string GetResponseText(IEnumerable<Message> messages) =>
        ExtractAssistantText(messages);

    public static ResultMessage? GetResult(IEnumerable<Message> messages) =>
        messages.OfType<ResultMessage>().FirstOrDefault();

    public static void AssertMessageTypes(IEnumerable<Message> messages, params string[] expectedTypes)
    {
        foreach (var expectedType in expectedTypes)
        {
            var found = messages.Any(m => expectedType switch
            {
                "system" => m is SystemMessage,
                "assistant" => m is AssistantMessage,
                "user" => m is UserMessage,
                "result" => m is ResultMessage,
                "stream_event" => m is StreamEvent,
                _ => false
            });

            if (!found)
            {
                throw new Exception($"Expected message type '{expectedType}' not found in messages");
            }
        }
    }

    public static void AssertResponseContains(IEnumerable<Message> messages, string expected)
    {
        var response = ExtractAssistantText(messages);
        if (!response.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Expected response to contain '{expected}'\nActual response:\n{response}");
        }
    }

    public static IEnumerable<ToolUseBlock> ExtractToolUses(IEnumerable<Message> messages) =>
        messages
            .OfType<AssistantMessage>()
            .SelectMany(m => m.Content)
            .OfType<ToolUseBlock>();

    public static int CountClaudeProcesses()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "tasklist",
                    Arguments = "/FI \"IMAGENAME eq claude*\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return 0;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Split('\n')
                    .Count(l => l.Contains("claude", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    Arguments = "-f claude",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return 0;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }
        catch
        {
            return 0;
        }
    }

    public static async Task<T> WithRetryAsync<T>(
        int maxAttempts,
        Func<Task<T>> action)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                lastException = ex;

                if (attempt < maxAttempts - 1)
                {
                    var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
                    await Task.Delay(delay);
                }
            }
        }

        throw lastException!;
    }
}
