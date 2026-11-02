using System.Collections;

namespace Reuniverse.Razor;

/// <summary>
/// Enumerates the source at most once, buffering elements as they are requested. Repeated enumerations
/// reuse the buffer, and partial enumerations (like <c>Any()</c> or <c>Take()</c>) only pull what they need.
/// </summary>
internal sealed class BufferedEnumerable<T>(IEnumerable<T> source) : IEnumerable<T>, IDisposable
{
    private readonly List<T> buffer = [];
    private IEnumerator<T>? sourceEnumerator;
    private bool completed;

    public IEnumerator<T> GetEnumerator()
    {
        // index based, so multiple enumerators can interleave over the shared buffer
        for (var i = 0; i < buffer.Count || TryBufferNext(); i++)
        {
            yield return buffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool TryBufferNext()
    {
        if (completed)
        {
            return false;
        }

        sourceEnumerator ??= source.GetEnumerator();

        if (sourceEnumerator.MoveNext())
        {
            buffer.Add(sourceEnumerator.Current);
            return true;
        }

        Dispose();
        return false;
    }

    /// <summary>
    /// Releases the source enumerator. Already buffered elements stay available, but no more are pulled from the source.
    /// </summary>
    public void Dispose()
    {
        completed = true;
        sourceEnumerator?.Dispose();
        sourceEnumerator = null;
    }
}
