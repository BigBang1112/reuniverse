namespace Reuniverse.Razor.Tests;

public class BufferedEnumerableTests
{
    private class TrackedSource
    {
        public int Pulled { get; private set; }
        public int Enumerations { get; private set; }
        public bool Disposed { get; private set; }

        public IEnumerable<int> Enumerate(int count)
        {
            Enumerations++;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    Pulled++;
                    yield return i;
                }
            }
            finally
            {
                Disposed = true;
            }
        }
    }

    [Test]
    public async Task GetEnumerator_CalledTwice_EnumeratesSourceOnce()
    {
        // Arrange
        var source = new TrackedSource();
        using var buffered = new BufferedEnumerable<int>(source.Enumerate(3));

        // Act
        _ = buffered.ToList();
        var second = buffered.ToList();

        // Assert
        await Assert.That(source.Enumerations).IsEqualTo(1);
        await second.ShouldEqualInOrderAsync(0, 1, 2);
    }

    [Test]
    public async Task Any_WithLazySource_PullsOnlyFirstElement()
    {
        // Arrange
        var source = new TrackedSource();
        using var buffered = new BufferedEnumerable<int>(source.Enumerate(1000));

        // Act
        _ = buffered.Any();

        // Assert
        await Assert.That(source.Pulled).IsEqualTo(1);
    }

    [Test]
    public async Task GetEnumerator_AfterPartialEnumeration_ContinuesFromBuffer()
    {
        // Arrange
        var source = new TrackedSource();
        using var buffered = new BufferedEnumerable<int>(source.Enumerate(3));
        _ = buffered.Take(1).ToList();

        // Act
        var all = buffered.ToList();

        // Assert
        await all.ShouldEqualInOrderAsync(0, 1, 2);
    }

    [Test]
    public async Task GetEnumerator_WithInterleavedEnumerators_YieldsAllElementsToBoth()
    {
        // Arrange
        using var buffered = new BufferedEnumerable<int>(Enumerable.Range(0, 3));
        var first = new List<int>();
        var second = new List<int>();
        using var firstEnumerator = buffered.GetEnumerator();
        using var secondEnumerator = buffered.GetEnumerator();

        // Act
        while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
        {
            first.Add(firstEnumerator.Current);
            second.Add(secondEnumerator.Current);
        }

        // Assert
        await first.ShouldEqualInOrderAsync(0, 1, 2);
        await second.ShouldEqualInOrderAsync(0, 1, 2);
    }

    [Test]
    public async Task Dispose_AfterPartialEnumeration_KeepsBufferedElements()
    {
        // Arrange
        var buffered = new BufferedEnumerable<int>(Enumerable.Range(0, 3));
        _ = buffered.Take(2).ToList();

        // Act
        buffered.Dispose();

        // Assert
        await buffered.ShouldEqualInOrderAsync(0, 1);
    }

    [Test]
    public async Task Dispose_AfterPartialEnumeration_DisposesSource()
    {
        // Arrange
        var source = new TrackedSource();
        var buffered = new BufferedEnumerable<int>(source.Enumerate(3));
        _ = buffered.Any();

        // Act
        buffered.Dispose();

        // Assert
        await Assert.That(source.Disposed).IsTrue();
    }

    [Test]
    public async Task GetEnumerator_WithExhaustedSource_DisposesSource()
    {
        // Arrange
        var source = new TrackedSource();
        using var buffered = new BufferedEnumerable<int>(source.Enumerate(1));

        // Act
        _ = buffered.ToList();

        // Assert
        await Assert.That(source.Disposed).IsTrue();
    }
}
