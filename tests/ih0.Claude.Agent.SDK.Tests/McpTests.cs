using System.Text.Json;
using ih0.Claude.Agent.SDK.Mcp;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Tests for MCP (Model Context Protocol) tool support matching Rust SDK test_mcp.rs
/// </summary>
public class McpTests
{
    public class McpContentTests
    {
        [Fact]
        public void TextContent_Creation()
        {
            var content = new SdkMcpTextContent { Text = "Hello, World!" };
            content.Text.Should().Be("Hello, World!");
        }

        [Fact]
        public void TextContent_EmptyText()
        {
            var content = new SdkMcpTextContent { Text = "" };
            content.Text.Should().BeEmpty();
        }

        [Fact]
        public void ImageContent_Creation()
        {
            var content = new SdkMcpImageContent
            {
                Data = "base64data==",
                MimeType = "image/png"
            };

            content.Data.Should().Be("base64data==");
            content.MimeType.Should().Be("image/png");
        }
    }

    public class ToolResultTests
    {
        [Fact]
        public void TextResult_Creation()
        {
            var result = SdkMcpToolBuilder.TextResult("Success!");
            result.Content.Should().HaveCount(1);
            result.IsError.Should().BeNull();
        }

        [Fact]
        public void ErrorResult_Creation()
        {
            var result = SdkMcpToolBuilder.ErrorResult("Something went wrong");
            result.IsError.Should().BeTrue();
            result.Content.Should().HaveCount(1);

            var textContent = result.Content[0] as SdkMcpTextContent;
            textContent.Should().NotBeNull();
            textContent!.Text.Should().Be("Something went wrong");
        }

        [Fact]
        public void ToolResult_WithMultipleContent()
        {
            var result = new SdkMcpToolResult
            {
                Content = new List<SdkMcpContent>
                {
                    new SdkMcpTextContent { Text = "Line 1" },
                    new SdkMcpTextContent { Text = "Line 2" },
                    new SdkMcpTextContent { Text = "Line 3" }
                }
            };
            result.Content.Should().HaveCount(3);
            result.IsError.Should().BeNull();
        }
    }

    public class McpServerConfigSerializationTests
    {
        [Fact]
        public void McpStdioServerConfig_Serialization()
        {
            var config = new McpStdioServerConfig
            {
                Command = "node",
                Args = new[] { "server.js", "--port", "3000" }
            };

            var json = JsonSerializer.SerializeToElement(config);

            json.GetProperty("type").GetString().Should().Be("stdio");
            json.GetProperty("command").GetString().Should().Be("node");
            json.GetProperty("args")[0].GetString().Should().Be("server.js");
        }
    }

    public class SdkMcpServerTests
    {
        [Fact]
        public void CreateServer_Empty()
        {
            var server = SdkMcpServer.Create("empty-server", "1.0.0");

            server.Name.Should().Be("empty-server");
            server.Version.Should().Be("1.0.0");
        }

        [Fact]
        public async Task CreateServer_WithTools()
        {
            var tool1 = SdkMcpToolBuilder.CreateTool(
                "tool1", "First tool",
                new Dictionary<string, Type>(),
                (args, ct) => Task.FromResult(SdkMcpToolBuilder.TextResult("1")));

            var tool2 = SdkMcpToolBuilder.CreateTool(
                "tool2", "Second tool",
                new Dictionary<string, Type>(),
                (args, ct) => Task.FromResult(SdkMcpToolBuilder.TextResult("2")));

            var server = SdkMcpServer.Create("multi-tool", "2.0.0", new[] { tool1, tool2 });

            server.Name.Should().Be("multi-tool");
            server.Version.Should().Be("2.0.0");

            var tools = await server.ListToolsAsync();
            tools.Should().HaveCount(2);
            tools[0].Name.Should().Be("tool1");
            tools[1].Name.Should().Be("tool2");
        }
    }

    public class SdkMcpToolTests
    {
        [Fact]
        public void ToolCreation()
        {
            var tool = SdkMcpToolBuilder.CreateTool(
                "greet",
                "Greet a user",
                new Dictionary<string, Type> { ["name"] = typeof(string) },
                (args, ct) => Task.FromResult(SdkMcpToolBuilder.TextResult("Hello!")));

            tool.Name.Should().Be("greet");
            tool.Description.Should().Be("Greet a user");
        }

