namespace Reuniverse.Razor.Tests;

public abstract class ReuObjectTreeTestBase : BunitContext
{
    protected ReuObjectTreeTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected void AcceptConfirmDialogs()
    {
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
    }

    protected IRenderedComponent<ReuObjectTree> RenderObjectTree(object value, Func<object, IEnumerable<object?>>? childrenProvider = null)
    {
        return Render<ReuObjectTree>(parameters =>
        {
            parameters.Add(x => x.Value, value).Add(x => x.IsExpandedByDefault, true);

            if (childrenProvider is not null)
            {
                parameters.Add(x => x.ChildrenProvider, childrenProvider);
            }
        });
    }
}
