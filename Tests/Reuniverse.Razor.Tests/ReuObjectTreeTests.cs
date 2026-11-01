using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Rendering;
using Reuniverse.Razor.TypeValueComponents;

namespace Reuniverse.Razor.Tests;

public class ReuObjectTreeTests : ReuObjectTreeTestBase
{
    private const int LargeCount = 1010;
    private const int LastLargeElement = LargeCount - 1;

    private class Person
    {
        public string Name { get; set; } = "Ann";
        public int Age { get; set; } = 30;
        public Person? Friend { get; set; }
    }

    private sealed class SameId
    {
        public int Id { get; init; }
        public SameId? Nested { get; set; }

        public override bool Equals(object? obj) => obj is SameId other && other.Id == Id;
        public override int GetHashCode() => Id;
    }

    public class ReplacementValueComponent : TypeValueComponentBase<int?>
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "replacement");
    }

    private class HasSpan
    {
        public Span<byte> Data => new byte[1];
        public int Number => 7;
    }

    private class HasMemory
    {
        public Memory<byte> Memory { get; } = new byte[4];
    }

    private class HasLazyEnumerables
    {
        public IEnumerable<int> FailsAtStart { get; } = Fail(after: 0);
        public IEnumerable<int> FailsAfterTwo { get; } = Fail(after: 2);

        private static IEnumerable<int> Fail(int after)
        {
            for (var i = 0; i < after; i++)
            {
                yield return i;
            }

            throw new InvalidOperationException("enumerable failed");
        }
    }

    private class ListWithMember : List<int>
    {
        public ListWithMember(IEnumerable<int> elements) : base(elements)
        {
        }

        public string Extra => "extra-member";
    }

    private static List<int> CreateList(int count) => Enumerable.Range(0, count).ToList();
    private static HashSet<int> CreateHashSet(int count) => new(Enumerable.Range(0, count));
    private static ListWithMember CreateListWithMember() => new(Enumerable.Range(0, LargeCount));

    private static object CreateCollectionWithMembers(string type) => type switch
    {
        "HashSet" => CreateHashSet(LargeCount),
        "Queue" => new Queue<int>(Enumerable.Range(0, LargeCount)),
        "ListWithMember" => CreateListWithMember(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static ConcurrentDictionary<int, int> CreateConcurrentDictionary()
    {
        return new ConcurrentDictionary<int, int>(Enumerable.Range(0, LargeCount).Select(i => KeyValuePair.Create(i, i)));
    }

    [Test]
    public async Task Render_WithNullValue_ShowsNull()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters.Add(x => x.Value, null));

        // Assert
        await Assert.That(cut.Find(".box-value").TextContent).IsEqualTo("null");
    }

    [Test]
    public async Task Render_WithDisplayName_ShowsDisplayName()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.DisplayName, "Display name"));

        // Assert
        await Assert.That(cut.Find(".box.general").TextContent).IsEqualTo("Display name");
    }

    [Test]
    public async Task Render_WithHideType_HidesTypes()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.HideType, true)
            .Add(x => x.IsExpandedByDefault, true));

        // Assert
        await Assert.That(cut.FindAll(".box.type").Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClickType_WithCallback_ReportsType()
    {
        // Arrange
        Type? clicked = null;
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.OnTypeClick, type => clicked = type));

        // Act
        cut.FindAll(".box.type .type").Last().Click(); // the nested type is listed after its declaring type

        // Assert
        await Assert.That(clicked).IsEqualTo(typeof(Person));
    }

    [Test]
    public async Task ToggleExpanded_WhenExpanded_HidesMembers()
    {
        // Arrange
        var cut = RenderObjectTree(new Person());

        // Act
        cut.Find(".item.root .expander").Click();

        // Assert
        await Assert.That(cut.FindAll(".items").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutMembers_HidesExpander()
    {
        // Act
        var cut = RenderObjectTree(new object());

        // Assert
        await Assert.That(cut.FindAll(".expander").Count).IsEqualTo(0);
    }

    [Test]
    public async Task ClickValue_WithCallback_SelectsValue()
    {
        // Arrange
        object? selected = null;
        var person = new Person();
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, person)
            .Add(x => x.SelectedObjectChanged, value => selected = value));

        // Act
        cut.Find(".box.general").Click();

        // Assert
        await Assert.That(selected).IsSameReferenceAs(person);
    }

    [Test]
    public async Task ClickValue_WhenSelected_ClearsSelection()
    {
        // Arrange
        var cut = RenderObjectTree(new Person());
        cut.Find(".box.general").Click();

        // Act
        cut.Find(".box.general").Click();

        // Assert
        await Assert.That(cut.Instance.SelectedObject).IsNull();
    }

    [Test]
    public async Task ClickMember_InNestedTree_SelectsOnRootTree()
    {
        // Arrange
        object? selected = null;
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.SelectedObjectChanged, value => selected = value));

        // Act
        cut.MemberRow("Name").QuerySelector(".box.general")!.Click();

        // Assert
        await Assert.That(selected).IsTypeOf<ReuObjectTreeMember>();
    }

    [Test]
    public async Task ToggleExpanded_WithCycle_ShowsError()
    {
        // Arrange
        var person = new Person();
        person.Friend = person;
        var cut = RenderObjectTree(person);

        // Act
        cut.MemberRow("Friend").QuerySelector(".expander")!.Click();

        // Assert
        await Assert.That(cut.Markup).Contains("Detected cyclic reference");
    }

    [Test]
    [Skip("Known bug: HasCyclicReference uses value equality, so an equal but distinct object is reported as a cycle")]
    public async Task ToggleExpanded_WithEqualDistinctObjects_DoesNotReportCycle()
    {
        // Arrange
        var cut = RenderObjectTree(new SameId { Id = 1, Nested = new SameId { Id = 1 } });

        // Act
        cut.MemberRow("Nested").QuerySelector(".expander")!.Click();

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Detected cyclic reference");
    }

    [Test]
    public async Task Render_WithIgnoreProvider_HidesIgnoredMembers()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.IgnoreProvider, value => value is ReuObjectTreeMember { Property.Name: nameof(Person.Age) }));

        // Assert
        await Assert.That(cut.Markup).DoesNotContain(">Age<");
    }

    [Test]
    public async Task Render_WithThrowingIgnoreProvider_ShowsMembers()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.IgnoreProvider, _ => throw new InvalidOperationException()));

        // Assert
        await Assert.That(cut.Markup).Contains(">Age<");
    }

    [Test]
    public async Task Render_WithFadeProvider_FadesMatchingMembers()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.FadeProvider, value => value is ReuObjectTreeMember { Property.Name: nameof(Person.Age) }));

        // Assert
        await Assert.That(cut.MemberRow("Age").ClassList.Contains("faded")).IsTrue();
        await Assert.That(cut.MemberRow("Name").ClassList.Contains("faded")).IsFalse();
    }

    [Test]
    public async Task Render_WithThrowingFadeProvider_DoesNotFade()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.FadeProvider, _ => throw new InvalidOperationException()));

        // Assert
        await Assert.That(cut.MemberRow("Age").ClassList.Contains("faded")).IsFalse();
    }

    [Test]
    public async Task Render_WithAdditionalTypeValueComponents_UsesReplacement()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.AdditionalTypeValueComponents, new Dictionary<Type, Type> { [typeof(int)] = typeof(ReplacementValueComponent) }));

        // Assert
        await Assert.That(cut.MemberRow("Age").TextContent).Contains("replacement");
    }

    [Test]
    public async Task Render_WithAdditionalTypeValueComponents_KeepsDefaults()
    {
        // Act
        var cut = Render<ReuObjectTree>(parameters => parameters
            .Add(x => x.Value, new Person())
            .Add(x => x.IsExpandedByDefault, true)
            .Add(x => x.AdditionalTypeValueComponents, new Dictionary<Type, Type> { [typeof(int)] = typeof(ReplacementValueComponent) }));

        // Assert
        await Assert.That(cut.MemberRow("Name").QuerySelector("input")!.GetAttribute("value")).IsEqualTo("Ann");
    }

    [Test]
    public async Task Update_AfterExternalChange_ShowsNewValue()
    {
        // Arrange
        var person = new Person();
        var cut = RenderObjectTree(person);
        person.Age = 99;

        // Act
        await cut.InvokeAsync(cut.Instance.Update);

        // Assert
        await Assert.That(cut.HasInputValue("99")).IsTrue();
    }

    [Test]
    public async Task UpdateAll_FromNestedTree_UpdatesRootTree()
    {
        // Arrange
        var person = new Person();
        var cut = RenderObjectTree(person);
        var nested = cut.FindComponents<ReuObjectTree>().Last();
        person.Age = 99;

        // Act
        await cut.InvokeAsync(nested.Instance.UpdateAll);

        // Assert
        await Assert.That(cut.HasInputValue("99")).IsTrue();
    }

    [Test]
    public async Task GoToPage_WithValidPage_ShowsPage()
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(25));

        // Act
        cut.GoToPage(2);

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("2/3");
    }

    [Test]
    [Arguments(0, "1/3")]
    [Arguments(99, "3/3")]
    public async Task GoToPage_OutOfRange_ClampsPage(int page, string expectedPageInfo)
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(25));

        // Act
        cut.GoToPage(page);

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo(expectedPageInfo);
    }

    [Test]
    public async Task GoToFirstPage_FromLastPage_ShowsFirstPage()
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(25));
        cut.GoToLastPage();

        // Act
        cut.GoToFirstPage();

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("1/3");
    }

    [Test]
    public async Task Search_WithoutMatches_ShowsNoElements()
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(25));

        // Act
        cut.Search("no match");

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("0/0");
    }

    [Test]
    public async Task Search_WhenCleared_ShowsAllElements()
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(25));
        cut.Search("24");

        // Act
        cut.Search("");

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("1/3");
    }

    [Test]
    public async Task Render_WithLazyEnumerable_ShowsUnknownPageCount()
    {
        // Act
        var cut = RenderObjectTree(EnumerateNumbers(25));

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("1/?");
    }

    [Test]
    public async Task GoToNextPage_WithLazyEnumerable_ShowsNextElements()
    {
        // Arrange
        var cut = RenderObjectTree(EnumerateNumbers(25));

        // Act
        cut.GoToNextPage();

        // Assert
        await cut.ShouldShowElementAsync(10);
    }

    [Test]
    public async Task GoToNextPage_ToLastLazyPage_DisablesNextPage()
    {
        // Arrange
        var cut = RenderObjectTree(EnumerateNumbers(15));

        // Act
        cut.GoToNextPage();

        // Assert
        await Assert.That(cut.FindAll(".page-btn")[2].HasAttribute("disabled")).IsTrue();
    }

    [Test]
    public async Task Render_WithInfiniteEnumerable_ShowsFirstPage()
    {
        // Act
        var cut = RenderObjectTree(EnumerateNumbers(int.MaxValue));

        // Assert
        await Assert.That(cut.ElementRowCount()).IsEqualTo(10);
    }

    [Test]
    public async Task Render_WithShortLazyEnumerable_HidesPaging()
    {
        // Act
        var cut = RenderObjectTree(EnumerateNumbers(5));

        // Assert
        await Assert.That(cut.FindAll(".page-info").Count).IsEqualTo(0);
    }

    private static IEnumerable<int> EnumerateNumbers(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    [Test]
    public async Task Render_WithSpanProperty_SkipsProperty()
    {
        // Act
        var cut = RenderObjectTree(new HasSpan());

        // Assert
        await Assert.That(cut.Markup).DoesNotContain(">Data<");
    }

    [Test]
    public async Task Render_WithSpanProperty_ShowsOtherProperties()
    {
        // Act
        var cut = RenderObjectTree(new HasSpan());

        // Assert
        await Assert.That(cut.Markup).Contains("Number");
    }

    [Test]
    public async Task ToggleExpanded_WithMemoryMember_ShowsMembers()
    {
        // Arrange
        var cut = RenderObjectTree(new HasMemory());

        // Act
        cut.ExpandFirstMember();

        // Assert
        await Assert.That(cut.Markup).Contains("Length");
    }

    [Test]
    public async Task Render_WithEnumerableFailingAtStart_ShowsError()
    {
        // Act
        var cut = RenderObjectTree(new HasLazyEnumerables());

        // Assert
        await Assert.That(cut.Markup).Contains("enumerable failed");
    }

    [Test]
    public async Task ToggleExpanded_WithEnumerableFailingLater_ShowsLoadedElements()
    {
        // Arrange
        var cut = RenderObjectTree(new HasLazyEnumerables());

        // Act
        cut.ExpandAllMembers();

        // Assert
        await cut.ShouldShowElementAsync(1);
    }

    [Test]
    public async Task ToggleExpanded_WithEnumerableFailingLater_ShowsError()
    {
        // Arrange
        var cut = RenderObjectTree(new HasLazyEnumerables());

        // Act
        cut.ExpandAllMembers();

        // Assert
        await Assert.That(cut.Markup).Contains("enumerable failed");
    }

    [Test]
    public async Task Render_WithThrowingChildrenProvider_ShowsError()
    {
        // Act
        var cut = RenderObjectTree(new object(), _ => throw new InvalidOperationException("provider failed"));

        // Assert
        await Assert.That(cut.Markup).Contains("provider failed");
    }

    [Test]
    public async Task Update_AfterChildrenProviderRecovers_ClearsError()
    {
        // Arrange
        var fail = true;
        var cut = RenderObjectTree(new object(), _ => fail ? throw new InvalidOperationException("provider failed") : [1]);
        fail = false;

        // Act
        await cut.InvokeAsync(cut.Instance.Update);

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("provider failed");
    }

    [Test]
    public async Task GoToNextPage_AfterExternalChange_ShowsNewValue()
    {
        // Arrange
        var list = CreateList(25);
        var cut = RenderObjectTree(list);
        list[15] = 999;

        // Act
        cut.GoToNextPage();

        // Assert
        await Assert.That(cut.HasInputValue("999")).IsTrue();
    }

    [Test]
    public async Task GoToPreviousPage_AfterElementsRemoved_HidesRemovedElements()
    {
        // Arrange
        var list = CreateList(25);
        var cut = RenderObjectTree(list);
        cut.GoToLastPage();
        list.RemoveRange(5, 20);

        // Act
        cut.GoToPreviousPage();

        // Assert
        await cut.ShouldShowElementAsync(4);
        await cut.ShouldNotShowElementAsync(5);
    }

    [Test]
    public async Task Search_AfterExternalChange_FindsNewValue()
    {
        // Arrange
        var list = CreateList(25);
        var cut = RenderObjectTree(list);
        list[20] = 777;

        // Act
        cut.Search("777");

        // Assert
        await Assert.That(cut.HasInputValue("777")).IsTrue();
    }

    [Test]
    [Arguments(1001)]
    [Arguments(1010)]
    [Arguments(1013)]
    [Arguments(2000)]
    public async Task GoToLastPage_WithLargeHashSet_ShowsLastElement(int count)
    {
        // Arrange
        var cut = RenderObjectTree(CreateHashSet(count));

        // Act
        cut.GoToLastPage();

        // Assert
        await cut.ShouldShowElementAsync(count - 1);
    }

    [Test]
    [Arguments(1001, "101/101")]
    [Arguments(1010, "101/101")]
    [Arguments(1013, "102/102")]
    [Arguments(2000, "200/200")]
    public async Task GoToLastPage_WithLargeHashSet_ShowsLastPageNumber(int count, string expectedPageInfo)
    {
        // Arrange
        var cut = RenderObjectTree(CreateHashSet(count));

        // Act
        cut.GoToLastPage();

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo(expectedPageInfo);
    }

    [Test]
    public async Task GoToLastPage_WithLargeHashSet_ShowsFullPage()
    {
        // Arrange
        var cut = RenderObjectTree(CreateHashSet(LargeCount));

        // Act
        cut.GoToLastPage();

        // Assert
        await Assert.That(cut.ElementRowCount()).IsEqualTo(10);
    }

    [Test]
    [Arguments("HashSet")]
    [Arguments("Queue")]
    [Arguments("ListWithMember")]
    public async Task GoToLastPage_WithMembersAlongsideElements_ShowsLastElement(string type)
    {
        // Arrange
        var cut = RenderObjectTree(CreateCollectionWithMembers(type));

        // Act
        cut.GoToLastPage();

        // Assert
        await cut.ShouldShowElementAsync(LastLargeElement);
    }

    [Test]
    public async Task Render_WithLargeConcurrentDictionary_ShowsFullPage()
    {
        // Act
        var cut = RenderObjectTree(CreateConcurrentDictionary());

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(10);
    }

    [Test]
    public async Task GoToLastPage_WithLargeConcurrentDictionary_ShowsFullPage()
    {
        // Arrange
        var cut = RenderObjectTree(CreateConcurrentDictionary());

        // Act
        cut.GoToLastPage();

        // Assert
        await Assert.That(cut.RemoveButtonCount()).IsEqualTo(10);
    }

    [Test]
    public async Task Render_WithLargeCollection_ShowsMembers()
    {
        // Act
        var cut = RenderObjectTree(CreateListWithMember());

        // Assert
        await Assert.That(cut.Markup).Contains("Extra");
    }

    [Test]
    public async Task Render_WithLargeCollection_LeavesMembersOutOfPage()
    {
        // Act
        var cut = RenderObjectTree(CreateListWithMember());

        // Assert
        await Assert.That(cut.ElementRowCount()).IsEqualTo(10);
    }

    [Test]
    public async Task GoToLastPage_WithLargeCollection_KeepsMembersVisible()
    {
        // Arrange
        var cut = RenderObjectTree(CreateListWithMember());

        // Act
        cut.GoToLastPage();

        // Assert
        await Assert.That(cut.Markup).Contains("Extra");
    }

    [Test]
    public async Task Search_InLargeCollection_IgnoresMembers()
    {
        // Arrange
        var cut = RenderObjectTree(CreateListWithMember());

        // Act
        cut.Search("extra");

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("0/0");
    }

    [Test]
    public async Task Search_InLargeCollection_FindsOnlyMatchingElement()
    {
        // Arrange
        var cut = RenderObjectTree(CreateHashSet(LargeCount));

        // Act
        cut.Search(LastLargeElement.ToString());

        // Assert
        await Assert.That(cut.ElementRowCount()).IsEqualTo(1);
        await cut.ShouldShowElementAsync(LastLargeElement);
    }

    [Test]
    public async Task Render_WithLargeList_ShowsPageCount()
    {
        // Act
        var cut = RenderObjectTree(CreateList(LargeCount));

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("1/101");
    }

    [Test]
    public async Task GoToLastPage_WithLargeList_ShowsLastElement()
    {
        // Arrange
        var cut = RenderObjectTree(CreateList(LargeCount));

        // Act
        cut.GoToLastPage();

        // Assert
        await cut.ShouldShowElementAsync(LastLargeElement);
    }

    [Test]
    public async Task Render_WithSmallCollection_ShowsPageCount()
    {
        // Act
        var cut = RenderObjectTree(CreateHashSet(25));

        // Assert
        await Assert.That(cut.PageInfo()).IsEqualTo("1/3");
    }

    [Test]
    public async Task GoToLastPage_WithSmallCollection_ShowsLastElement()
    {
        // Arrange
        var cut = RenderObjectTree(CreateHashSet(25));

        // Act
        cut.GoToLastPage();

        // Assert
        await cut.ShouldShowElementAsync(24);
    }

    [Test]
    public async Task Render_WithLargeCollection_CreatesOnlyVisibleElements()
    {
        // Arrange
        var list = CreateList(100_000);
        var created = 0;

        IEnumerable<object?> ProvideChildren(object parent)
        {
            if (parent is not List<int> source)
            {
                yield break;
            }

            for (var i = 0; i < source.Count; i++)
            {
                created++;
                yield return new ReuObjectTreeItem(source, i, source[i]);
            }
        }

        // Act
        RenderObjectTree(list, ProvideChildren);

        // Assert
        await Assert.That(created).IsBetween(1, 100);
    }
}
