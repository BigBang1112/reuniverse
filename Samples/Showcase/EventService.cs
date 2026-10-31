namespace Showcase;

public sealed class EventService
{
    public event Action<byte[]>? OnUpload;

    public void RaiseUpload(byte[] data)
    {
        OnUpload?.Invoke(data);
    }
}
