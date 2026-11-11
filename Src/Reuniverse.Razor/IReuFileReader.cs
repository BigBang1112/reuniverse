namespace Reuniverse.Razor;

/// <summary>
/// Reads byte ranges of a file that stays on the client.
/// </summary>
internal interface IReuFileReader : IAsyncDisposable
{
    /// <summary>
    /// Reads up to <paramref name="buffer"/>.Length bytes starting at <paramref name="offset"/>. Returns 0 only when nothing is left to read.
    /// </summary>
    ValueTask<int> ReadAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken);
}
