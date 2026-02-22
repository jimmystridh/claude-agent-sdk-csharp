namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Base class for thinking configuration.
/// </summary>
public abstract record ThinkingConfig;

/// <summary>
/// Adaptive thinking configuration that adjusts based on the task.
/// </summary>
/// <remarks>
/// When <see cref="BudgetTokens"/> is not set and no legacy <c>MaxThinkingTokens</c> is configured,
/// defaults to 32000 tokens.
/// </remarks>
public sealed record ThinkingConfigAdaptive : ThinkingConfig
{
    /// <summary>
    /// Optional budget in tokens for thinking. Defaults to 32000 when not set.
    /// </summary>
    public int? BudgetTokens { get; init; }
}

/// <summary>
/// Explicitly enabled thinking with a required token budget.
/// </summary>
public sealed record ThinkingConfigEnabled : ThinkingConfig
{
    /// <summary>
    /// The token budget for thinking.
    /// </summary>
    public required int BudgetTokens { get; init; }
}

/// <summary>
/// Disabled thinking configuration.
/// </summary>
public sealed record ThinkingConfigDisabled : ThinkingConfig;
