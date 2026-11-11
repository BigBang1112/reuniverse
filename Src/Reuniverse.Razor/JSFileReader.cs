using Microsoft.JSInterop;

namespace Reuniverse.Razor;

/// <summary>
/// Reads ranges of a browser <c>File</c> through the object the upload script hands over, which slices the file on demand.
/// </summary>
/// <param name="file">Reference to the object with <c>read</c> and <c>slice</c> functions, created by the upload script.</param>
/// <param name="useStreamReference">
/// Whether the ranges are transferred as JS streams instead of byte arrays. Outside the browser (serverside interactivity),
/// byte arrays are limited by the SignalR message size, while streams are sent in chunks.
/// </param>
internal sealed class JSFileReader(IJSObjectReference file, bool useStreamReference) : IReuFileReader
{
    public async ValueTask<int> ReadAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }

        try
        {
            return useStreamReference
                ? await ReadStreamAsync(offset, buffer, cancellationToken)
                : await ReadBytesAsync(offset, buffer, cancellationToken);
        }
        catch (JSException ex)
        {
            // usually NotReadableError, when the file was changed or removed after it was selected
            throw new IOException($"The file could not be read: {ex.Message}", ex);
        }
    }

    private async Task<int> ReadBytesAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var data = await file.InvokeAsync<byte[]>("read", cancellationToken, offset, buffer.Length);

        // the client is not trusted to return what was asked for
        var count = Math.Min(data.Length, buffer.Length);
        data.AsSpan(0, count).CopyTo(buffer.Span);
        return count;
    }

    private async Task<int> ReadStreamAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        await using var streamReference = await file.InvokeAsync<IJSStreamReference>("slice", cancellationToken, offset, buffer.Length);

        // throws when the client sends more than was asked for
        await using var stream = await streamReference.OpenReadStreamAsync(buffer.Length, cancellationToken);

        var total = 0;
        int read;

        while (total < buffer.Length && (read = await stream.ReadAsync(buffer[total..], cancellationToken)) > 0)
        {
            total += read;
        }

        return total;
    }

    public ValueTask DisposeAsync() => file.DisposeAsync();
}
