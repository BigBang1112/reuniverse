# Reuniverse.Razor

[![NuGet](https://img.shields.io/nuget/vpre/Reuniverse.Razor?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Reuniverse.Razor/)

Common Blazor components for BigBang1112 projects (gbx.tools).

## Setup

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

In Blazor WebAssembly:

`wwwroot/index.html`

```html
<link rel="stylesheet" href="_content/Reuniverse.Razor/reuniverse.css" />
<link href="[YOUR_PROJECT_NAME].styles.css" rel="stylesheet" />
```

```html
<script src="_content/Reuniverse.Razor/reuniverse.js" defer></script>
```

### Theming

Reuniverse has a light/dark theme system driven by the `data-theme` attribute on `<html>`. Include `reuniverse.theme.js` as a **blocking** (no `defer`/`async`) script at the very top of `<head>`, before any stylesheets:

In Blazor Web App (`App.razor`):

```html
<script src="@Assets["_content/Reuniverse.Razor/reuniverse.theme.js"]"></script>
```

In Blazor WebAssembly (`wwwroot/index.html`):

```html
<script src="_content/Reuniverse.Razor/reuniverse.theme.js"></script>
```

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

### Components

To use the components:

```cshtml
@using Reuniverse.Razor;

<ReuNavMenu Brand="reuniverse.net">
    <ReuNavItem Name="Home" Href="" IconType="IconType.Home" NavLinkMatch="NavLinkMatch.All"></ReuNavItem>
    <ReuNavItem Name="Downloads" Href="downloads" IconType="IconType.Download"></ReuNavItem>
    <ReuNavItem Name="About" Href="about" IconType="IconType.Info"></ReuNavItem>
    <ReuNavItem Name="GitHub" Href="https://github.com/BigBang1112/nations-converter" IconType="IconType.GitHub" IsNavLink="false" IconOnly="true" IconWidth="28" IconHeight="28"></ReuNavItem>
    <ReuNavItem Name="Discord" Href="https://discord.nc.gbx.tools" IconType="IconType.Discord" IsNavLink="false" IconOnly="true" Title="Join the Discord server"></ReuNavItem>
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
- `Tooltip` - The tooltip text to display on hover. **(*currently* requires interactivity)**
- `CssClass` - Additional CSS classes to apply to the button.
- Additional attributes are supported.

Example:

```cshtml
<ReuButton Href="https://example.com" Variant="ButtonVariant.Blue" Tooltip="Go to example.com">
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
<ReuFriendlyType Type="typeof(Dictionary&lt;string, List&lt;int&gt;&gt;)" />
```

### `<ReuCheckbox>`

A custom checkbox component with label and state management. It should also work for SSR forms.

- Allows HTML content (has `ChildContent`).
- `IsChecked` - Whether the checkbox is checked.
- `IsCheckedChanged` - Callback invoked when the checked state changes. **(requires interactivity)**
- `Size` - The size of the checkbox in pixels. (default: 28)
- `Tooltip` - The tooltip text to display on hover. **(*currently* requires interactivity)**
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

### `<ReuList>` and `<ReuListItem>`

A horizontal (or vertical) list container and its items.

**`<ReuList>`**

- Allows HTML content (has `ChildContent`).
- `Panel` - Whether to render with a panel background and border.
- `Vertical` - Whether to lay items out vertically instead of horizontally.
- `Wrap` - Whether to wrap items when they overflow.
- `Grow` - Whether list items grow to fill available space. (default: `true`)
- `CssClass` - Additional CSS classes.
- Additional attributes are passed to the list element.

**`<ReuListItem>`**

- Allows HTML content (has `ChildContent`).
- `Href` - If set, renders as `<a>`; otherwise, renders as `<button>`.
- `OnClick` - Callback invoked when the item is clicked. **(requires interactivity)**
- `Active` - Whether the item is in an active state.
- `Enable` - Whether the item is enabled.
- `NavLink` - Whether to render as a Blazor `NavLink` (only when `Href` is set).
- `CssClass` - Additional CSS classes.
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

### `<ReuLogs>`

A virtualized log viewer that displays log entries for all log levels. It can be hooked onto `ILogger` through a `ReuLoggerProvider`, or fed entries manually. **(requires interactivity)**

- `Provider` - A `ReuLoggerProvider` to subscribe to for live log entries.
- `Entries` - A static set of `ReuLogEntry` items to display when no `Provider` is set.
- `ItemSize` - The fixed height (in pixels) of every log row. (default: 26)
- `MaxEntries` - The maximum number of entries to keep when receiving live entries. (default: 1000)
- `ShowTimestamp` - Whether to show the timestamp column. (default: `true`)
- `OnEntryClick` - Callback invoked when a row with an exception is clicked (the whole row is clickable).
- `OnParameterClick` - Callback invoked when a structured log parameter token is clicked.
- `CssClass` - Additional CSS classes to apply to the root element.
- Additional attributes are passed to the root element.

Public methods available via `@ref`:
- `Add(ReuLogEntry)` - Appends an entry manually.
- `Clear()` - Clears all entries.
- `GetEntries()` - Returns a snapshot of current entries.
- `ExportAsText()` - Exports entries as a plain-text string.
- `ExportAsCsv()` - Exports entries as a CSV string (with header row).
- `ExportAsJson()` - Exports entries as a JSON array string.

Selectable state (readable via `@ref`):
- `SelectedException` - The exception of the currently selected row, or `null`.
- `SelectedParameter` - The currently selected `ReuLogParameter` token, or `null`.

Clicking a row with an exception toggles `SelectedException` and highlights the entire row. Clicking a parameter token toggles `SelectedParameter`. Both support text selection.

Scopes from `ILogger.BeginScope(...)` are captured automatically and displayed between the level badge and the message as `scope1 → scope2 → …`.

Hook it onto the logging pipeline:

```csharp
var logProvider = new ReuLoggerProvider();
builder.Services.AddSingleton(logProvider);
builder.Logging.AddProvider(logProvider);
```

Example:

```cshtml
@inject ReuLoggerProvider LogProvider

<ReuLogs @ref="logs" Provider="LogProvider"
         OnEntryClick="e => selectedException = e.Exception"
         OnParameterClick="p => Console.WriteLine($"{p.Name} = {p.Value}")" />

@code {
    private ReuLogs? logs;
    private Exception? selectedException;

    private void Export() =>
        Console.WriteLine(logs?.ExportAsText());
}
```

### `<ReuMain>`

A main content wrapper to define the primary content area of a page. It is used to fill the vertical space between the header and footer.

- Allows HTML content (has `ChildContent`).
- `CssClass` - Additional CSS classes to apply to the main element.
- Additional attributes are passed to the main element.

### `<ReuNavMenu>` and `<ReuNavItem>`

A top navigation bar with a brand name, burger menu for mobile, and nav items.

**`<ReuNavMenu>`**

- Allows HTML content (has `ChildContent`). Place `<ReuNavItem>` elements inside.
- `Brand` - The brand/site name shown on the left. **(required)**
- `BurgerImageSrc` - Optional custom image source for the burger menu icon.
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
- `IconWidth` - Icon width in pixels. (default: 24)
- `IconHeight` - Icon height in pixels. (default: 24)
- `IconOnly` - Whether to show only the icon with no label text.
- `Title` - Tooltip/title text for the nav item.
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
- `HideTypes` - If to hide type names in the tree.
- `OnTypeClick` - Callback invoked when a type name is clicked.
- `IsExpandedByDefault` - Whether the **root** object is expanded by default.

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
- `Tooltip` - The tooltip text to display on hover. **(*currently* requires interactivity)**
- Additional attributes are passed to the **input** element.

Example:

```cshtmlhtml
<ReuRadio Name="Group">Option 1</ReuRadio>
<ReuRadio Name="Group">Option 2</ReuRadio>
<ReuRadio Name="Group" disabled>Option 3</ReuRadio>
<ReuRadio Name="Group">Option 4</ReuRadio>
```

### `<ReuTooltip>`

A component to display a tooltip either globally for `Tooltip` parameters, or for specific content.

TBD

### `<ReuUploadArea>`

A drag-and-drop file upload area with click-to-upload functionality.

- **Requires interactivity** to function properly.
- Allows HTML content (has `ChildContent`).
- `OnUpload` - Callback invoked when files are uploaded.
- `OnFileTooLarge` - Callback invoked when a file is too large.
- `OnFileExceedCount` - Callback invoked when the file count exceeds the maximum.
- `MaxFileCount` - Maximum number of files allowed to upload. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `MaxFileSize` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of one or more file types, or unique file type specifiers, describing which file types to allow.
- `CssClass` - Additional CSS classes to apply to the upload area element.
- Additional attributes are passed to the **input** element.

### `<ReuFileBrowser>`

A file-system browser with breadcrumb navigation, sorted listings, and keyboard support. Entries are virtualized for performance. **(requires interactivity)**

- `Root` - The root `IReuFolder` to browse. **(required)**
- `OnFileOpen` - Callback invoked when a file is opened.
- `OnFolderOpen` - Callback invoked when a folder is navigated into.
- `OnSelect` - Callback invoked when an entry is single-clicked.
- `DoubleClickToOpen` - When `true`, a double-click opens entries and a single click selects. When `false`, a single click selects and a second click (or Enter) opens. (default: `false`)
- `ItemSize` - Row height in pixels used for virtualization. Must match the actual rendered row height. (default: 32)
- `CssClass` - Additional CSS classes to apply to the browser element.
- Additional attributes are supported.

Implement `IReuFolder` and `IReuFile` (both extend `IReuEntry`) to provide your own data source:

```cshtml
<ReuFileBrowser Root="root" OnFileOpen="f => lastOpened = f.Name" style="height: 400px" />

@code {
    private IReuFolder root = new MyFolder("Root",
    [
        new MyFolder("Documents", [new MyFile("notes.txt")]),
        new MyFile("readme.md")
    ]);
}