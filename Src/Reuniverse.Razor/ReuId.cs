namespace Reuniverse.Razor;

/// <summary>
/// Makes element IDs that are unique within the app, for ARIA attributes that link one element to another.
/// </summary>
internal static class ReuId
{
    private static long next;

    public static string Next(string prefix) => $"{prefix}-{Interlocked.Increment(ref next)}";
}
