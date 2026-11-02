using AngleSharp.Dom;

namespace Reuniverse.Razor.Tests;

public static class ReuObjectTreeTestExtensions
{
    public static void GoToFirstPage(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".page-btn")[0].Click();
    public static void GoToPreviousPage(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".page-btn")[1].Click();
    public static void GoToNextPage(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".page-btn")[2].Click();
    public static void GoToLastPage(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".page-btn")[3].Click();
    public static void Search(this IRenderedComponent<ReuObjectTree> cut, string term) => cut.Find("input[type=search]").Input(term);

    public static void ExpandFirstMember(this IRenderedComponent<ReuObjectTree> cut) => cut.Find(".items .expander").Click();

    public static void ExpandAllMembers(this IRenderedComponent<ReuObjectTree> cut)
    {
        foreach (var expander in cut.FindAll(".items .expander"))
        {
            expander.Click();
        }
    }

    public static void GoToPage(this IRenderedComponent<ReuObjectTree> cut, int page) => cut.Find(".page-goto input").Change(page.ToString());

    public static IElement MemberRow(this IRenderedComponent<ReuObjectTree> cut, string name)
    {
        return cut.FindAll(".item").First(item => item.QuerySelectorAll(".box.general").Any(box => box.TextContent == name));
    }

    public static void RemoveRow(this IRenderedComponent<ReuObjectTree> cut, int row) => cut.FindAll(".remove")[row].Click();
    public static void EditNumber(this IRenderedComponent<ReuObjectTree> cut, int row, string value) => cut.FindAll("input[type=number]")[row].Change(value);

    public static string PageInfo(this IRenderedComponent<ReuObjectTree> cut) => cut.Find(".page-info").TextContent;
    public static int ElementRowCount(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll("code.box.general").Count;
    public static int RemoveButtonCount(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll(".remove").Count;
    public static int NumberInputCount(this IRenderedComponent<ReuObjectTree> cut) => cut.FindAll("input[type=number]").Count;
    public static string? NumberValue(this IRenderedComponent<ReuObjectTree> cut, int row) => cut.FindAll("input[type=number]")[row].GetAttribute("value");
    public static bool HasInputValue(this IRenderedComponent<ReuObjectTree> cut, string value) => cut.FindAll("input").Any(x => x.GetAttribute("value") == value);

    public static async Task ShouldShowElementAsync(this IRenderedComponent<ReuObjectTree> cut, int index)
    {
        await Assert.That(cut.Markup).Contains($"#{index}<");
    }

    public static async Task ShouldNotShowElementAsync(this IRenderedComponent<ReuObjectTree> cut, int index)
    {
        await Assert.That(cut.Markup).DoesNotContain($"#{index}<");
    }
}
