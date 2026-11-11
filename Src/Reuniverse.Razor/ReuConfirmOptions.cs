namespace Reuniverse.Razor;

/// <summary>
/// How <see cref="ReuDialogService.ConfirmAsync"/> asks.
/// </summary>
public sealed record ReuConfirmOptions
{
    /// <summary>
    /// The title of the dialog. Defaults to <c>Confirm</c>.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// The text of the button that confirms. Defaults to <c>OK</c>.
    /// </summary>
    public string ConfirmText { get; init; } = "OK";

    /// <summary>
    /// The text of the button that declines. Defaults to <c>Cancel</c>.
    /// </summary>
    public string CancelText { get; init; } = "Cancel";

    /// <summary>
    /// Whether confirming destroys something, such as removing or discarding it. The confirm button is then red,
    /// and the cancel button has the focus at first, so that pressing Enter does not destroy anything.
    /// </summary>
    public bool Destructive { get; init; }
}
