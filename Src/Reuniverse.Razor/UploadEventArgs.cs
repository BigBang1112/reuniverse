namespace Reuniverse.Razor;

public sealed record UploadEventArgs(string FileName, byte[] Data, DateTimeOffset LastModified);