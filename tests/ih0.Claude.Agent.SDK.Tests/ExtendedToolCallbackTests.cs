using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Extended tool callback tests matching Rust SDK test_tool_callbacks.rs
/// </summary>
public class ExtendedToolCallbackTests
{
    public class PermissionResultTests
    {
        [Fact]
        public void PermissionAllow_Creation()
        {
            var result = new PermissionAllow();

            result.Behavior.Should().Be("allow");
            result.UpdatedInput.Should().BeNull();
            result.UpdatedPermissions.Should().BeNull();
        }

        [Fact]
        public void PermissionDeny_Creation()
        {
            var result = new PermissionDeny { Message = "" };

            result.Behavior.Should().Be("deny");
            result.Message.Should().BeEmpty();
            result.Interrupt.Should().BeFalse();
        }

        [Fact]
        public void PermissionDeny_WithMessage()
        {
            var result = new PermissionDeny { Message = "Security policy violation" };

            result.Behavior.Should().Be("deny");
            result.Message.Should().Be("Security policy violation");
            result.Interrupt.Should().BeFalse();
        }

        [Fact]
        public void PermissionAllow_WithUpdatedInput()
        {
            var updatedInput = JsonDocument.Parse("""{"safe_mode": true, "param": "value"}""").RootElement;
            var result = new PermissionAllow { UpdatedInput = updatedInput };

            result.Behavior.Should().Be("allow");
            result.UpdatedInput.Should().NotBeNull();
            result.UpdatedInput!.Value.GetProperty("safe_mode").GetBoolean().Should().BeTrue();
            result.UpdatedInput!.Value.GetProperty("param").GetString().Should().Be("value");
        }

        [Fact]
        public void PermissionDeny_WithInterrupt()
        {
            var result = new PermissionDeny
            {
                Message = "Critical security violation",
                Interrupt = true
            };

            result.Behavior.Should().Be("deny");
            result.Message.Should().Be("Critical security violation");
            result.Interrupt.Should().BeTrue();
        }
    }

    public class ToolPermissionContextTests
    {
        [Fact]
        public void ToolPermissionContext_Default()
        {
            var context = new ToolPermissionContext();

            context.Suggestions.Should().BeEmpty();
        }

        [Fact]
        public void ToolPermissionContext_WithSuggestions()
        {
            var context = new ToolPermissionContext
            {
                Suggestions = new List<PermissionUpdate>
                {
                    new PermissionUpdate { Type = PermissionUpdateType.AddRules }
                }
            };

            context.Suggestions.Should().HaveCount(1);
        }
    }

    public class HookMatcherTests
    {
        [Fact]
        public void HookMatcher_Creation()
        {
            HookCallback callback = async (input, toolUseId, context) =>
            {
                await Task.Yield();
                return new HookOutput();
            };

            var matcher = new HookMatcher
            {
                Matcher = "Bash",
                Hooks = new List<HookCallback> { callback },
                Timeout = 30.0
            };

            matcher.Matcher.Should().Be("Bash");
            matcher.Hooks.Should().HaveCount(1);
            matcher.Timeout.Should().Be(30.0);
        }

        [Fact]
        public void HookMatcher_WithoutMatcher()
        {
            HookCallback callback = async (input, toolUseId, context) =>
            {
                await Task.Yield();
                return new HookOutput();
            };

            var matcher = new HookMatcher
            {
                Matcher = null, // Match all tools
                Hooks = new List<HookCallback> { callback },
                Timeout = null
            };

            matcher.Matcher.Should().BeNull();
            matcher.Hooks.Should().HaveCount(1);
            matcher.Timeout.Should().BeNull();
        }
    }

    public class HookOutputTests
    {
        [Fact]
        public void HookOutput_Default()
        {
            var output = new HookOutput();

            output.Continue.Should().BeNull();
            output.SuppressOutput.Should().BeNull();
            output.StopReason.Should().BeNull();
            output.Decision.Should().BeNull();
            output.SystemMessage.Should().BeNull();
            output.Reason.Should().BeNull();
            output.HookSpecificOutput.Should().BeNull();
        }

        [Fact]
        public void HookOutput_WithBlock()
        {
            var output = new HookOutput
            {
                Continue = false,
                Decision = "block",
                Reason = "Security policy violation"
            };

            output.Continue.Should().BeFalse();
            output.Decision.Should().Be("block");
            output.Reason.Should().Be("Security policy violation");
        }
    }

