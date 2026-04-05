namespace Reuniverse.Razor;

internal static class ByteHelper
{
    public static string ToByteSize(int size)
    {
        if (size < 1024)
        {
            return $"{size} B";
        }

        if (size < 1024 * 1024)
        {
            return $"{size / 1024.0:F2} kB";
        }

        return $"{size / (1024.0 * 1024.0):F2} MB";
    }
}
