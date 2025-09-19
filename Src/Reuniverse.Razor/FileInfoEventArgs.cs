namespace Reuniverse.Razor;

public sealed record FileInfoEventArgs(string FileName, int Size, DateTimeOffset LastModified);