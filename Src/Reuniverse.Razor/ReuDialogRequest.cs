namespace Reuniverse.Razor;

/// <summary>
/// A question asked through <see cref="ReuDialogService"/>, waiting for an answer.
/// </summary>
public sealed class ReuDialogRequest
{
    private readonly ReuDialogService service;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenRegistration cancellation;

    internal ReuDialogRequest(ReuDialogService service, string message, ReuConfirmOptions options)
    {
        this.service = service;
        Message = message;
        Options = options;
    }

    /// <summary>
    /// The question.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The title and button texts.
    /// </summary>
    public ReuConfirmOptions Options { get; }

    internal Task<bool> Task => completion.Task;

    /// <summary>
    /// Answers the question, which shows the next one, if any. Later answers are ignored.
    /// </summary>
    public void Respond(bool confirmed)
    {
        if (completion.TrySetResult(confirmed))
        {
            cancellation.Dispose();
            service.Remove(this);
        }
    }

    internal void CancelWith(CancellationToken cancellationToken)
    {
        cancellation = cancellationToken.Register(() =>
        {
            if (completion.TrySetCanceled(cancellationToken))
            {
                service.Remove(this);
            }
        });
    }
}