        [Fact]
        public async Task ToolHandlerExecution()
        {
            var tool = SdkMcpToolBuilder.CreateTool(
                "add",
                "Add numbers",
                new Dictionary<string, Type> { ["a"] = typeof(int), ["b"] = typeof(int) },
                (args, ct) =>
                {
                    var a = args.TryGetProperty("a", out var aVal) ? aVal.GetInt32() : 0;
                    var b = args.TryGetProperty("b", out var bVal) ? bVal.GetInt32() : 0;
                    return Task.FromResult(SdkMcpToolBuilder.TextResult((a + b).ToString()));
                });

            var input = JsonDocument.Parse("""{"a": 5, "b": 3}""").RootElement;
            var result = await tool.Handler(input, CancellationToken.None);

            result.Content.Should().HaveCount(1);
            var textContent = result.Content[0] as SdkMcpTextContent;
            textContent!.Text.Should().Be("8");
        }

        [Fact]
        public async Task ToolErrorHandling()
        {
            var tool = SdkMcpToolBuilder.CreateTool(
                "divide",
                "Divide numbers",
                new Dictionary<string, Type> { ["a"] = typeof(double), ["b"] = typeof(double) },
                (args, ct) =>
                {
                    var a = args.TryGetProperty("a", out var aVal) ? aVal.GetDouble() : 0.0;
                    var b = args.TryGetProperty("b", out var bVal) ? bVal.GetDouble() : 0.0;
                    if (b == 0.0)
                        return Task.FromResult(SdkMcpToolBuilder.ErrorResult("Division by zero"));
                    return Task.FromResult(SdkMcpToolBuilder.TextResult((a / b).ToString()));
                });

            // Test division by zero
            var zeroInput = JsonDocument.Parse("""{"a": 10.0, "b": 0.0}""").RootElement;
            var zeroResult = await tool.Handler(zeroInput, CancellationToken.None);
            zeroResult.IsError.Should().BeTrue();

            // Test normal division
            var normalInput = JsonDocument.Parse("""{"a": 10.0, "b": 2.0}""").RootElement;
            var normalResult = await tool.Handler(normalInput, CancellationToken.None);
            normalResult.IsError.Should().BeNull();
        }
    }

    public class McpToolWorkflowTests
    {
        [Fact]
        public async Task CalculatorToolsWorkflow()
        {
            var add = SdkMcpToolBuilder.CreateTool(
                "add",
                "Add two numbers",
                new Dictionary<string, Type> { ["a"] = typeof(double), ["b"] = typeof(double) },
                (args, ct) =>
                {
                    var a = args.TryGetProperty("a", out var aVal) ? aVal.GetDouble() : 0.0;
                    var b = args.TryGetProperty("b", out var bVal) ? bVal.GetDouble() : 0.0;
                    return Task.FromResult(SdkMcpToolBuilder.TextResult((a + b).ToString()));
                });

            var multiply = SdkMcpToolBuilder.CreateTool(
                "multiply",
                "Multiply two numbers",
                new Dictionary<string, Type> { ["a"] = typeof(double), ["b"] = typeof(double) },
                (args, ct) =>
                {
                    var a = args.TryGetProperty("a", out var aVal) ? aVal.GetDouble() : 0.0;
                    var b = args.TryGetProperty("b", out var bVal) ? bVal.GetDouble() : 0.0;
                    return Task.FromResult(SdkMcpToolBuilder.TextResult((a * b).ToString()));
                });

            var server = SdkMcpServer.Create("calculator", "1.0.0", new[] { add, multiply });

            server.Name.Should().Be("calculator");
            var tools = await server.ListToolsAsync();
            tools.Should().HaveCount(2);

            // Execute add
            var addInput = JsonDocument.Parse("""{"a": 10, "b": 5}""").RootElement;
            var addResult = await server.CallToolAsync("add", addInput);
            var addText = addResult.Content[0] as SdkMcpTextContent;
            addText!.Text.Should().Be("15");

            // Execute multiply
            var mulInput = JsonDocument.Parse("""{"a": 4, "b": 7}""").RootElement;
            var mulResult = await server.CallToolAsync("multiply", mulInput);
            var mulText = mulResult.Content[0] as SdkMcpTextContent;
            mulText!.Text.Should().Be("28");
        }

