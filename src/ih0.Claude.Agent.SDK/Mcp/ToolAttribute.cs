namespace ih0.Claude.Agent.SDK.Mcp;

/// <summary>
/// Marks a method as an MCP tool.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ToolAttribute : Attribute
{
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A description of what the tool does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">A description of what the tool does.</param>
    public ToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
