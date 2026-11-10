# Reuniverse

Common Blazor, backend, Discord bot, or general code pieces for less repetitive development.

## Reuniverse.Razor

[![NuGet](https://img.shields.io/nuget/vpre/Reuniverse.Razor?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Reuniverse.Razor/)

In Blazor Web App:

`App.razor`

```html
<link rel="stylesheet" href="@Assets["_content/Reuniverse.Razor/reuniverse.css"]" />
<link rel="stylesheet" href="@Assets["_content/Reuniverse.Razor/Reuniverse.Razor.bundle.scp.css"]" />
```

For the responsive `<ReuNavMenu>`, also reference the script before the closing `</body>` tag (it works without interactivity; if omitted, the navbar falls back to static width breakpoints):

```html
<script src="@Assets["_content/Reuniverse.Razor/reuniverse.js"]" defer></script>
```

In Blazor WebAssembly or Photino Blazor:

`wwwroot/index.html`

```html
<link rel="stylesheet" href="_content/Reuniverse.Razor/reuniverse.css" />
<link rel="stylesheet" href="[YOUR_PROJECT_NAME].styles.css" />
```

```html
<script src="_content/Reuniverse.Razor/reuniverse.js" defer></script>
```

`reuniverse.css`, `reuniverse.js` and `reuniverse.theme.js` also come minified as `reuniverse.min.css`, `reuniverse.min.js` and `reuniverse.theme.min.js`, to use in their place.

### Theming

Reuniverse has a light/dark theme system driven by the `data-theme` attribute on `<html>`. Include `reuniverse.theme.js` as a **blocking** (no `defer`/`async`) script at the very top of `<head>`, before any stylesheets:

In Blazor Web App (`App.razor`):

```html
<script src="@Assets["_content/Reuniverse.Razor/reuniverse.theme.js"]"></script>
```

In Blazor WebAssembly or Photino Blazor (`wwwroot/index.html`):

```html
<script src="_content/Reuniverse.Razor/reuniverse.theme.js"></script>
```

Without a saved theme, it follows the OS/browser color scheme preference. If the [Dark Reader](https://darkreader.org/) extension is active, it uses the dark theme instead and adds `<meta name="darkreader-lock">`, so the real dark theme shows rather than an inverted light one.

The script exposes a `window.reuTheme` API:

| Method | Description |
|--------|-------------|
| `reuTheme.get()` | Returns the current theme: `"dark"` (default) or `"light"`. |
| `reuTheme.set(theme)` | Sets and persists the theme. Returns the applied theme. |
| `reuTheme.toggle()` | Toggles between `"dark"` and `"light"`. Returns the new theme. |

You can call `reuTheme.toggle()` directly from HTML, for example in an `onclick` handler:

```html
<button onclick="reuTheme.toggle()">Toggle theme</button>
```

Or use the [`<ReuThemeToggle>`](#reuthemetoggle) component.

### Components

To use the components:

```cshtml
@using Reuniverse.Razor;

<ReuNavMenu Brand="reuniverse.net">
    <ReuNavItem Name="Home" Href="" IconType="IconType.Home" NavLinkMatch="NavLinkMatch.All"></ReuNavItem>
    <ReuNavItem Name="Downloads" Href="downloads" IconType="IconType.Download"></ReuNavItem>
    <ReuNavItem Name="About" Href="about" IconType="IconType.Info"></ReuNavItem>
    <ReuNavItem Name="GitHub" Href="https://github.com/BigBang1112/reuniverse" IconType="IconType.GitHub" IsNavLink="false" IconOnly="true" IconWidth="28" IconHeight="28"></ReuNavItem>
    <ReuNavItem Name="Discord" Href="https://discord.gbx.tools" IconType="IconType.Discord" IsNavLink="false" IconOnly="true" Title="Join the Discord server"></ReuNavItem>
</ReuNavMenu>
```

### `<ReuButton>`

A versatile button component that can render as a link or a button with various styles and behaviors.

- Allows HTML content (has `ChildContent`).
- `Href` - If set, renders as `<a>`; otherwise, renders as `<button>`.
- `OnClick` - Callback invoked when the button is clicked. **(requires interactivity)**
- `Active` - Whether the button is in an active state.
- `Enable` - Whether the button is enabled.
- `NavLink` - Whether to render as a Blazor `NavLink` (only when `Href` is set).
- `Variant` - The variant style of the button (`Default`, `Blue`, `Yellow`, `Red`).
- `Tooltip` - The tooltip text to display on hover or keyboard focus (uses `data-reu-tooltip`).
- `CssClass` - Additional CSS classes to apply to the button.
- Additional attributes are supported.

Example:

```cshtml
<ReuButton Href="https://example.com" Variant="ReuButtonVariant.Blue" Tooltip="Go to example.com">
    Click Me
</ReuButton>
```

### `<ReuCenter>`

A simple component to center its child content (currently only horizontally).

- Allows HTML content (has `ChildContent`).
- `CssClass` - Additional CSS classes to apply to the center element.
- Additional attributes are passed to the center element.

Example:

```cshtml
<ReuCenter>
    <p>This content is centered.</p>
</ReuCenter>
```

### `<ReuContainer>`

A page container to wrap content with padding and natural width constraints.

- Allows HTML content (has `ChildContent`).
- `CssClass` - Additional CSS classes to apply to the container element.
- `Overflow` - Whether to enable horizontal overflow handling.
- Additional attributes are passed to the container element.

Example:

```cshtml
<ReuContainer Overflow="true">
    <p>This content is inside a container with overflow handling.</p>
</ReuContainer>
```

### `<ReuFooter>`

A footer component for consistent page footers.

- Allows HTML content:
  - `<CopyrightContent>` - Content for copyright information.
  - `<LinkContent>` - Content for footer links.
  - `<ExtraContent>` - Extra content to include in the footer.
- `CssClass` - Additional CSS classes to apply to the footer element.
- Additional attributes are passed to the footer element.

Example:

```cshtml
<ReuFooter>
    <CopyrightContent>
        © 2026 Petr '<a href="https://bigbang1112.cz">BigBang1112</a>' Pivoňka<br />
        <small>
            Not affiliated with or endorsed by Nadeo or Ubisoft.
            All relevant trademarks belong to their respective owners.
        </small>
    </CopyrightContent>
    <LinkContent>
        <a href="#">Terms of Service</a>
        <a href="#">Privacy Policy</a>
        <a href="#">API</a>
    </LinkContent>
    <ExtraContent>
        <div class="footer-discover-more">
            <span>Discover more:</span>
            <a href="https://gbx.tools">gbx.tools</a>
            <a href="https://explorer.gbx.tools">explorer.gbx.tools</a>
        </div>
    </ExtraContent>
</ReuFooter>
```

### `<ReuFriendlyType>`

A component to display friendly names for .NET types.

- `Type` - The .NET type to display.
- `OnClick` - Callback invoked when one of the types is clicked. **(requires interactivity)**

Example:

```cshtml
<ReuFriendlyType Type="typeof(Dictionary<string, List<int>>)" />
```

### `<ReuCheckbox>`

A custom checkbox component with label and state management. It should also work for SSR forms.

- Allows HTML content (has `ChildContent`).
- `IsChecked` - Whether the checkbox is checked.
- `IsCheckedChanged` - Callback invoked when the checked state changes. **(requires interactivity)**
- `Size` - The size of the checkbox in pixels. (default: 30)
- `Tooltip` - The tooltip text to display on hover or keyboard focus (uses `data-reu-tooltip`).
- Additional attributes are passed to the **input** element.

Example:

```cshtml
<ReuCheckbox IsChecked="@isChecked" IsCheckedChanged="OnCheckedChanged" Tooltip="Check me!">
    Accept Terms and Conditions
</ReuCheckbox>

@code {
    private bool isChecked = false;
    private void OnCheckedChanged(bool newValue)
    {
        isChecked = newValue;
    }
}
```

### `<ReuHexEditor>`

A virtualized hex editor with offset, hex and text columns, which edits a `byte[]` in place (overwrite only). **(requires interactivity)**

- `Data` - The bytes to show and edit. **(required)**
- `ReadOnly` - Allows only viewing, selecting and copying.
- `BytesPerRow` - The number of bytes on each row, from 1 to 64. (default: 16)
- `ItemSize` - The fixed row height in pixels for virtualization. (default: 20)
- `Highlights` - `ReuHexHighlight(Offset, Length, Color, Tooltip)` ranges shown with a background color and tooltip, such as chunks or fields.
- `History` - A `ReuObjectTreeHistory` to record edits, also undone and redone with Ctrl+Z and Ctrl+Y. It can be shared with a `<ReuObjectTree>`.
- `ShowStatus` - Shows the offset, value and field of the byte at the cursor. (default: `true`)
- `Label` - The accessible label. (default: `Hex editor`)
- `OnEdit` - Callback invoked with a `ReuHexEdit` after a byte was changed by typing.
- `SelectionChanged` - Callback invoked with the new `ReuHexSelection`.
- `CssClass` - Additional CSS classes to apply to the editor.
- Additional attributes are passed to the editor element.

Click, Shift+click or drag to select, and use the arrow keys, Page Up/Down, Home/End (with Ctrl for the whole data) to move, with Shift to extend the selection. Type hex digits in the hex column or characters in the text column to overwrite bytes. Ctrl+A selects all and Ctrl+C copies the selection as hex or text. Changed bytes are marked until they get their original value back. Hovering a byte also marks it in the other column and its column in the header. `SelectAsync(offset, length)` selects bytes and scrolls to them, and `Selection` gives the selected range.

```cshtml
<ReuHexEditor Data="data" Highlights="highlights" History="history" style="height: 400px" />

@code {
    private byte[] data = File.ReadAllBytes("map.Map.Gbx");
    private ReuObjectTreeHistory history = new();
    private ReuHexHighlight[] highlights = [new(0, 3, Tooltip: "Magic"), new(3, 2, "#1E72CC66", "Version")];
}
```

### `<ReuList>` and `<ReuListItem>`

A horizontal (or vertical) list container and its items.

**`<ReuList>`**

- Allows HTML content (has `ChildContent`).
- `Panel` - Whether to render with a panel background and border.
- `Vertical` - Whether to lay items out vertically instead of horizontally.
- `Wrap` - Whether to wrap items when they overflow.
- `Grow` - Whether list items grow to fill available space. (default: `true`)
- `CssClass` - Additional CSS classes to apply to the list element.
- Additional attributes are passed to the list element.

**`<ReuListItem>`**

- Allows HTML content (has `ChildContent`).
- `Href` - If set, renders as `<a>`; otherwise, renders as `<button>`.
- `OnClick` - Callback invoked when the item is clicked. **(requires interactivity)**
- `Active` - Whether the item is in an active state.
- `Enable` - Whether the item is enabled.
- `NavLink` - Whether to render as a Blazor `NavLink` (only when `Href` is set).
- `CssClass` - Additional CSS classes to apply to the item element.
- Additional attributes are passed to the item element.

Example:

```cshtml
<ReuList Panel="true" Wrap="true">
    <ReuListItem>Item A</ReuListItem>
    <ReuListItem Active="true">Item B (active)</ReuListItem>
    <ReuListItem Enable="false">Item C (disabled)</ReuListItem>
</ReuList>
```

### `<ReuLoader>`

A loading spinner component to indicate loading states.

- `Size` - The size of the loader in pixels. (default: 50)
- `Thickness` - The thickness of the loader border in pixels. (default: 8)
- `CssClass` - Additional CSS classes to apply to the loader element.
- Additional attributes are passed to the footer element.

Example:

```cshtml
<ReuLoader Size="60" Thickness="10" />
```

### `<ReuMain>`

A main content wrapper to define the primary content area of a page. It is used to fill the vertical space between the header and footer.

- Allows HTML content (has `ChildContent`).
- `CssClass` - Additional CSS classes to apply to the main element.
- Additional attributes are passed to the main element.

### `<ReuMetadataTags>`

Renders the description, Open Graph, Twitter and canonical tags. Place it inside `<HeadContent>`.

- `Title` - The page title. **(required)**
- `Description`, `SiteName`, `Type` (default: `website`), `ThemeColor`
- `Url` / `CanonicalUrl` - Default to the current URL.
- `ImageUrl` (absolute) or `ImagePath` (relative), `ImageWidth` / `ImageHeight` (default: 1200 × 630)
- `IncludeTwitter`, `TwitterCard` (default: `summary_large_image`), `IncludeCanonical`

```cshtml
<HeadContent>
    <ReuMetadataTags Title="My App" Description="My description." ImagePath="img/preview.png" />
</HeadContent>
```

### `<ReuModal>`

A dialog shown over the page. **(requires interactivity)**

- `IsOpen` - Whether the modal is open (supports `@bind-IsOpen`).
- `Title` / `TitleContent` - The header title as text or custom content.
- `ChildContent` / `FooterContent` - The body and the optional footer.
- `ShowCloseButton`, `CloseOnBackdropClick`, `CloseOnEscape` - Ways to close it. (default: `true`)
- `OnClosed` - Callback invoked after the modal closes.
- `MaxWidth` / `Height` - CSS sizes of the panel. (default: `600px` / `auto`)
- `CssClass` - Additional CSS classes to apply to the panel.

```cshtml
<ReuModal @bind-IsOpen="isOpen" Title="Example">
    <ChildContent>Modal body.</ChildContent>
    <FooterContent><ReuButton OnClick="() => isOpen = false">Close</ReuButton></FooterContent>
</ReuModal>
```

### `<ReuNavMenu>` and `<ReuNavItem>`

A top navigation bar with a brand name, burger menu for mobile, and nav items.

**`<ReuNavMenu>`**

- Allows HTML content (has `ChildContent`). Place `<ReuNavItem>` elements inside.
- `Brand` - The brand/site name shown on the left. **(required)**
- `BurgerImageSrc` - Optional custom image source for the burger menu icon.
- `IsAbsolute` - Whether to place the bar over the page content instead of above it.
- `CssClass` - Additional CSS classes to apply to the nav element.
- Additional attributes are passed to the nav element.

**`<ReuNavItem>`**

- `Name` - The label text for the nav item.
- `AltName` - An alternative label shown on mobile/narrow layouts.
- `Href` - The URL the nav item links to.
- `IsNavLink` - Whether to render as a Blazor `NavLink` for active-state tracking. (default: `true`)
- `NavLinkMatch` - The `NavLinkMatch` rule used when `IsNavLink` is `true`.
- `IconType` - A built-in `IconType` enum value for the icon.
- `IconSrc` - A custom icon image source (used when `IconType` is not set).
- `IconWidth` - Icon width in pixels. (default: 32)
- `IconHeight` - Icon height in pixels. (default: 32)
- `IconOnly` - Whether to show only the icon with no label text.
- `Title` - Tooltip text for the nav item (uses `data-reu-tooltip`). Defaults to `Name`.
- `ChildContent` - Nested `<ReuNavItem>` elements, shown as a submenu.
- `CssClass` - Additional CSS classes to apply to the nav item element.
- Additional attributes are passed to the nav item element.

Example:

```cshtml
<ReuNavMenu Brand="My App">
    <ReuNavItem Name="Home" Href="" IconType="IconType.Home" NavLinkMatch="NavLinkMatch.All" />
    <ReuNavItem Name="Downloads" Href="downloads" IconType="IconType.Download" />
    <ReuNavItem Name="GitHub" Href="https://github.com/example/repo"
                IconType="IconType.GitHub" IsNavLink="false" IconOnly="true" />
</ReuNavMenu>
```

### `<ReuObjectTree>`

A component to display object properties in a tree structure.

- **Requires interactivity** to function properly.
- `Value` - The object to display.
- `DisplayName` - The display name for the root object.
- `HideType` - If to hide type names in the tree.
- `OnTypeClick` - Callback invoked when a type name is clicked.
- `IsExpandedByDefault` - Whether the **root** object is expanded by default.
- `History` - A `ReuObjectTreeHistory` to record edits for `Undo()` / `Redo()`. Nothing is recorded when not set.

Example:

```cshtml
<ReuObjectTree Value="obj" IsExpandedByDefault="true"></ReuObjectTree>

@code {
    private MyClass obj = new MyClass
    {
        Name = "Example",
        Age = 30,
        Address = new Address
        {
            Street = "123 Main St",
            City = "Anytown"
        }
    };

    public class MyClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }
}
```

### `<ReuRadio>`

A custom radio button component with label and state management. It should also work for SSR forms.

- Allows HTML content (has `ChildContent`).
- `IsChecked` - Whether the radio button is checked.
- `IsCheckedChanged` - Callback invoked when the checked state changes. **(requires interactivity)**
- `Size` - The size of the radio button in pixels. (default: 28)
- `Name` - The name of the radio button group. **(required)**
- Additional attributes are passed to the **input** element.

Example:

```cshtml
<ReuRadio Name="Group">Option 1</ReuRadio>
<ReuRadio Name="Group">Option 2</ReuRadio>
<ReuRadio Name="Group" disabled>Option 3</ReuRadio>
<ReuRadio Name="Group">Option 4</ReuRadio>
```

### `<ReuThemeToggle>`

A lightbulb icon button that switches between the light and dark theme. Needs `reuniverse.theme.js` (see [Theming](#theming)), works without interactivity.

- `Size` - The size of the icon in pixels. (default: 20)
- `Tooltip` - The tooltip text, also used as the accessible label (uses `data-reu-tooltip`). (default: `Switch theme`)
- `CssClass` - Additional CSS classes to apply to the button.
- Additional attributes are passed to the button element.

```cshtml
<ReuThemeToggle />
```

### `<ReuTooltip>`

A component to show a tooltip for its content on hover or keyboard focus. Needs `reuniverse.js`, works without interactivity.

- `Text` - The tooltip text.

```cshtml
<ReuTooltip Text="Saved 2 minutes ago">Saved</ReuTooltip>
```

`Tooltip` parameters and `<ReuTooltip>` render the `data-reu-tooltip` attribute, which can also be set on any element directly.

### `<ReuUploadArea>`

A drag-and-drop file upload area with click-to-upload functionality.

- **Requires interactivity** to function properly.
- Allows HTML content (has `ChildContent`).
- `OnUpload` - Callback invoked when files are uploaded.
- `OnFileTooLarge` - Callback invoked when a file is too large.
- `OnFileExceedCount` - Callback invoked when the file count exceeds the maximum.
- `MaxFileCount` - Maximum number of files allowed to upload. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `MaxFileSize` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of file types or unique file type specifiers describing which file types to allow.
- `LoaderSize` - The size of the loader in pixels. (default: 50)
- `LoaderThickness` - The thickness of the loader border in pixels. (default: 8)
- `CssClass` - Additional CSS classes to apply to the upload area element.
- `Streamed` - Opens files as seekable streams through `OnStream` instead of uploading them whole through `OnUpload`. (default: `false`)
- `OnStream` - Callback invoked for each file when `Streamed` is `true`, with a `ReuBrowserFileStream` the handler should dispose.
- `Directory` - Picks a folder instead of files. Every file in it matching `Accept` is uploaded, with its path within the folder in `RelativePath`.
- `Paste` - Uploads files pasted anywhere on the page (Ctrl+V), filtered by `Accept`. Enable it on only one upload component per page.
- Additional attributes are passed to the **input** element.

Example:

```cshtml
<ReuUploadArea Accept=".gbx,.zip" MaxFileCount="10" MaxFileSize="10485760"
               OnUpload="HandleUpload">
    Drop files here or click to browse
</ReuUploadArea>
```

### `<ReuUploadButton>`

A button that opens a file picker and uploads the selected files.

- **Requires interactivity** to function properly.
- Allows HTML content (has `ChildContent`).
- `OnUpload` - Callback invoked when files are uploaded.
- `OnFileTooLarge` - Callback invoked when a file is too large.
- `OnFileExceedCount` - Callback invoked when the file count exceeds the maximum.
- `MaxFileCount` - Maximum number of files allowed to upload. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `MaxFileSize` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of file types or unique file type specifiers describing which file types to allow.
- `Enable` - Whether the button is enabled.
- `Variant` - The variant style of the button (`Default`, `Blue`, `Yellow`, `Red`).
- `Tooltip` - The tooltip text to display on hover or keyboard focus (uses `data-reu-tooltip`).
- `LoaderSize` - The size of the in-progress loader in pixels.
- `LoaderThickness` - The thickness of the loader border in pixels.
- `CssClass` - Additional CSS classes to apply to the button.
- `Streamed` - Opens files as seekable streams through `OnStream` instead of uploading them whole through `OnUpload`. (default: `false`)
- `OnStream` - Callback invoked for each file when `Streamed` is `true`, with a `ReuBrowserFileStream` the handler should dispose.
- `Directory` - Picks a folder instead of files. Every file in it matching `Accept` is uploaded, with its path within the folder in `RelativePath`.
- `Paste` - Uploads files pasted anywhere on the page (Ctrl+V), filtered by `Accept`. Enable it on only one upload component per page.
- Additional attributes are supported.

Example:

```cshtml
<ReuUploadButton Accept=".gbx" MaxFileSize="52428800" OnUpload="HandleUpload">
    Open files...
</ReuUploadButton>
```

### `<ReuUploadScreen>`

A full-screen overlay upload area that appears when `IsVisible` is `true`.

- **Requires interactivity** to function properly.
- Allows HTML content (has `ChildContent`) rendered inside the upload area.
- `IsVisible` - Whether the screen is visible.
- `IsVisibleChanged` - Callback invoked when the visibility changes (supports two-way binding).
- `OnUpload` - Callback invoked when files are uploaded.
- `OnFileTooLarge` - Callback invoked when a file is too large.
- `OnFileExceedCount` - Callback invoked when the file count exceeds the maximum.
- `MaxFileCount` - Maximum number of files allowed to upload. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `MaxFileSize` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of file types or unique file type specifiers describing which file types to allow.
- `CssClass` - Additional CSS classes to apply to the upload screen.
- `CssClassArea` - Additional CSS classes to apply to the inner upload area.
- `Streamed` - Opens files as seekable streams through `OnStream` instead of uploading them whole through `OnUpload`. (default: `false`)
- `OnStream` - Callback invoked for each file when `Streamed` is `true`, with a `ReuBrowserFileStream` the handler should dispose.
- `Directory` - Picks a folder instead of files. Every file in it matching `Accept` is uploaded, with its path within the folder in `RelativePath`.
- `Paste` - Uploads files pasted anywhere on the page (Ctrl+V), filtered by `Accept`. Enable it on only one upload component per page.

Example:

```cshtml
<ReuUploadScreen @bind-IsVisible="uploadVisible" OnUpload="HandleUpload" />
```

### `<ReuDataTable>` and `<ReuDataColumn>`

A table with sortable columns and pages, styled after the tables of envimix: no frame, thin row dividers and an optional dimmed second line. It renders without interactivity, but sorting and paging need it. Wide tables scroll sideways.

**`<ReuDataTable>`**

- `Items` - The items, which the table sorts and pages itself.
- `ItemsProvider` - Loads a page instead, given a `ReuDataTableRequest` (`Page`, `PageSize`, `Skip`, `SortBy`, `SortDirection`, `CancellationToken`), and returns a `ReuDataTableResult` with the items and the total count. Takes precedence over `Items`.
- `PageSize` - The number of items on a page. Null shows everything.
- `RowClass` - CSS classes for the row of an item.
- `EmptyContent` - What to show without items. (default: `No items`)
- `MinWidth` - The smallest width of the table as a CSS length, below which it scrolls.
- `CountLabel` / `ShowCount` - The word after the item count below the pages (default: `items`), and whether to show the count on a single page too.
- `CssClass` - Additional CSS classes. Additional attributes are passed to the container.

`GoToPageAsync(page)` and `RefreshAsync()` change the page or reload it, and `CurrentPage` and `PageCount` tell where it is.

**`<ReuDataColumn>`**

- `Title` / `TitleContent` - The header. `HideTitle` keeps it for screen readers only.
- `Value` - The value of the cell, also sorted by. `Format` formats it, such as `N0` or `yyyy-MM-dd`.
- `Secondary` - A dimmed value on a line below.
- `ChildContent` - A custom cell template (name the item with `Context="record"`).
- `SortBy` / `Sortable` / `DefaultSort` - What to sort by if not `Value`, whether the header sorts (default: when there is something to sort by), and the initial sort.
- `SortKey` - The key passed to `ItemsProvider` when sorting by this column. (default: `Title`)
- `Fit` - Makes the column only as wide as its content. `AlignRight` aligns it to the right.

```cshtml
<ReuDataTable Items="records" PageSize="20" RowClass="@(r => r.Removed ? "removed" : null)">
    <ReuDataColumn Title="#" Value="r => r.Rank" Fit="true" DefaultSort="ReuSortDirection.Ascending" />
    <ReuDataColumn Title="Player" Value="r => r.Nickname" Secondary="r => r.Login" />
    <ReuDataColumn Title="Time" Value="r => r.Time" Format="m\:ss\.fff" />
    <ReuDataColumn Title="Download" HideTitle="true" Fit="true" Context="record">
        <ReuButton Href="@record.ReplayUrl">Replay</ReuButton>
    </ReuDataColumn>
</ReuDataTable>
```

### `<ReuExpansionPanels>` and `<ReuExpansionPanel>`

An accordion-style container with collapsible panels.

**`<ReuExpansionPanels>`**

- Allows HTML content (has `ChildContent`). Place `<ReuExpansionPanel>` elements inside.
- `SingleExpansion` - When `true`, only one panel can be expanded at a time.
- `CssClass` - Additional CSS classes to apply to the panels container.
- Additional attributes are passed to the container element.

**`<ReuExpansionPanel>`**

- `TitleContent` - Render fragment for the panel header.
- `ChildContent` - Render fragment for the panel body.
- `Expanded` - Whether the panel is expanded.
- `ExpandedChanged` - Callback invoked when the expanded state changes.
- `CssClass` - Additional CSS classes to apply to the panel element.
- Additional attributes are passed to the panel element.

Example:

```cshtml
<ReuExpansionPanels SingleExpansion="true">
    <ReuExpansionPanel>
        <TitleContent>Panel 1</TitleContent>
        <ChildContent>Content of panel 1.</ChildContent>
    </ReuExpansionPanel>
    <ReuExpansionPanel Expanded="true">
        <TitleContent>Panel 2 (open by default)</TitleContent>
        <ChildContent>Content of panel 2.</ChildContent>
    </ReuExpansionPanel>
</ReuExpansionPanels>
```

### `<ReuSplitPane>`

Two panes side by side (or stacked) with a handle between them to resize them. Resizing needs `reuniverse.js` and works without interactivity: drag the handle, or focus it and use the arrow keys (Shift for bigger steps), Home and End. Double-click the handle to go back to `Size`.

- `FirstContent` / `SecondContent` - The content of the two panes.
- `Vertical` - Stacks the panes top and bottom. The split pane then needs a height.
- `Border` - Whether to draw a border around the split pane. Turn it off for a split pane nested in another one. (default: `true`)
- `Size` - The initial size of the first pane as a CSS length. (default: `50%`)
- `FirstMinSize` / `SecondMinSize` - The smallest size of each pane as a CSS length.
- `StorageKey` - Keeps the resized size in `localStorage` under this key and restores it.
- `HandleLabel` - The accessible label of the handle. (default: `Resize panes`)
- `CssClass` - Additional CSS classes to apply to the split pane.
- Additional attributes are passed to the split pane element.

A `reu-split-resize` DOM event with the new size in `detail.size` is raised on the split pane after each resize.

```cshtml
<ReuSplitPane Size="30%" FirstMinSize="150px" SecondMinSize="200px" StorageKey="explorer" style="height: 500px">
    <FirstContent>File list</FirstContent>
    <SecondContent>Details</SecondContent>
</ReuSplitPane>
```

### `<ReuTabs>` and `<ReuTab>`

A tabbed content container.

**`<ReuTabs>`**

- Allows HTML content (has `ChildContent`). Place `<ReuTab>` elements inside.
- `CssClass` - Additional CSS classes to apply to the tabs container.
- Additional attributes are passed to the container element.

**`<ReuTab>`**

- `TitleContent` - Render fragment for the tab header button.
- `ChildContent` - Render fragment for the tab body.
- `CssClass` - Additional CSS classes to apply to the tab header.
- `ShowCloseButton` - Whether to show a close (✕) button in the tab header.
- `OnClosed` - Callback invoked when the close button is clicked. Remove the tab here.

Example:

```cshtml
<ReuTabs>
    <ReuTab>
        <TitleContent>Overview</TitleContent>
        <ChildContent>This is the overview tab.</ChildContent>
    </ReuTab>
    <ReuTab>
        <TitleContent>Details</TitleContent>
        <ChildContent>This is the details tab.</ChildContent>
    </ReuTab>
</ReuTabs>
```

### `<ReuMenuBar>` and `<ReuMenuBarItemData>`

A horizontal menu bar with nested dropdown support.

- `Items` - The collection of `ReuMenuBarItemData` items to render. **(required)**
- `TemplateContent` - An optional `RenderFragment<ReuMenuBarItemData>` to customize item rendering.

`ReuMenuBarItemData` has:
- `Name` - The label text.
- `Href` - Optional link URL. When set the item acts as a link.
- `Items` - Optional nested sub-items (produces a dropdown).
- `Target` - Optional link target, such as `_blank`.
- `Action` - Optional `Func<Task>` invoked when the item is clicked.

Example:

```cshtml
<ReuMenuBar Items="menuItems" />

@code {
    IEnumerable<ReuMenuBarItemData> menuItems = new[]
    {
        new ReuMenuBarItemData
        {
            Name = "File",
            Items = new()
            {
                new() { Name = "New" },
                new() { Name = "Open" },
                new() { Name = "Save" }
            }
        },
        new ReuMenuBarItemData { Name = "Help", Href = "help" }
    };
}
```

### `<ReuFileBrowser>`

A file-system browser with breadcrumb navigation, sorted listings, and keyboard support.

- **Requires interactivity** to function properly.
- `Root` - The root `IReuFolder` to browse. **(required)**
- `OnFileOpen` - Callback invoked when a file is opened.
- `OnFolderOpen` - Callback invoked when a folder is navigated into.
- `OnSelect` - Callback invoked when an entry is single-clicked.
- `DoubleClickToOpen` - When `true`, a double-click opens entries; a single click selects. When `false`, a single click selects and a second click (or Enter) opens. (default: `false`)
- `ItemSize` - Row height in pixels used for virtualization. (default: 32)
- `CssClass` - Additional CSS classes to apply to the browser element.
- Additional attributes are supported.

Implement `IReuFolder` and `IReuFile` (both extend `IReuEntry`) to provide your own data:

```cshtml
<ReuFileBrowser Root="root" OnFileOpen="f => Console.WriteLine(f.Name)" style="height: 400px" />

@code {
    private IReuFolder root = new MyFolder("Root",
    [
        new MyFolder("Documents", [new MyFile("notes.txt")]),
        new MyFile("readme.md")
    ]);
}
```

### `<ReuLogs>`

A virtualized, live log viewer for `Microsoft.Extensions.Logging` entries.

- **Requires interactivity** to function properly.
- `Provider` - A `ReuLoggerProvider` to subscribe to for live entries.
- `Entries` - A static list of `ReuLogEntry` items to display (used when `Provider` is not set). Reloaded only when a different collection instance is passed, so pass a new collection or use `Add()` instead of changing the existing one.
- `ShowTimestamp` - Whether to show the timestamp column. (default: `true`)
- `MaxEntries` - Maximum number of entries to keep; oldest are discarded. Null keeps all. (default: `null`)
- `ScrollToBottom` - Whether to scroll to the bottom when an entry is added. (default: `true`)
- `ItemSize` - Row height in pixels for virtualization. (default: 26)
- `OnEntryClick` - Callback invoked when a row with an exception is clicked.
- `OnParameterClick` - Callback invoked when a structured log parameter token is clicked.
- `CssClass` - Additional CSS classes to apply to the log element.
- Additional attributes are supported.

Public methods: `Add(entry)`, `Clear()`, `GetEntries()`, `ExportAsText()`, `ExportAsCsv()`, `ExportAsJson()`.

Selectable state: `SelectedException` and `SelectedParameter` reflect the currently highlighted exception row or parameter token.

Scopes from `ILogger.BeginScope(...)` are captured automatically and rendered between the level badge and the message as `scope1 → scope2 → …`.

Example:

```cshtml
<ReuLogs Provider="logProvider" style="height: 300px" />

@code {
    private readonly ReuLoggerProvider logProvider = new();

    protected override void OnInitialized()
    {
        var logger = logProvider.CreateLogger("MyApp");
        logger.LogInformation("Application started");
        logger.LogWarning("Disk space is low: {FreeGb} GB remaining", 1.2);
    }
}
```
