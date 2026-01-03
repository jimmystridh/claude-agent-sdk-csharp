using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ih0.Claude.Agent.SDK.Extensions;

/// <summary>
/// Extension methods for registering Claude Agent services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IClaudeAgentService"/> to the service collection with configured options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure the options using a builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Basic registration
    /// services.AddClaudeAgent();
    ///
    /// // With options
    /// services.AddClaudeAgent(builder => builder
    ///     .WithModel("claude-sonnet-4-20250514")
    ///     .WithMaxTurns(10)
    ///     .WithPermissionMode(PermissionMode.AcceptEdits));
    /// </code>
    /// </example>
    public static IServiceCollection AddClaudeAgent(
        this IServiceCollection services,
        Action<ClaudeAgentOptionsBuilder>? configure = null)
    {
        var builder = new ClaudeAgentOptionsBuilder();
        configure?.Invoke(builder);
        var options = builder.Build();

        services.TryAddSingleton<IClaudeAgentService>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new ClaudeAgentService(options, loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IClaudeAgentService"/> to the service collection with the specified options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The options to use.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var options = new ClaudeAgentOptionsBuilder()
    ///     .WithModel("claude-sonnet-4-20250514")
    ///     .Build();
    ///
    /// services.AddClaudeAgent(options);
    /// </code>
    /// </example>
    public static IServiceCollection AddClaudeAgent(
        this IServiceCollection services,
        ClaudeAgentOptions options)
    {
        services.TryAddSingleton<IClaudeAgentService>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new ClaudeAgentService(options, loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IClaudeAgentService"/> to the service collection with options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read options from.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Claude".</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // appsettings.json:
    /// // {
    /// //   "Claude": {
    /// //     "Model": "claude-sonnet-4-20250514",
    /// //     "MaxTurns": 10
    /// //   }
    /// // }
    ///
    /// services.AddClaudeAgent(configuration);
    ///
    /// // Or with a custom section name
    /// services.AddClaudeAgent(configuration, "MyClaudeSettings");
    /// </code>
    /// </example>
    public static IServiceCollection AddClaudeAgent(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Claude")
    {
        var options = configuration.GetClaudeAgentOptions(sectionName);

        services.TryAddSingleton<IClaudeAgentService>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new ClaudeAgentService(options, loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IClaudeAgentService"/> to the service collection with options from configuration
    /// and additional programmatic configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read options from.</param>
    /// <param name="configure">Additional configuration to apply after loading from config.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "Claude".</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Load from config, then override specific settings
    /// services.AddClaudeAgent(configuration, builder => builder
    ///     .WithMaxTurns(20)); // Override the config value
    /// </code>
    /// </example>
    public static IServiceCollection AddClaudeAgent(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ClaudeAgentOptionsBuilder> configure,
        string sectionName = "Claude")
    {
        var builder = configuration.GetClaudeAgentOptionsBuilder(sectionName);
        configure(builder);
        var options = builder.Build();

        services.TryAddSingleton<IClaudeAgentService>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new ClaudeAgentService(options, loggerFactory);
        });

        return services;
    }
}
