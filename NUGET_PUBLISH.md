# NuGet Publish Checklist

## Prerequisites

- NuGet API key with push permissions.
- `dotnet` SDK 8.x.

## Manual Publish

1. Update package version in `src/OwnershipGuard/OwnershipGuard.csproj` (`<Version>`).
2. Run validation:

```bash
dotnet restore OwnershipGuard.sln
dotnet test OwnershipGuard.sln -c Release
dotnet pack -c Release src/OwnershipGuard/OwnershipGuard.csproj -o artifacts
```

3. Push package and symbols:

```bash
dotnet nuget push "artifacts/*.nupkg" --api-key "<NUGET_API_KEY>" --source "https://api.nuget.org/v3/index.json" --skip-duplicate
dotnet nuget push "artifacts/*.snupkg" --api-key "<NUGET_API_KEY>" --source "https://api.nuget.org/v3/index.json" --skip-duplicate
```

## GitHub Actions Publish

Workflow: `.github/workflows/release-nuget.yml`

- Trigger on tag `v*` (for example: `v0.1.1`) or manually via `workflow_dispatch`.
- Required secret: `NUGET_API_KEY`.
- Workflow validates that tag version matches `<Version>` in `src/OwnershipGuard/OwnershipGuard.csproj`.
