# AuthPlaypen.Client.Sdk

NuGet package ID: `AuthPlaypen.Client.Sdk`
Target framework: `netstandard2.0`

This SDK is intended for older and newer .NET applications that need outbound calls to AuthPlaypen.

> This README is embedded in the NuGet package (`PackageReadmeFile`) so it is visible in package-feed UIs after publish.

## Covered features

- Client credentials token requests (`/connect/token`)
- Token introspection (`/connect/introspect`)
- Permission alias metadata retrieval (`/.well-known/authplaypen/permissions`)

## Registration

```csharp
services.AddAuthPlaypenClientSdk(options =>
{
    options.Authority = "https://localhost:5100";
    options.ClientId = "legacy-app";
    options.ClientSecret = "change-me";
});
```

## Azure DevOps/internal feed packaging note

If you use a `DotNetCoreCLI@2` `pack` step (like your sample pipeline), the generated `.nupkg` will include this README automatically.
No extra pipeline step is required beyond running `dotnet pack` on this project.
