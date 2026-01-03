using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Extensions;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== Configuration Example ===\n");

// Build configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Example 1: Load options directly from configuration
Console.WriteLine("Example 1: Load from default section 'Claude'");
var options = configuration.GetClaudeAgentOptions();

Console.WriteLine($"  Model: {options.Model}");
Console.WriteLine($"  MaxTurns: {options.MaxTurns}");
Console.WriteLine($"  MaxBudgetUsd: {options.MaxBudgetUsd}");
Console.WriteLine($"  PermissionMode: {options.PermissionMode}");
Console.WriteLine($"  AllowedTools: {string.Join(", ", options.AllowedTools ?? [])}");
Console.WriteLine($"  DisallowedTools: {string.Join(", ", options.DisallowedTools ?? [])}");
Console.WriteLine($"  Cwd: {options.Cwd}");
Console.WriteLine($"  Env: {string.Join(", ", options.Env?.Select(kv => $"{kv.Key}={kv.Value}") ?? [])}");
Console.WriteLine($"  MaxThinkingTokens: {options.MaxThinkingTokens}");
Console.WriteLine($"  EnableFileCheckpointing: {options.EnableFileCheckpointing}");
Console.WriteLine($"  User: {options.User}");
Console.WriteLine();

// Example 2: Load from a custom section
Console.WriteLine("Example 2: Load from custom section 'AltClaude'");
var altOptions = configuration.GetClaudeAgentOptions("AltClaude");

Console.WriteLine($"  Model: {altOptions.Model}");
Console.WriteLine($"  MaxTurns: {altOptions.MaxTurns}");
Console.WriteLine($"  PermissionMode: {altOptions.PermissionMode}");
Console.WriteLine();

// Example 3: Load as builder and customize
Console.WriteLine("Example 3: Load as builder and customize");
var customOptions = configuration
    .GetClaudeAgentOptionsBuilder()
    .WithMaxTurns(20)  // Override the config value
    .AddAllowedTool("Write")  // Add to the existing tools
    .WithSystemPrompt("You are a helpful assistant.")
    .Build();

Console.WriteLine($"  Model: {customOptions.Model} (from config)");
Console.WriteLine($"  MaxTurns: {customOptions.MaxTurns} (overridden from 10 to 20)");
Console.WriteLine($"  AllowedTools: {string.Join(", ", customOptions.AllowedTools ?? [])} (added Write)");
Console.WriteLine($"  SystemPrompt: {(customOptions.SystemPrompt.HasValue ? "set" : "not set")}");
Console.WriteLine();

Console.WriteLine("=== Done ===");