    public class AsyncHookOutputTests
    {
        [Fact]
        public void AsyncHookOutput_Creation()
        {
            var output = new AsyncHookOutput
            {
                Async = true,
                AsyncTimeout = 5000
            };

            output.Async.Should().BeTrue();
            output.AsyncTimeout.Should().Be(5000);
        }
    }

    public class OptionsWithHooksTests
    {
        [Fact]
        public void Options_WithHooks()
        {
            HookCallback callback = async (input, toolUseId, context) =>
            {
                await Task.Yield();
                return new HookOutput();
            };

            var hooks = new Dictionary<HookEvent, IReadOnlyList<HookMatcher>>
            {
                [HookEvent.PreToolUse] = new List<HookMatcher>
                {
                    new HookMatcher
                    {
                        Matcher = "Bash",
                        Hooks = new List<HookCallback> { callback }
                    }
                }
            };

            var options = new ClaudeAgentOptions { Hooks = hooks };

            options.Hooks.Should().NotBeNull();
            options.Hooks.Should().ContainKey(HookEvent.PreToolUse);
            options.Hooks![HookEvent.PreToolUse].Should().HaveCount(1);
        }

        [Fact]
        public void Options_WithCanUseTool()
        {
            CanUseToolCallback callback = async (toolName, input, context) =>
            {
                await Task.Yield();
                if (toolName == "Bash")
                    return new PermissionDeny { Message = "Bash not allowed" };
                return new PermissionAllow();
            };

            var options = new ClaudeAgentOptions { CanUseTool = callback };

            options.CanUseTool.Should().NotBeNull();
        }
    }

    public class HookEventTests
    {
        [Fact]
        public void HookEvent_AllVariants()
        {
            var events = new[]
            {
                HookEvent.PreToolUse,
                HookEvent.PostToolUse,
                HookEvent.UserPromptSubmit,
                HookEvent.Stop,
                HookEvent.SubagentStop,
                HookEvent.PreCompact
            };

            // Verify they can be used as dictionary keys
            var dict = new Dictionary<HookEvent, string>();
            foreach (var evt in events)
            {
                dict[evt] = evt.ToEventName();
            }

            dict.Should().HaveCount(6);
            dict.Should().ContainKey(HookEvent.PreToolUse);
            dict.Should().ContainKey(HookEvent.PostToolUse);
        }
    }

    public class HookContextTests
    {
        [Fact]
        public void HookContext_Default()
        {
            var context = new HookContext();
            context.Signal.Should().BeNull();
        }
    }

    public class PermissionResultSerializationTests
    {
        [Fact]
        public void PermissionAllow_Serialization()
        {
            var allow = new PermissionAllow();
            var json = JsonSerializer.Serialize(allow);
            json.Should().Contain("allow");
        }

        [Fact]
        public void PermissionDeny_WithMessage_Serialization()
        {
            var deny = new PermissionDeny { Message = "Not allowed" };
            var json = JsonSerializer.Serialize(deny);
            json.Should().Contain("deny");
            json.Should().Contain("Not allowed");
        }
    }

    public class HookOutputSerializationTests
    {
        [Fact]
        public void HookOutput_Serialization()
        {
            var output = new HookOutput
            {
                Continue = true,
                SuppressOutput = false,
                StopReason = "Test reason",
                Decision = "allow",
                SystemMessage = "Test message",
                Reason = "Test reason detail"
            };

            var json = JsonSerializer.Serialize(output);

            // Verify the serialized field name is "continue" not "continue_"
            json.Should().Contain("\"continue\"");
            json.Should().NotContain("\"continue_\"");
            json.Should().Contain("stopReason");
            json.Should().Contain("systemMessage");
        }
    }

    public class HookCallbackExecutionTests
    {
        [Fact]
        public async Task HookCallback_Execution()
        {
            var called = false;

            HookCallback callback = async (input, toolUseId, context) =>
            {
                called = true;
                await Task.Yield();

                input.Should().BeOfType<PreToolUseHookInput>();
                var pre = (PreToolUseHookInput)input;
                pre.ToolName.Should().Be("TestTool");

                return new HookOutput();
            };

            var hookInput = new PreToolUseHookInput
            {
                SessionId = "test-session",
                TranscriptPath = "/tmp/transcript",
                Cwd = "/test",
                ToolName = "TestTool",
                ToolInput = JsonDocument.Parse("""{"param": "value"}""").RootElement
            };

            var result = await callback(hookInput, null, new HookContext());

            called.Should().BeTrue();
            result.Should().NotBeNull();
        }
    }
}
