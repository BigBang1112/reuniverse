namespace Reuniverse.Razor;

/// <summary>
/// Asks the user to confirm something with a <see cref="ReuModal"/>, in place of the browser's own <c>confirm</c>.
/// A <see cref="ReuDialogHost"/> shows the questions one at a time, so render one where it can be seen, such as in the layout.
/// Register it with <see cref="ReuDialogServiceCollectionExtensions.AddReuDialogs"/>, or create one and pass it to the host.
/// </summary>
public sealed class ReuDialogService
{
    private readonly object sync = new();
    private readonly List<ReuDialogRequest> requests = [];
    private int hosts;

    /// <summary>
    /// Raised when a question is asked or answered, which is when <see cref="Current"/> can change.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The question being shown, or null when there is none. Later questions wait until it is answered.
    /// </summary>
    public ReuDialogRequest? Current
    {
        get
        {
            lock (sync)
            {
                return requests.Count > 0 ? requests[0] : null;
            }
        }
    }

    /// <summary>
    /// Whether a <see cref="ReuDialogHost"/> is rendered for this service, which is needed to ask anything.
    /// </summary>
    public bool HasHost => Volatile.Read(ref hosts) > 0;

    /// <summary>
    /// Asks the user to confirm, and completes with whether they did. Closing the dialog, such as with Escape, counts as declining.
    /// </summary>
    /// <param name="message">The question, such as <c>Discard changes?</c></param>
    /// <param name="options">The title and button texts, and whether confirming destroys something.</param>
    /// <param name="cancellationToken">Takes the question back, which cancels the returned task.</param>
    /// <exception cref="InvalidOperationException">No <see cref="ReuDialogHost"/> is rendered for this service.</exception>
    public Task<bool> ConfirmAsync(string message, ReuConfirmOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!HasHost)
        {
            throw new InvalidOperationException(
                $"No {nameof(ReuDialogHost)} is rendered for this {nameof(ReuDialogService)}, so nothing could show the question. " +
                $"Add <{nameof(ReuDialogHost)} /> to the layout, in an interactive render mode.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var request = new ReuDialogRequest(this, message, options ?? new ReuConfirmOptions());

        lock (sync)
        {
            requests.Add(request);
        }

        if (cancellationToken.CanBeCanceled)
        {
            request.CancelWith(cancellationToken);
        }

        Changed?.Invoke();
        return request.Task;
    }

    internal void Remove(ReuDialogRequest request)
    {
        bool removed;

        lock (sync)
        {
            removed = requests.Remove(request);
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    internal void AttachHost()
    {
        Interlocked.Increment(ref hosts);
    }

    // once nothing can show the questions anymore, they are declined rather than left waiting forever
    internal void DetachHost()
    {
        if (Interlocked.Decrement(ref hosts) > 0)
        {
            return;
        }

        ReuDialogRequest[] pending;

        lock (sync)
        {
            pending = requests.ToArray();
        }

        foreach (var request in pending)
        {
            request.Respond(false);
        }
    }
}
