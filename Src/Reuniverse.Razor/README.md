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

For tooltips, the [error panel](#error-panel), the [update prompt](#update-prompt), the responsive `<ReuNavMenu>` and keyboard focus in `<ReuModal>`, also reference the script before the closing `</body>` tag. It works without interactivity, so tooltips also work with static SSR. If omitted, tooltips are not shown, errors only show in `#blazor-error-ui`, new versions of a PWA are not announced, the navbar falls back to static width breakpoints and an open modal neither takes the focus nor keeps it:

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

`reuniverse.css`, `reuniverse.js` and `reuniverse.theme.js` also come minified as `reuniverse.min.css`, `reuniverse.min.js` and `reuniverse.theme.min.js`, to use in their place.

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

### Error panel

When Blazor reports an unhandled error, `reuniverse.js` shows a themed panel with the error log in place of `#blazor-error-ui`, with buttons to copy the log and to reload the page. It needs no setup other than the script, but the `#blazor-error-ui` element has to stay in the page, as that is how Blazor reports the error.

The log holds the last 100 errors written to the console, along with uncaught errors and rejected promises, which do not open the panel by themselves. It is kept until the panel is dismissed or the page is reloaded.

### Update prompt

In a PWA, `reuniverse.js` shows a prompt in the corner of the page once a new version of the app is installed, with a button to switch to it by reloading the page. It also checks for a new version every hour and when the page is shown again, as an app that does not navigate would otherwise only check when it is opened. Without a service worker, as in a Blazor Web App, it does nothing.

A new service worker only takes over once every tab of the app is closed, unless it calls `skipWaiting()`. For the prompt to be able to switch to it, the service worker has to call it when asked, and not on install, as the version still running would lose its cache (`wwwroot/service-worker.published.js`):

```js
self.addEventListener('message', event => {
    if (event.data?.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
```

A service worker that still calls `skipWaiting()` on install has already taken over when the prompt shows, so updating then just reloads the page.

To handle updates yourself, opt out with the `data-reu-update-prompt="off"` attribute on `<html>` (`wwwroot/index.html`). That turns off both the prompt and the update checks, while the rest of the script keeps working:

```html
<html lang="en" data-reu-update-prompt="off">
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
- `Entries` - A static set of `ReuLogEntry` items to display when no `Provider` is set. Reloaded only when a different collection instance is passed, so pass a new collection or use `Add()` instead of changing the existing one.
- `ItemSize` - The fixed height (in pixels) of every log row. (default: 26)
- `MaxEntries` - The maximum number of entries to keep when receiving live entries. Null keeps all. (default: `null`)
- `ScrollToBottom` - Whether to scroll to the bottom when an entry is added. (default: `true`)
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
- `IconSrc` - A custom icon image source (used when `IconType` is not set). It is shown as it is, so it should suit both themes.
- `IconWidth` - Icon width in pixels. (default: 32)
- `IconHeight` - Icon height in pixels. (default: 32)
- `IconOnly` - Whether to show only the icon with no label text.
- `Title` - Tooltip text for the nav item (uses `data-reu-tooltip`). Defaults to `Name`.
- `ChildContent` - Nested `<ReuNavItem>` elements, shown as a submenu. The item keeps its own `Href`, which is optional.
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

Submenus work without interactivity, so also with static SSR. They show on hover and on keyboard focus, and the burger menu lists them in place, always expanded. On touch screens outside of the burger menu, the first tap on the item shows the submenu and the second follows its link, which needs `reuniverse.js`. The parent is highlighted while one of its items is the current page.

```cshtml
<ReuNavMenu Brand="My App">
    <ReuNavItem Name="Tools" Href="tools" IconType="IconType.Folder">
        <ReuNavItem Name="Converter" Href="tools/converter" IconType="IconType.Webhook" />
        <ReuNavItem Name="Viewer" Href="tools/viewer" />
    </ReuNavItem>
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
- `History` - A `ReuObjectTreeHistory` that records the edits made through the tree, which can also be shared between trees. When not set, nothing is recorded.

The tree does not show any undo controls itself, so call `Undo()` and `Redo()` from your own buttons or shortcuts. Edits happen inside the tree, so subscribe to `Changed` to keep `CanUndo` and `CanRedo` up to date on your page. Call `Clear()` when the tree starts showing a different object, as the steps still edit the previous one.

```cshtml
<ReuButton Enable="history.CanUndo" OnClick="() => history.Undo()">Undo</ReuButton>
<ReuButton Enable="history.CanRedo" OnClick="() => history.Redo()">Redo</ReuButton>
<ReuObjectTree Value="obj" History="history"></ReuObjectTree>

@code {
    private readonly ReuObjectTreeHistory history = new();

    protected override void OnInitialized()
    {
        history.Changed += StateHasChanged;
    }
}
```

Custom value components can take part by calling `History.Record(description, undo, redo)`.

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

### `<ReuTabs>` and `<ReuTab>`

Tabs that show the content of the selected `<ReuTab>`. The first tab is selected by default.

- **Requires interactivity** to function properly.
- `TitleContent` - Content of the tab header.
- `ChildContent` - Content shown while the tab is selected.
- `CssClass` - Additional CSS classes to apply to the tab header.
- `ShowCloseButton` - Whether to show a close (✕) button in the tab header.
- `OnClosed` - Callback invoked when the close button is clicked.

A tab stays until it is removed from the markup, so remove it in `OnClosed`. If it was selected, the tab next to it is selected instead.

The tabs follow the [ARIA tabs pattern](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/), so screen readers announce them as tabs. The tab bar is a single stop for Tab, which goes on to the content of the selected tab; the arrow keys, Home and End move between the tabs and select them. A tab closed from the keyboard moves the focus to the tab selected in its place.

Example:

```cshtml
<ReuTabs>
    <ReuTab>
        <TitleContent>Tab 1</TitleContent>
        <ChildContent>Content of Tab 1.</ChildContent>
    </ReuTab>
    @if (showTab)
    {
        <ReuTab ShowCloseButton="true" OnClosed="() => showTab = false">
            <TitleContent>Tab 2</TitleContent>
            <ChildContent>This tab can be closed.</ChildContent>
        </ReuTab>
    }
</ReuTabs>

@code {
    private bool showTab = true;
}
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

A component to show a tooltip for its content after it is hovered or focused with the keyboard for half a second. Works in every render mode, including static SSR, and needs no setup other than the `reuniverse.js` script.

- `Text` - The text of the tooltip. Line breaks are kept.

Example:

```cshtml
<ReuTooltip Text="Saved 2 minutes ago">Saved</ReuTooltip>
```

Components with a `Tooltip` parameter use the same tooltip. To add one to any other element, set the `data-reu-tooltip` attribute:

```cshtml
<img src="map.png" alt="Map" data-reu-tooltip="Made by Hylis" />
```

The tooltip disappears on its own when the element is removed, and picks up any change to the text.

### `<ReuUploadArea>`

A drag-and-drop file upload area with click-to-upload functionality.

- **Requires interactivity** to function properly.
- Allows HTML content (has `ChildContent`).
- `OnUpload` - Callback invoked when files are uploaded.
- `OnFileTooLarge` - Callback invoked when a file is too large.
- `OnFileExceedCount` - Callback invoked when the file count exceeds the maximum.
- `MaxFileCount` - Maximum number of files allowed in a single selection. Null means no limit. This does not limit the total number of files a client can upload, as each new selection starts over.
- `MaxFileSize` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of one or more file types, or unique file type specifiers, describing which file types to allow.
- `CssClass` - Additional CSS classes to apply to the upload area element.
- `LoaderSize` / `LoaderThickness` - Size and border thickness of the loader in pixels. (default: 50 / 8)
- `Streamed` - Opens files as seekable streams through `OnStream` instead of uploading them whole through `OnUpload`. Use it for large files. (default: `false`)
- `OnStream` - Callback invoked for each file when `Streamed` is `true`, with a seekable `ReuBrowserFileStream`. The handler owns the stream and should dispose it.
- `Directory` - Picks a folder instead of files. Every file in it matching `Accept` is uploaded, with its path within the folder in `RelativePath`.
- `Paste` - Uploads files pasted anywhere on the page (Ctrl+V), filtered by `Accept`. Enable it on only one upload component per page.
- Additional attributes are passed to the **input** element.

`<ReuUploadButton>` and `<ReuUploadScreen>` take the same upload parameters.

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