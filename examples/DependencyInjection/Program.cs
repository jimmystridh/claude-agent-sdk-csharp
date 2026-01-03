using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Extensions;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== Dependency Injection Example ===\n");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Example 1: Register with builder options
Console.WriteLine("Example 1: Register with builder options");
{
    var services = new ServiceCollection();

    services.AddLogging(builder => builder.AddConsole());
    services.AddClaudeAgent(builder => builder
        .WithModel("claude-sonnet-4-20250514")
        .WithMaxTurns(10)
        .AddAllowedTools("Read", "Glob", "Grep"));

    var provider = services.BuildServiceProvider();
    var claude = provider.GetRequiredService<IClaudeAgentService>();

    Console.WriteLine($"  Got IClaudeAgentService: {claude.GetType().Name}");
    Console.WriteLine();
}

// Example 2: Register from configuration
Console.WriteLine("Example 2: Register from configuration");
{
    var services = new ServiceCollection();

    services.AddLogging(builder => builder.AddConsole());
    services.AddClaudeAgent(configuration);

    var provider = services.BuildServiceProvider();
    var claude = provider.GetRequiredService<IClaudeAgentService>();

    Console.WriteLine($"  Got IClaudeAgentService: {claude.GetType().Name}");
    Console.WriteLine();
}

// Example 3: Register from configuration with overrides
Console.WriteLine("Example 3: Register from configuration with overrides");
{
    var services = new ServiceCollection();

    services.AddLogging(builder => builder.AddConsole());
    services.AddClaudeAgent(configuration, builder => builder
        .WithMaxTurns(20)  // Override the config value
        .AddAllowedTool("Write"));

    var provider = services.BuildServiceProvider();
    var claude = provider.GetRequiredService<IClaudeAgentService>();

    Console.WriteLine($"  Got IClaudeAgentService: {claude.GetType().Name}");
    Console.WriteLine();
}

// Example 4: Using the service in a class
Console.WriteLine("Example 4: Using the service in a class");
{
    var services = new ServiceCollection();

    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    services.AddClaudeAgent(builder => builder
        .WithModel("claude-sonnet-4-20250514")
        .WithMaxTurns(3));

    // Register our demo service that uses IClaudeAgentService
    services.AddTransient<DemoService>();

    var provider = services.BuildServiceProvider();
    var demo = provider.GetRequiredService<DemoService>();

    Console.WriteLine($"  DemoService created with injected IClaudeAgentService");
    Console.WriteLine($"  DemoService.Claude type: {demo.Claude.GetType().Name}");
    Console.WriteLine();
}

// Example 5: Mocking for tests (conceptual)
Console.WriteLine("Example 5: Mocking for tests (conceptual)");
Console.WriteLine("  In unit tests, you can mock IClaudeAgentService:");
Console.WriteLine("  var mockClaude = new Mock<IClaudeAgentService>();");
Console.WriteLine("  mockClaude.Setup(x => x.QueryAsync(It.IsAny<string>(), ...))");
Console.WriteLine("      .Returns(AsyncEnumerable.FromResult(new ResultMessage { ... }));");
Console.WriteLine();

Console.WriteLine("=== Done ===");

// Demo service that uses IClaudeAgentService
public class DemoService
{
    public IClaudeAgentService Claude { get; }

    public DemoService(IClaudeAgentService claude)
    {
        Claude = claude;
    }

    public async Task<string> ProcessPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        await foreach (var message in Claude.QueryAsync(prompt, cancellationToken: cancellationToken))
        {
            if (message is ResultMessage result && result.Result != null)
            {
                return result.Result;
            }
        }
        return string.Empty;
    }
}
