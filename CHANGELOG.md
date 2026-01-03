# Changelog

All notable changes to the Claude Agent SDK for C# will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-01-03

### Added

#### Core Features
- Initial C# port of the Claude Agent SDK from Python
- `ClaudeAgent` static class for one-shot queries
- `ClaudeAgentClient` for interactive, bidirectional conversations
- Full async/await support with `IAsyncEnumerable<Message>` streaming
- Comprehensive type system matching the Python SDK

#### Options and Configuration
- `ClaudeAgentOptions` record with 27+ configuration properties
- `ClaudeAgentOptionsBuilder` fluent builder pattern for easy configuration
- `IConfiguration` support via `GetClaudeAgentOptions()` extension method
- Support for loading options from `appsettings.json`

#### Dependency Injection
- `IClaudeAgentService` interface for DI and testability
- `ClaudeAgentService` implementation with options merging
- `AddClaudeAgent()` extension methods for `IServiceCollection`:
  - Configure with builder pattern
  - Load from `IConfiguration`
  - Combine configuration with programmatic overrides

#### MCP Server Support
- In-process SDK MCP servers via `ISdkMcpServer` interface
- `SdkMcpServer` base class with `[Tool]` attribute for easy tool definition
- Support for external stdio and SSE MCP servers
- Mixed server configurations (SDK + external)

#### Hooks System
- Pre/Post tool use hooks
- User prompt submit hooks
- Stop and subagent stop hooks
- Pre-compact hooks
- `HookCallback` delegate for custom hook implementations

#### Permission System
- `PermissionMode` enum (Default, AcceptEdits, BypassPermissions, Plan)
- `CanUseTool` callback for programmatic permission decisions
- `PermissionResult`, `PermissionAllow`, `PermissionDeny` types

#### Logging and Diagnostics
- `Microsoft.Extensions.Logging` integration
- `ILoggerFactory` support throughout the SDK
- `DiagnosticHelper` for debug-only exception logging
- Thread-safe state management with `volatile` flags

#### Documentation
- Full XML documentation on all public types and members
- Code examples in documentation comments
- 21 example projects demonstrating various features

### Technical Improvements
- Record types for immutable message and options types
- `OneOf<T0, T1>` discriminated unions for type-safe alternatives
- Source-generated JSON serialization context (partial)
- Proper async enumerable patterns with `[EnumeratorCancellation]`

### Examples Included
- `QuickStart` - Basic usage
- `StreamingMode` - Interactive client usage
- `OptionsBuilder` - Fluent builder pattern
- `ConfigurationExample` - Loading from appsettings.json
- `DependencyInjection` - DI container integration
- `Hooks` - Custom hook implementations
- `McpCalculator` - In-process MCP server
- `ToolPermissions` - Permission callbacks
- `Agents` - Subagent definitions
- And more...

### Dependencies
- .NET 10.0+
- Microsoft.Extensions.Logging.Abstractions 9.0.0
- Microsoft.Extensions.Configuration.Abstractions 9.0.0
- Microsoft.Extensions.Configuration.Binder 9.0.0
- Microsoft.Extensions.DependencyInjection.Abstractions 9.0.0
- OneOf 3.0.271
