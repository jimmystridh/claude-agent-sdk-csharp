using System.Collections.Concurrent;
using System.Text.Json;
using ih0.Claude.Agent.SDK.Types;
using FluentAssertions;
using Xunit;

namespace ih0.Claude.Agent.SDK.Tests;

/// <summary>
/// Tests for thread-safety and concurrent access patterns matching Rust SDK test_concurrency.rs
/// </summary>
public class ConcurrencyTests
{
    public class ThreadSafetyTests
    {
        [Fact]
        public async Task Options_IsThreadSafe()
        {
            var options = new ClaudeAgentOptions
            {
                Model = "test-model",
                MaxTurns = 5
            };

            var tasks = Enumerable.Range(0, 10).Select(i =>
                Task.Run(() =>
                {
                    options.Model.Should().Be("test-model");
                    options.MaxTurns.Should().Be(5);
                }));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task PermissionResult_IsThreadSafe()
        {
            var allow = new PermissionAllow();

            var tasks = Enumerable.Range(0, 100).Select(_ =>
                Task.Run(() =>
                {
                    allow.Behavior.Should().Be("allow");
                }));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task HookOutput_IsThreadSafe()
        {
            var output = new HookOutput
            {
                Continue = true,
                SuppressOutput = false
            };

            var tasks = Enumerable.Range(0, 100).Select(_ =>
                Task.Run(() =>
                {
                    output.Continue.Should().BeTrue();
                    output.SuppressOutput.Should().BeFalse();
                }));

            await Task.WhenAll(tasks);
        }
    }

    public class ConcurrentOptionsBuildingTests
    {
        [Fact]
        public async Task ConcurrentOptionsBuilding()
        {
            var tasks = Enumerable.Range(0, 10).Select(i =>
                Task.Run(() =>
                {
                    var options = new ClaudeAgentOptionsBuilder()
                        .WithModel($"model-{i}")
                        .WithMaxTurns(i + 1)
                        .WithSystemPrompt($"System prompt {i}")
                        .Build();

                    options.Model.Should().Be($"model-{i}");
                    options.MaxTurns.Should().Be(i + 1);
                }));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task SharedOptionsReading()
        {
            var options = new ClaudeAgentOptions
            {
                Model = "shared-model",
                MaxTurns = 5
            };

            var tasks = Enumerable.Range(0, 10).Select(_ =>
                Task.Run(() =>
                {
                    options.Model.Should().Be("shared-model");
                    options.MaxTurns.Should().Be(5);
                }));

            await Task.WhenAll(tasks);
        }
    }

    public class ConcurrentCallbackTests
    {
        [Fact]
        public async Task ConcurrentPermissionCallbackInvocations()
        {
            var callCount = 0;
            CanUseToolCallback callback = async (toolName, input, context) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Yield();
                return new PermissionAllow();
            };

            var tasks = Enumerable.Range(0, 100).Select(i =>
                Task.Run(async () =>
                {
                    var result = await callback($"Tool{i}", JsonDocument.Parse("{}").RootElement, new ToolPermissionContext());
                    result.Behavior.Should().Be("allow");
                }));

            await Task.WhenAll(tasks);
            callCount.Should().Be(100);
        }

        [Fact]
        public async Task ConcurrentHookCallbackInvocations()
        {
            var callCount = 0;
            HookCallback callback = async (input, toolUseId, context) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Yield();
                return new HookOutput();
            };

            var tasks = Enumerable.Range(0, 100).Select(_ =>
                Task.Run(async () =>
                {
                    var input = new PreToolUseHookInput
                    {
                        SessionId = "test-session",
                        TranscriptPath = "/tmp/test",
                        Cwd = "/",
                        ToolName = "TestTool",
                        ToolInput = JsonDocument.Parse("{}").RootElement
                    };
                    await callback(input, null, new HookContext());
                }));

            await Task.WhenAll(tasks);
            callCount.Should().Be(100);
        }
    }

    public class SharedStateCallbackTests
    {
        [Fact]
        public async Task PermissionCallbackWithSharedState()
        {
            var allowedTools = new HashSet<string> { "Read", "Write" };
            var lockObj = new object();

            CanUseToolCallback callback = async (toolName, input, context) =>
            {
                await Task.Yield();
                bool isAllowed;
                lock (lockObj)
                {
                    isAllowed = allowedTools.Contains(toolName);
                }
                return isAllowed ? new PermissionAllow() : new PermissionDeny { Message = "Not allowed" };
            };

            var tasks = Enumerable.Range(0, 50).Select(i =>
            {
                var tool = i % 2 == 0 ? "Read" : "Bash";
                return Task.Run(async () =>
                {
                    var result = await callback(tool, JsonDocument.Parse("{}").RootElement, new ToolPermissionContext());
                    return (tool, result);
                });
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (tool, result) in results)
            {
                if (tool == "Read")
                    result.Behavior.Should().Be("allow");
                else
                    result.Behavior.Should().Be("deny");
            }

            // Update shared state
            lock (lockObj)
            {
                allowedTools.Add("Bash");
            }

            // Now Bash should be allowed
            var newResult = await callback("Bash", JsonDocument.Parse("{}").RootElement, new ToolPermissionContext());
            newResult.Behavior.Should().Be("allow");
        }

        [Fact]
        public async Task HookCallbackWithSharedCounter()
        {
            var toolUsage = new ConcurrentDictionary<string, int>();

            HookCallback callback = async (input, toolUseId, context) =>
            {
                await Task.Yield();
                if (input is PreToolUseHookInput pre)
                {
                    toolUsage.AddOrUpdate(pre.ToolName, 1, (_, count) => count + 1);
                }
                return new HookOutput();
            };

            var tasks = Enumerable.Range(0, 100).Select(i =>
            {
                var tool = (i % 3) switch
                {
                    0 => "Read",
                    1 => "Write",
                    _ => "Bash"
                };
                return Task.Run(async () =>
                {
                    var input = new PreToolUseHookInput
                    {
                        SessionId = "test-session",
                        TranscriptPath = "/tmp/test",
                        Cwd = "/",
                        ToolName = tool,
                        ToolInput = JsonDocument.Parse("{}").RootElement
                    };
                    await callback(input, null, new HookContext());
                });
            });

            await Task.WhenAll(tasks);

            toolUsage.GetValueOrDefault("Read", 0).Should().Be(34);
            toolUsage.GetValueOrDefault("Write", 0).Should().Be(33);
            toolUsage.GetValueOrDefault("Bash", 0).Should().Be(33);
        }
    }

