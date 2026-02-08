using System.Text.Json;
using ih0.Claude.Agent.SDK.Internal.Transport;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

public class ToolCallbackTests
{
    public class PermissionTypesTests
    {
        [Fact]
        public void PermissionAllow_Creation()
        {
            var allow = new PermissionAllow();

            allow.Behavior.Should().Be("allow");
        }

        [Fact]
        public void PermissionAllow_WithUpdatedInput()
        {
            var updatedInput = JsonDocument.Parse("""{"modified": true}""").RootElement;
            var allow = new PermissionAllow { UpdatedInput = updatedInput };

            allow.Behavior.Should().Be("allow");
            allow.UpdatedInput.Should().NotBeNull();
            allow.UpdatedInput!.Value.GetProperty("modified").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void PermissionDeny_WithMessage()
        {
            var deny = new PermissionDeny { Message = "Security policy violation" };

            deny.Behavior.Should().Be("deny");
            deny.Message.Should().Be("Security policy violation");
        }

        [Fact]
        public void ToolPermissionContext_Properties()
        {
            var context = new ToolPermissionContext
            {
                Suggestions = new List<PermissionUpdate>()
            };

            context.Suggestions.Should().BeEmpty();
        }
    }

    public class HookTypesTests
    {
        [Fact]
        public void HookEvent_AllValuesAreDefined()
        {
            Enum.GetValues<HookEvent>().Should().HaveCount(10);
            Enum.IsDefined(HookEvent.PreToolUse).Should().BeTrue();
            Enum.IsDefined(HookEvent.PostToolUse).Should().BeTrue();
            Enum.IsDefined(HookEvent.PostToolUseFailure).Should().BeTrue();
            Enum.IsDefined(HookEvent.UserPromptSubmit).Should().BeTrue();
            Enum.IsDefined(HookEvent.Stop).Should().BeTrue();
            Enum.IsDefined(HookEvent.SubagentStop).Should().BeTrue();
            Enum.IsDefined(HookEvent.PreCompact).Should().BeTrue();
            Enum.IsDefined(HookEvent.Notification).Should().BeTrue();
            Enum.IsDefined(HookEvent.SubagentStart).Should().BeTrue();
            Enum.IsDefined(HookEvent.PermissionRequest).Should().BeTrue();
        }

        [Fact]
        public void HookMatcher_Creation()
        {
            var matcher = new HookMatcher
            {
                Matcher = "tool:TestTool",
                Hooks = new List<HookCallback>()
            };

            matcher.Matcher.Should().Be("tool:TestTool");
            matcher.Hooks.Should().NotBeNull();
        }

        [Fact]
        public void HookOutput_Properties()
        {
            var output = new HookOutput
            {
                Continue = true,
                SuppressOutput = false,
                StopReason = "Test reason"
            };

            output.Continue.Should().BeTrue();
            output.SuppressOutput.Should().BeFalse();
            output.StopReason.Should().Be("Test reason");
        }
    }

    public class OptionsWithCallbacksTests
    {
        [Fact]
        public void Options_WithCanUseTool()
        {
            CanUseToolCallback myCallback = (toolName, input, context) =>
            {
                return Task.FromResult<PermissionResult>(new PermissionAllow());
            };

            var options = new ClaudeAgentOptions
            {
                CanUseTool = myCallback
            };

            options.CanUseTool.Should().Be(myCallback);
        }

        [Fact]
        public void Options_WithHooks()
        {
            HookCallback myHook = (input, toolUseId, context) =>
            {
                return Task.FromResult(new HookOutput());
            };

            var options = new ClaudeAgentOptions
            {
                Hooks = new Dictionary<HookEvent, IReadOnlyList<HookMatcher>>
                {
                    [HookEvent.PreToolUse] = new List<HookMatcher>
                    {
                        new HookMatcher
                        {
                            Matcher = "tool:Bash",
                            Hooks = new List<HookCallback> { myHook }
                        }
                    }
                }
            };

            options.Hooks.Should().ContainKey(HookEvent.PreToolUse);
            options.Hooks![HookEvent.PreToolUse].Should().HaveCount(1);
            options.Hooks[HookEvent.PreToolUse][0].Hooks.Should().Contain(myHook);
        }
    }

    public class MockTransportTests
    {
        [Fact]
        public void MockTransport_ImplementsITransport()
        {
            var transport = new TestMockTransport();

            transport.Should().BeAssignableTo<ITransport>();
        }

        [Fact]
        public async Task MockTransport_RecordsWrittenMessages()
        {
            var transport = new TestMockTransport();

            await transport.WriteAsync("test message");

            transport.WrittenMessages.Should().Contain("test message");
        }

        [Fact]
        public async Task MockTransport_ConnectDisconnect()
        {
            var transport = new TestMockTransport();

            transport.IsReady.Should().BeFalse();

            await transport.ConnectAsync();
            transport.IsReady.Should().BeTrue();

            await transport.CloseAsync();
            transport.IsReady.Should().BeFalse();
        }
    }

    private class TestMockTransport : ITransport
    {
        public List<string> WrittenMessages { get; } = new();
        public List<JsonElement> MessagesToRead { get; } = new();
        private bool _connected;

        public bool IsReady => _connected;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = true;
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            _connected = false;
            return Task.CompletedTask;
        }

        public Task WriteAsync(string data, CancellationToken cancellationToken = default)
        {
            WrittenMessages.Add(data);
            return Task.CompletedTask;
        }

        public Task EndInputAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<JsonElement> ReadMessagesAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var msg in MessagesToRead)
            {
                yield return msg;
                await Task.Yield();
            }
        }

        public ValueTask DisposeAsync()
        {
            _connected = false;
            return ValueTask.CompletedTask;
        }
    }
}
