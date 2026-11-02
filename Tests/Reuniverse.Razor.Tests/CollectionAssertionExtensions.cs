using TUnit.Assertions.Enums;

namespace Reuniverse.Razor.Tests;

public static class CollectionAssertionExtensions
{
    // IsEquivalentTo ignores the order by default, but the tests depend on which element is at which index
    public static async Task ShouldEqualInOrderAsync<T>(this IEnumerable<T> actual, params T[] expected)
    {
        await Assert.That(actual.ToList()).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }
}
