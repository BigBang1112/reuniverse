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

In Blazor WebAssembly:

`wwwroot/index.html`

```html
<link rel="stylesheet" href="_content/Reuniverse.Razor/reuniverse.css" />
<link href="[YOUR_PROJECT_NAME].styles.css" rel="stylesheet" />
```

To use the components:

```cs
@using Reuniverse.Razor;

<ReuNavMenu Brand="reuniverse.net">
    <ReuNavItem Name="Home" Href="" IconType="IconType.Home" NavLinkMatch="NavLinkMatch.All"></ReuNavItem>
    <ReuNavItem Name="Downloads" Href="downloads" IconType="IconType.Download"></ReuNavItem>
    <ReuNavItem Name="About" Href="about" IconType="IconType.Info"></ReuNavItem>
    <ReuNavItem Name="GitHub" Href="https://github.com/BigBang1112/nations-converter" IconType="IconType.GitHub" IsNavLink="false" IconOnly="true" IconWidth="28" IconHeight="28"></ReuNavItem>
    <ReuNavItem Name="Discord" Href="https://discord.nc.gbx.tools" IconType="IconType.Discord" IsNavLink="false" IconOnly="true" Title="Join the Discord server"></ReuNavItem>
</ReuNavMenu>
```
