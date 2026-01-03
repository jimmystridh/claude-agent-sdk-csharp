using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("=== Options Builder Example ===\n");

// begin-snippet: OptionsBuilder
var options = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(3)
    .WithMaxBudgetUsd(0.50)
    .AddAllowedTools("Read", "Glob", "Grep")
    .AddDisallowedTool("Bash")
    .WithSystemPrompt("You are a helpful code reviewer. Be concise.")
    .Build();
// end-snippet

Console.WriteLine("Built options:");
Console.WriteLine($"  Model: {options.Model}");
Console.WriteLine($"  MaxTurns: {options.MaxTurns}");
Console.WriteLine($"  MaxBudgetUsd: {options.MaxBudgetUsd}");
Console.WriteLine($"  AllowedTools: {string.Join(", ", options.AllowedTools!)}");
Console.WriteLine($"  DisallowedTools: {string.Join(", ", options.DisallowedTools!)}");
Console.WriteLine();

// Example 2: Creating options from existing options using ToBuilder
var baseOptions = new ClaudeAgentOptions
{
    Model = "claude-sonnet-4-20250514",
    MaxTurns = 5,
    PermissionMode = PermissionMode.AcceptEdits
};

var derivedOptions = baseOptions.ToBuilder()
    .WithMaxTurns(10)
    .AddAllowedTool("Write")
    .WithSystemPrompt("You are a code writer.")
    .Build();

Console.WriteLine("Derived options (from base):");
Console.WriteLine($"  Model: {derivedOptions.Model} (inherited)");
Console.WriteLine($"  MaxTurns: {derivedOptions.MaxTurns} (overridden from 5)");
Console.WriteLine($"  PermissionMode: {derivedOptions.PermissionMode} (inherited)");
Console.WriteLine();

// Example 3: Complex configuration with MCP servers
var complexOptions = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .WithMaxTurns(10)
    .WithPermissionMode(PermissionMode.AcceptEdits)
    .AddMcpServer("calculator", new McpStdioServerConfig
    {
        Command = "node",
        Args = new[] { "calculator-server.js" }
    })
    .AddEnv("DEBUG", "true")
    .AddEnv("LOG_LEVEL", "info")
    .AddDir("/path/to/extra/context")
    .WithCwd("/project/root")
    .Build();

Console.WriteLine("Complex options:");
Console.WriteLine($"  Model: {complexOptions.Model}");
Console.WriteLine($"  PermissionMode: {complexOptions.PermissionMode}");
Console.WriteLine($"  Cwd: {complexOptions.Cwd}");
Console.WriteLine($"  Env: {string.Join(", ", complexOptions.Env!.Select(kv => $"{kv.Key}={kv.Value}"))}");
Console.WriteLine($"  MCP Servers: {(complexOptions.McpServers.HasValue ? "configured" : "none")}");
Console.WriteLine();

// Example 4: With agents
var agentOptions = new ClaudeAgentOptionsBuilder()
    .WithModel("claude-sonnet-4-20250514")
    .AddAgent("reviewer", new AgentDefinition
    {
        Description = "Code review specialist",
        Prompt = "You review code for bugs and improvements.",
        Tools = new[] { "Read", "Grep" },
        Model = AgentModel.Haiku
    })
    .AddAgent("writer", new AgentDefinition
    {
        Description = "Code writing specialist",
        Prompt = "You write clean, efficient code.",
        Tools = new[] { "Read", "Write", "Edit" },
        Model = AgentModel.Sonnet
    })
    .Build();

Console.WriteLine("Agent options:");
foreach (var (name, agent) in agentOptions.Agents!)
{
    Console.WriteLine($"  {name}: {agent.Description}");
}
Console.WriteLine();

Console.WriteLine("=== Done ===");
