# OwnershipGuard

[![NuGet](https://img.shields.io/nuget/v/OwnershipGuard.svg)](https://www.nuget.org/packages/OwnershipGuard/)

OwnershipGuard is an ASP.NET Core library that enforces ownership checks and optional tenant checks before endpoint or MVC action execution. It centralizes resource access validation by route id and helps prevent IDOR and broken access control caused by missing per-endpoint checks.

## Requirements

- .NET 8
- ASP.NET Core 8

## Installation

```bash
dotnet add package OwnershipGuard
```

```xml
<PackageReference Include="OwnershipGuard" Version="x.y.z" />
```

## Quick Start

1. Register services.

```csharp
using System.Security.Claims;

builder.Services.AddOwnershipGuard(options =>
{
    options.UserIdClaimType = ClaimTypes.NameIdentifier;
    options.TenantIdClaimType = "tenant_id";
    options.UseProblemDetailsResponses = true;
    options.HideExistenceWhenForbidden = false;
});
```

2. Register descriptors after `builder.Build()`.

```csharp
var app = builder.Build();
var registry = app.Services.GetRequiredService<IOwnershipDescriptorRegistry>();

// String key + ownership check
registry.Register<Note>(
    sp => sp.GetRequiredService<YourDbContext>().Notes,
    n => n.Id,
    n => n.OwnerId);

// Typed key (Guid) + ownership + tenant check
registry.Register<Document, Guid>(
    sp => sp.GetRequiredService<YourDbContext>().Documents,
    d => d.Id,
    d => d.OwnerId,
    tenantSelector: d => d.TenantId);
```

3. Apply the check to endpoints.

Minimal API:

```csharp
var documentOwnership = new RequireOwnershipFilter("id", typeof(Document));
app.MapGet("/documents/{id}", ...).AddEndpointFilter(documentOwnership);
app.MapPut("/documents/{id}", ...).AddEndpointFilter(documentOwnership);
```

MVC:

```csharp
[ApiController]
[Route("documents")]
[RequireOwnership("id", typeof(Document))]
public sealed class DocumentsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(string id) => Ok();
}
```

## Response Behavior

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | Route id is missing or empty, or typed key parsing fails (for `Register<T, TKey>`). |
| `401 Unauthorized` | User claim is missing, or tenant claim is missing when a tenant-aware descriptor is used. |
| `403 Forbidden` | Resource exists but ownership or tenant check fails, and `HideExistenceWhenForbidden` is `false`. |
| `404 Not Found` | Resource does not exist, or ownership/tenant check fails when `HideExistenceWhenForbidden` is `true`. |
| `500 Internal Server Error` | Descriptor is not registered for the requested entity type. |

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `UserIdClaimType` | `ClaimTypes.NameIdentifier` | Claim type used to resolve the current user id. |
| `TenantIdClaimType` | `"tenant_id"` | Claim type used to resolve the current tenant id when tenant checks are required. |
| `UseProblemDetailsResponses` | `true` | When `true`, filters return ProblemDetails payloads for errors. When `false`, filters return plain status responses. |
| `HideExistenceWhenForbidden` | `false` | When `true`, failed ownership/tenant checks return `404`; otherwise they return `403`. |

## Optional Query Helpers

```csharp
using OwnershipGuard.EntityFrameworkCore;

var ownedDocs = await db.Documents
    .WhereOwnedBy(userId, d => d.OwnerId)
    .ToListAsync();

var tenantDocs = await db.Documents
    .WhereTenant(tenantId, d => d.TenantId)
    .ToListAsync();
```

## Non-Goals

OwnershipGuard does not implement:

- Authentication
- Authorization policy systems (RBAC)
- Rate limiting
- Input validation

## License

MIT. See [LICENSE](https://github.com/ownershipguard/ownershipguard/blob/main/LICENSE).