    public class ConcurrentMessageCreationTests
    {
        [Fact]
        public async Task ConcurrentMessageCreation()
        {
            var tasks = Enumerable.Range(0, 100).Select(i =>
                Task.Run(() =>
                {
                    var msg = new AssistantMessage
                    {
                        Content = new ContentBlock[]
                        {
                            new TextBlock { Text = $"Message {i}" }
                        },
                        Model = "claude-3"
                    };

                    msg.Content[0].Should().BeOfType<TextBlock>();
                    ((TextBlock)msg.Content[0]).Text.Should().Be($"Message {i}");
                }));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task SharedMessageReading()
        {
            var messages = Enumerable.Range(0, 10).Select(i =>
                new AssistantMessage
                {
                    Content = new ContentBlock[]
                    {
                        new TextBlock { Text = $"Message {i}" }
                    },
                    Model = "claude-3"
                }).ToArray();

            var tasks = Enumerable.Range(0, 100).Select(i =>
            {
                var msg = messages[i % 10];
                return Task.Run(() =>
                {
                    var textBlock = msg.Content[0] as TextBlock;
                    textBlock!.Text.Should().StartWith("Message ");
                });
            });

            await Task.WhenAll(tasks);
        }
    }

    public class ConcurrentResultMessageAccessTests
    {
        [Fact]
        public async Task ConcurrentResultMessageAccess()
        {
            var result = new ResultMessage
            {
                Subtype = "success",
                DurationMs = 1000,
                DurationApiMs = 800,
                IsError = false,
                NumTurns = 5,
                SessionId = "test-session",
                TotalCostUsd = 0.05
            };

            var tasks = Enumerable.Range(0, 100).Select(_ =>
                Task.Run(() =>
                {
                    result.DurationMs.Should().Be(1000);
                    result.NumTurns.Should().Be(5);
                    result.TotalCostUsd.Should().Be(0.05);
                    result.IsError.Should().BeFalse();
                }));

            await Task.WhenAll(tasks);
        }
    }

    public class HighConcurrencyStressTests
    {
        [Fact]
        public async Task HighConcurrencyPermissionCallbacks()
        {
            var callCount = 0;
            CanUseToolCallback callback = async (toolName, input, context) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Yield();
                return new PermissionAllow();
            };

            var tasks = Enumerable.Range(0, 1000).Select(i =>
                Task.Run(async () =>
                {
                    await callback($"Tool{i}", JsonDocument.Parse("{}").RootElement, new ToolPermissionContext());
                }));

            await Task.WhenAll(tasks);
            callCount.Should().Be(1000);
        }

        [Fact]
        public async Task HighConcurrencyMessageCreation()
        {
            var tasks = Enumerable.Range(0, 1000).Select(i =>
                Task.Run(() =>
                {
                    var msg = new AssistantMessage
                    {
                        Content = new ContentBlock[]
                        {
                            new TextBlock { Text = $"Message {i} part 1" },
                            new TextBlock { Text = $"Message {i} part 2" }
                        },
                        Model = "claude-3"
                    };

                    msg.Content.Should().HaveCount(2);
                    return msg;
                }));

            var messages = await Task.WhenAll(tasks);
            messages.Should().HaveCount(1000);
        }
    }

    public class ChannelBasedCommunicationTests
    {
        [Fact]
        public async Task ConcurrentMessageChannel()
        {
            var channel = System.Threading.Channels.Channel.CreateUnbounded<Message>();

            // Spawn producers
            var producerTasks = Enumerable.Range(0, 10).Select(i =>
                Task.Run(async () =>
                {
                    for (var j = 0; j < 10; j++)
                    {
                        var msg = new AssistantMessage
                        {
                            Content = new ContentBlock[]
                            {
                                new TextBlock { Text = $"Producer {i} Message {j}" }
                            },
                            Model = "claude-3"
                        };
                        await channel.Writer.WriteAsync(msg);
                    }
                }));

            // Consumer
            var consumerTask = Task.Run(async () =>
            {
                var count = 0;
                while (count < 100)
                {
                    if (await channel.Reader.WaitToReadAsync())
                    {
                        var msg = await channel.Reader.ReadAsync();
                        if (msg is AssistantMessage asst)
                        {
                            var textBlock = asst.Content[0] as TextBlock;
                            textBlock!.Text.Should().Contain("Producer");
                            count++;
                        }
                    }
                }
                return count;
            });

            await Task.WhenAll(producerTasks);
            channel.Writer.Complete();

            var totalCount = await consumerTask;
            totalCount.Should().Be(100);
        }
    }
}
