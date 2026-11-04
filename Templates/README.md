# Reuniverse.Templates

[![NuGet](https://img.shields.io/nuget/vpre/Reuniverse.Templates?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Reuniverse.Templates/)

Various templates for opinionated development by BigBang1112.

## Development

Within this repository, `Directory.Build.targets` replaces each `PackageReference` that matches a project in `Src/` (for example `Reuniverse.Razor`) with a `ProjectReference`, so the templates build against the current checkout, including changes not yet published. Pass `-p:UseLocalTemplateReferences=false` to build against NuGet instead.

`Directory.Build.targets` is not included in the template package. Projects created with `dotnet new` keep the package references and versions declared in the templates.