        [Fact]
        public async Task StringProcessingTools()
        {
            var uppercase = SdkMcpToolBuilder.CreateTool(
                "uppercase",
                "Convert to uppercase",
                new Dictionary<string, Type> { ["text"] = typeof(string) },
                (args, ct) =>
                {
                    var text = args.TryGetProperty("text", out var v) ? v.GetString() ?? "" : "";
                    return Task.FromResult(SdkMcpToolBuilder.TextResult(text.ToUpper()));
                });

            var reverse = SdkMcpToolBuilder.CreateTool(
                "reverse",
                "Reverse a string",
                new Dictionary<string, Type> { ["text"] = typeof(string) },
                (args, ct) =>
                {
                    var text = args.TryGetProperty("text", out var v) ? v.GetString() ?? "" : "";
                    return Task.FromResult(SdkMcpToolBuilder.TextResult(new string(text.Reverse().ToArray())));
                });

            var server = SdkMcpServer.Create("string-utils", "1.0.0", new[] { uppercase, reverse });

            // Test uppercase
            var upperInput = JsonDocument.Parse("""{"text": "hello"}""").RootElement;
            var upperResult = await server.CallToolAsync("uppercase", upperInput);
            var upperText = upperResult.Content[0] as SdkMcpTextContent;
            upperText!.Text.Should().Be("HELLO");

            // Test reverse
            var revInput = JsonDocument.Parse("""{"text": "hello"}""").RootElement;
            var revResult = await server.CallToolAsync("reverse", revInput);
            var revText = revResult.Content[0] as SdkMcpTextContent;
            revText!.Text.Should().Be("olleh");
        }

        [Fact]
        public async Task ToolWithMissingInput()
        {
            var tool = SdkMcpToolBuilder.CreateTool(
                "greet",
                "Greet someone",
                new Dictionary<string, Type> { ["name"] = typeof(string) },
                (args, ct) =>
                {
                    var name = args.TryGetProperty("name", out var v) ? v.GetString() ?? "stranger" : "stranger";
                    return Task.FromResult(SdkMcpToolBuilder.TextResult($"Hello, {name}!"));
                });

            // Missing name should use default
            var emptyInput = JsonDocument.Parse("{}").RootElement;
            var result = await tool.Handler(emptyInput, CancellationToken.None);
            var textContent = result.Content[0] as SdkMcpTextContent;
            textContent!.Text.Should().Be("Hello, stranger!");
        }

        [Fact]
        public async Task ToolWithComplexOutput()
        {
            var tool = SdkMcpToolBuilder.CreateTool(
                "stats",
                "Calculate statistics",
                new Dictionary<string, Type>(),
                (args, ct) =>
                {
                    return Task.FromResult(new SdkMcpToolResult
                    {
                        Content = new List<SdkMcpContent>
                        {
                            new SdkMcpTextContent { Text = "Count: 10" },
                            new SdkMcpTextContent { Text = "Sum: 55" },
                            new SdkMcpTextContent { Text = "Average: 5.5" }
                        }
                    });
                });

            var emptyInput = JsonDocument.Parse("{}").RootElement;
            var result = await tool.Handler(emptyInput, CancellationToken.None);
            result.Content.Should().HaveCount(3);
        }
    }

    public class McpSdkServerConfigTests
    {
        [Fact]
        public void McpSdkServerConfig_Creation()
        {
            var server = SdkMcpServer.Create("test-server", "1.0.0");
            var config = new McpSdkServerConfig
            {
                Name = "test-server",
                Instance = server
            };

            config.Type.Should().Be("sdk");
            config.Name.Should().Be("test-server");
            config.Instance.Should().Be(server);
        }
    }

    public class SandboxSettingsSerializationTests
    {
        [Fact]
        public void SandboxSettings_Serialization()
        {
            var settings = new SandboxSettings
            {
                Enabled = true,
                AutoAllowBashIfSandboxed = true,
                ExcludedCommands = new[] { "docker", "kubectl" },
                AllowUnsandboxedCommands = false,
                Network = new SandboxNetworkConfig
                {
                    AllowUnixSockets = new[] { "/var/run/docker.sock" },
                    AllowLocalBinding = true
                }
            };

            var json = JsonSerializer.SerializeToElement(settings);

            json.GetProperty("enabled").GetBoolean().Should().BeTrue();
            json.GetProperty("autoAllowBashIfSandboxed").GetBoolean().Should().BeTrue();
            json.GetProperty("excludedCommands")[0].GetString().Should().Be("docker");
            json.GetProperty("excludedCommands")[1].GetString().Should().Be("kubectl");
        }
    }
}
