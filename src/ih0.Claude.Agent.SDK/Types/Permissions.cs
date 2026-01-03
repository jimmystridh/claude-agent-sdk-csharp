using System.Text.Json;
using System.Text.Json.Serialization;

namespace ih0.Claude.Agent.SDK.Types;

/// <summary>
/// Defines a permission rule for a specific tool.
/// </summary>
public sealed record PermissionRuleValue
{
    /// <summary>
    /// The name of the tool this rule applies to.
    /// </summary>
    [JsonPropertyName("toolName")]
    public required string ToolName { get; init; }

    /// <summary>
    /// Additional content for the rule (e.g., path patterns).
    /// </summary>
    [JsonPropertyName("ruleContent")]
    public string? RuleContent { get; init; }
}

/// <summary>
/// Represents an update to permission settings.
/// </summary>
public sealed record PermissionUpdate
{
    /// <summary>
    /// The type of permission update.
    /// </summary>
    public required PermissionUpdateType Type { get; init; }

    /// <summary>
    /// Permission rules to add, replace, or remove.
    /// </summary>
    public IReadOnlyList<PermissionRuleValue>? Rules { get; init; }

    /// <summary>
    /// The behavior to apply to the rules (allow, deny, ask).
    /// </summary>
    public PermissionBehavior? Behavior { get; init; }

    /// <summary>
    /// The permission mode to set.
    /// </summary>
    public PermissionMode? Mode { get; init; }

    /// <summary>
    /// Directories to add or remove.
    /// </summary>
    public IReadOnlyList<string>? Directories { get; init; }

    /// <summary>
    /// Where to apply the update (user, project, local, or session).
    /// </summary>
    public PermissionUpdateDestination? Destination { get; init; }

    /// <summary>
    /// Converts this update to a dictionary for JSON serialization.
    /// </summary>
    /// <returns>A dictionary representation of this update.</returns>
    public Dictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>
        {
            ["type"] = Type switch
            {
                PermissionUpdateType.AddRules => "addRules",
                PermissionUpdateType.ReplaceRules => "replaceRules",
                PermissionUpdateType.RemoveRules => "removeRules",
                PermissionUpdateType.SetMode => "setMode",
                PermissionUpdateType.AddDirectories => "addDirectories",
                PermissionUpdateType.RemoveDirectories => "removeDirectories",
                _ => throw new ArgumentOutOfRangeException()
            }
        };

        if (Destination.HasValue)
        {
            result["destination"] = Destination switch
            {
                PermissionUpdateDestination.UserSettings => "userSettings",
                PermissionUpdateDestination.ProjectSettings => "projectSettings",
                PermissionUpdateDestination.LocalSettings => "localSettings",
                PermissionUpdateDestination.Session => "session",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (Type is PermissionUpdateType.AddRules or PermissionUpdateType.ReplaceRules or PermissionUpdateType.RemoveRules)
        {
            if (Rules != null)
            {
                result["rules"] = Rules.Select(rule => new Dictionary<string, object?>
                {
                    ["toolName"] = rule.ToolName,
                    ["ruleContent"] = rule.RuleContent
                }).ToList();
            }
            if (Behavior.HasValue)
            {
                result["behavior"] = Behavior switch
                {
                    PermissionBehavior.Allow => "allow",
                    PermissionBehavior.Deny => "deny",
                    PermissionBehavior.Ask => "ask",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        else if (Type == PermissionUpdateType.SetMode && Mode.HasValue)
        {
            result["mode"] = Mode switch
            {
                PermissionMode.Default => "default",
                PermissionMode.AcceptEdits => "acceptEdits",
                PermissionMode.Plan => "plan",
                PermissionMode.BypassPermissions => "bypassPermissions",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else if (Type is PermissionUpdateType.AddDirectories or PermissionUpdateType.RemoveDirectories && Directories != null)
        {
            result["directories"] = Directories.ToList();
        }

        return result;
    }
}

/// <summary>
/// Context passed to the CanUseTool callback.
/// </summary>
public sealed record ToolPermissionContext
{
    /// <summary>
    /// Signal object for async coordination.
    /// </summary>
    public object? Signal { get; init; }

    /// <summary>
    /// Suggested permission updates from Claude.
    /// </summary>
    public IReadOnlyList<PermissionUpdate> Suggestions { get; init; } = [];
}

/// <summary>
/// Base class for permission decision results.
/// </summary>
public abstract record PermissionResult
{
    /// <summary>
    /// The behavior of this result (allow or deny).
    /// </summary>
    public abstract string Behavior { get; }
}

/// <summary>
/// Permission result that allows the tool to be used.
/// </summary>
public sealed record PermissionAllow : PermissionResult
{
    /// <summary>
    /// The behavior string for this result.
    /// </summary>
    public override string Behavior => "allow";

    /// <summary>
    /// Modified input to pass to the tool instead of the original.
    /// </summary>
    public JsonElement? UpdatedInput { get; init; }

    /// <summary>
    /// Permission updates to apply alongside this decision.
    /// </summary>
    public IReadOnlyList<PermissionUpdate>? UpdatedPermissions { get; init; }
}

/// <summary>
/// Permission result that denies the tool from being used.
/// </summary>
public sealed record PermissionDeny : PermissionResult
{
    /// <summary>
    /// The behavior string for this result.
    /// </summary>
    public override string Behavior => "deny";

    /// <summary>
    /// Message explaining why the tool was denied.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether to interrupt the conversation after denying.
    /// </summary>
    public bool Interrupt { get; init; }
}
