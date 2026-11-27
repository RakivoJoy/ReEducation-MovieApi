# Generate and maintain DocFX documentation

From the repository root with the .NET 10 SDK, run:

```powershell
dotnet tool restore
dotnet restore src/MovieApi/MovieApi.csproj
dotnet docfx docs/docfx.json
```

The tool version is pinned in `.config/dotnet-tools.json`. The configuration in `docs/docfx.json` generates API metadata into `docs/api/` and HTML into `docs/_site/`. Run `dotnet docfx serve docs/_site` to preview locally.

On a pull request, add `gen-docfx`, open the **DocFX** Actions run, and download **movieapi-docfx** under **Artifacts**. Extract it before serving the site locally.

Put contributor articles in `docs/guides/` and add them to `docs/toc.yml`. Link to another article with a relative Markdown link. Use `<xref:MovieApi.Services.CastAssignmentService>` to link to a generated type.

If an article is missing, check the content glob and TOC. If a type is missing, check `docs/api-filter.yml` and the generated metadata first. For an unresolved cross-reference, compare the link target with the UID in the generated YAML. Edit the Markdown or C# source for the fix.
