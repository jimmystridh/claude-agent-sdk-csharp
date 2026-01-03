using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;

namespace ih0.Claude.Agent.SDK;

/// <summary>
/// Interface for Claude Agent operations, enabling dependency injection and testability.
/// </summary>
/// <remarks>
/// Use this interface when you need to:
/// <list type="bullet">
///   <item><description>Inject Claude Agent as a dependency</description></item>
///   <item><description>Mock Claude Agent in unit tests</description></item>
///   <item><description>Use different implementations in different environments</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI container
/// services.AddClaudeAgent(options =>
/// {
///     options.Model = "claude-sonnet-4-20250514";
///     options.MaxTurns = 10;
/// });
///
/// // Inject and use
/// public class MyService
/// {
///     private readonly IClaudeAgentService _claude;
///
///     public MyService(IClaudeAgentService claude)
///     {
///         _claude = claude;
///     }
///
///     public async Task&lt;string&gt; ProcessAsync(string prompt)
///     {
///         await foreach (var message in _claude.QueryAsync(prompt))
///         {
///             if (message is ResultMessage result)
///             {
///                 return result.Result;
///             }
///         }
///         return string.Empty;
///     }
/// }
/// </code>
/// </example>
public interface IClaudeAgentService
{
    /// <summary>
    /// Queries Claude with a prompt and streams back messages.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude.</param>
    /// <param name="options">Optional overrides for the default options. These are merged with the configured defaults.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of messages from Claude.</returns>
    /// <example>
    /// <code>
    /// await foreach (var message in claude.QueryAsync("Explain async/await"))
    /// {
    ///     switch (message)
    ///     {
    ///         case AssistantMessage assistant:
    ///             Console.WriteLine(assistant.Content);
    ///             break;
    ///         case ResultMessage result:
    ///             Console.WriteLine($"Result: {result.Result}");
    ///             break;
    ///     }
    /// }
    /// </code>
    /// </example>
    IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries Claude with a streaming prompt and streams back messages.
    /// </summary>
    /// <param name="prompt">An async enumerable of JSON elements representing the prompt.</param>
    /// <param name="options">Optional overrides for the default options. These are merged with the configured defaults.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of messages from Claude.</returns>
    /// <remarks>
    /// This overload is useful for conversational scenarios where the prompt
    /// consists of multiple message turns.
    /// </remarks>
    IAsyncEnumerable<Message> QueryAsync(
        IAsyncEnumerable<JsonElement> prompt,
        ClaudeAgentOptions? options = null,
        CancellationToken cancellationToken = default);
}
