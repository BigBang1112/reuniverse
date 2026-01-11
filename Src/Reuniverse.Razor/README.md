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

In Blazor WebAssembly:

`wwwroot/index.html`

```html
<link rel="stylesheet" href="_content/Reuniverse.Razor/reuniverse.css" />
<link href="[YOUR_PROJECT_NAME].styles.css" rel="stylesheet" />
```

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
        © 2026 Petr '<a href="https://bigbang1112.cz">BigBang1112</a>' Pivoòka<br />
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

TBD

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

### `<ReuNavMenu>` and `<ReuNavItem>`

TBD

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
- `MaxFileSizeInBytes` - Maximum file size in bytes. Null means no limit. **Make sure to set limits if in serverside interactivity!**
- `Accept` - A comma-separated list of one or more file types, or unique file type specifiers, describing which file types to allow.
- `CssClass` - Additional CSS classes to apply to the upload area element.
- Additional attributes are passed to the **input** element.