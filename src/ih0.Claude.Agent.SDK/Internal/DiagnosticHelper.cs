using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ih0.Claude.Agent.SDK.Internal;

internal static class DiagnosticHelper
{
    [Conditional("DEBUG")]
    public static void LogIgnoredException(Exception ex, [CallerMemberName] string? caller = null)
    {
        Debug.WriteLine($"[ClaudeAgentSdk:{caller}] Ignored {ex.GetType().Name}: {ex.Message}");
    }
}
