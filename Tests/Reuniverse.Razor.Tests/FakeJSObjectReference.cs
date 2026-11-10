using Microsoft.JSInterop;

namespace Reuniverse.Razor.Tests;

/// <summary>
/// Stands in for the object the upload script creates for a file, answering calls with <see cref="Handler"/>.
/// </summary>
public sealed class FakeJSObjectReference : IJSObjectReference
{
    public Func<string, object?[], object?> Handler { get; set; } = (identifier, _) => throw new InvalidOperationException($"Unexpected call to {identifier}.");

    public List<string> Calls { get; } = [];

    public bool Disposed { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add(identifier);
        return ValueTask.FromResult((TValue)Handler(identifier, args ?? [])!);
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Stands in for a JS Blob received as a stream.
/// </summary>
public sealed class FakeJSStreamReference(byte[] data) : IJSStreamReference
{
    public long Length => data.Length;

    public long? MaxAllowedSize { get; private set; }

    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        MaxAllowedSize = maxAllowedSize;

        if (data.Length > maxAllowedSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAllowedSize));
        }

        return ValueTask.FromResult<Stream>(new MemoryStream(data));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
