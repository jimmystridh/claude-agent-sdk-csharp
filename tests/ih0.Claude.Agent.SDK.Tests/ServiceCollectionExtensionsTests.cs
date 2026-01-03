using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Extensions;
using ih0.Claude.Agent.SDK.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddClaudeAgent_RegistersService()
    {
        var services = new ServiceCollection();

        services.AddClaudeAgent();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IClaudeAgentService>();

        Assert.NotNull(service);
        Assert.IsType<ClaudeAgentService>(service);
    }

    [Fact]
    public void AddClaudeAgent_WithBuilder_RegistersService()
    {
        var services = new ServiceCollection();

        services.AddClaudeAgent(builder => builder
            .WithModel("claude-sonnet-4-20250514")
            .WithMaxTurns(10));

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IClaudeAgentService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddClaudeAgent_WithOptions_RegistersService()
    {
        var services = new ServiceCollection();
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-sonnet-4-20250514")
            .Build();

        services.AddClaudeAgent(options);

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IClaudeAgentService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddClaudeAgent_WithConfiguration_RegistersService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:Model"] = "claude-sonnet-4-20250514",
                ["Claude:MaxTurns"] = "10"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddClaudeAgent(config);

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IClaudeAgentService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddClaudeAgent_WithConfigurationAndBuilder_RegistersService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:Model"] = "claude-sonnet-4-20250514",
                ["Claude:MaxTurns"] = "5"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddClaudeAgent(config, builder => builder
            .WithMaxTurns(20));  // Override config value

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IClaudeAgentService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddClaudeAgent_ReturnsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClaudeAgent();

        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<IClaudeAgentService>();
        var service2 = provider.GetService<IClaudeAgentService>();

        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddClaudeAgent_DoesNotOverwriteExistingRegistration()
    {
        var services = new ServiceCollection();

        // First registration
        services.AddClaudeAgent(builder => builder.WithModel("first"));

        // Second registration (should be ignored due to TryAddSingleton)
        services.AddClaudeAgent(builder => builder.WithModel("second"));

        // Only one registration should exist
        var registrations = services.Where(s => s.ServiceType == typeof(IClaudeAgentService)).ToList();
        Assert.Single(registrations);
    }
}

public class ClaudeAgentServiceTests
{
    [Fact]
    public void Constructor_WithOptions_SetsOptions()
    {
        var options = new ClaudeAgentOptionsBuilder()
            .WithModel("claude-sonnet-4-20250514")
            .WithMaxTurns(10)
            .Build();

        var service = new ClaudeAgentService(options);

        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        var service = new ClaudeAgentService((ClaudeAgentOptions)null!);

        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithLoggerFactory_SetsLoggerFactory()
    {
        var options = new ClaudeAgentOptions();

        var service = new ClaudeAgentService(options, null);

        Assert.NotNull(service);
    }

    [Fact]
    public void DefaultConstructor_CreatesService()
    {
        var service = new ClaudeAgentService();

        Assert.NotNull(service);
    }
}
