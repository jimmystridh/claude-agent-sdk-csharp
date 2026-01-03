using System.Text.Json;
using ih0.Claude.Agent.SDK;
using ih0.Claude.Agent.SDK.Types;

Console.WriteLine("Starting Claude SDK Setting Sources Examples...");
Console.WriteLine(new string('=', 50));
Console.WriteLine();

var example = args.Length > 0 ? args[0] : null;

if (example == null)
{
    Console.WriteLine("Usage: dotnet run <example_name>");
    Console.WriteLine();
    Console.WriteLine("Available examples:");
    Console.WriteLine("  all - Run all examples");
    Console.WriteLine("  default");
    Console.WriteLine("  user_only");
    Console.WriteLine("  project_and_user");
    return;
}

var sdkDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

switch (example)
{
    case "all":
        await DefaultExample();
        Console.WriteLine(new string('-', 50) + "\n");
        await UserOnlyExample();
        Console.WriteLine(new string('-', 50) + "\n");
        await ProjectAndUserExample();
        break;
    case "default":
        await DefaultExample();
        break;
    case "user_only":
        await UserOnlyExample();
        break;
    case "project_and_user":
        await ProjectAndUserExample();
        break;
    default:
        Console.WriteLine($"Error: Unknown example '{example}'");
        return;
}

async Task DefaultExample()
{
    Console.WriteLine("=== Default Behavior Example ===");
    Console.WriteLine("Setting sources: None (default)");
    Console.WriteLine("Expected: No custom slash commands will be available\n");

    var options = new ClaudeAgentOptions
    {
        Cwd = sdkDir
    };

    await using var client = new ClaudeAgentClient(options);
    await client.ConnectAsync();
    await client.QueryAsync("What is 2 + 2?");

    await foreach (var msg in client.ReceiveResponseAsync())
    {
        if (msg is SystemMessage systemMsg && systemMsg.Subtype == "init")
        {
            var commands = ExtractSlashCommands(systemMsg.Data);
            Console.WriteLine($"Available slash commands: [{string.Join(", ", commands)}]");
            if (commands.Contains("commit"))
            {
                Console.WriteLine("X /commit is available (unexpected)");
            }
            else
            {
                Console.WriteLine("OK /commit is NOT available (expected - no settings loaded)");
            }
            break;
        }
    }
    Console.WriteLine();
}

async Task UserOnlyExample()
{
    Console.WriteLine("=== User Settings Only Example ===");
    Console.WriteLine("Setting sources: [\"user\"]");
    Console.WriteLine("Expected: Project slash commands (like /commit) will NOT be available\n");

    var options = new ClaudeAgentOptions
    {
        SettingSources = new[] { SettingSource.User },
        Cwd = sdkDir
    };

    await using var client = new ClaudeAgentClient(options);
    await client.ConnectAsync();
    await client.QueryAsync("What is 2 + 2?");

    await foreach (var msg in client.ReceiveResponseAsync())
    {
        if (msg is SystemMessage systemMsg && systemMsg.Subtype == "init")
        {
            var commands = ExtractSlashCommands(systemMsg.Data);
            Console.WriteLine($"Available slash commands: [{string.Join(", ", commands)}]");
            if (commands.Contains("commit"))
            {
                Console.WriteLine("X /commit is available (unexpected)");
            }
            else
            {
                Console.WriteLine("OK /commit is NOT available (expected)");
            }
            break;
        }
    }
    Console.WriteLine();
}

async Task ProjectAndUserExample()
{
    Console.WriteLine("=== Project + User Settings Example ===");
    Console.WriteLine("Setting sources: [\"user\", \"project\"]");
    Console.WriteLine("Expected: Project slash commands (like /commit) WILL be available\n");

    var options = new ClaudeAgentOptions
    {
        SettingSources = new[] { SettingSource.User, SettingSource.Project },
        Cwd = sdkDir
    };

    await using var client = new ClaudeAgentClient(options);
    await client.ConnectAsync();
    await client.QueryAsync("What is 2 + 2?");

    await foreach (var msg in client.ReceiveResponseAsync())
    {
        if (msg is SystemMessage systemMsg && systemMsg.Subtype == "init")
        {
            var commands = ExtractSlashCommands(systemMsg.Data);
            Console.WriteLine($"Available slash commands: [{string.Join(", ", commands)}]");
            if (commands.Contains("commit"))
            {
                Console.WriteLine("OK /commit is available (expected)");
            }
            else
            {
                Console.WriteLine("X /commit is NOT available (unexpected)");
            }
            break;
        }
    }
    Console.WriteLine();
}

static List<string> ExtractSlashCommands(JsonElement data)
{
    var result = new List<string>();
    if (data.TryGetProperty("slash_commands", out var commandsEl) && commandsEl.ValueKind == JsonValueKind.Array)
    {
        foreach (var cmd in commandsEl.EnumerateArray())
        {
            if (cmd.ValueKind == JsonValueKind.String)
            {
                result.Add(cmd.GetString() ?? "");
            }
        }
    }
    return result;
}
