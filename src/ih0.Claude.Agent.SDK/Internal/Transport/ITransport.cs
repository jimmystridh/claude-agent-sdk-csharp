using System.Text.Json;

namespace ih0.Claude.Agent.SDK.Internal.Transport;

public interface ITransport : IAsyncDisposable
{
    bool IsReady { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<JsonElement> ReadMessagesAsync(CancellationToken cancellationToken = default);

    Task WriteAsync(string data, CancellationToken cancellationToken = default);

    Task EndInputAsync(CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);
}
