namespace Reuniverse.Razor;

public sealed record FileInfoEventArgs(string FileName, long Size, DateTimeOffset LastModified, string Type);