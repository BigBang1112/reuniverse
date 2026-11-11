namespace Reuniverse.Razor.Tests;

public abstract class ReuObjectTreeTestBase : BunitContext
{
    private bool? confirmAnswer;
    private bool hostRendered;

    protected ReuObjectTreeTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // answers right as it is asked, or leaves the question open when no answer was chosen
        Dialogs.Changed += () =>
        {
            if (confirmAnswer is bool answer)
            {
                Dialogs.Current?.Respond(answer);
            }
        };
    }

    /// <summary>
    /// What the trees ask through, with a host of its own, so that the tests can answer.
    /// </summary>
    protected ReuDialogService Dialogs { get; } = new();

    protected void AcceptConfirmDialogs()
    {
        confirmAnswer = true;
    }

    protected void DeclineConfirmDialogs()
    {
        confirmAnswer = false;
    }

    protected ReuDialogService DialogsWithHost()
    {
        if (!hostRendered)
        {
            hostRendered = true;
            Render<ReuDialogHost>(parameters => parameters.Add(x => x.Service, Dialogs));
        }

        return Dialogs;
    }

    protected IRenderedComponent<ReuObjectTree> RenderObjectTree(object value, Func<object, IEnumerable<object?>>? childrenProvider = null)
    {
        return Render<ReuObjectTree>(parameters =>
        {
            parameters.Add(x => x.Value, value).Add(x => x.IsExpandedByDefault, true).Add(x => x.Dialogs, DialogsWithHost());

            if (childrenProvider is not null)
            {
                parameters.Add(x => x.ChildrenProvider, childrenProvider);
            }
        });
    }
}
