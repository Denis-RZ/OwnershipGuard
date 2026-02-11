using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OwnershipGuard;
using Xunit;

namespace OwnershipGuard.Tests;

public class AccessGuardTests
{
    private static IAccessGuard CreateGuard(bool hideExistence = false)
    {
        var options = Options.Create(new OwnershipGuardOptions { HideExistenceWhenForbidden = hideExistence });
        var registry = new EmptyDescriptorRegistry();
        return new AccessGuard(options, registry);
    }

    private sealed class EmptyDescriptorRegistry : IOwnershipDescriptorRegistry
    {
        public void Register<T>(Func<IServiceProvider, IQueryable<T>> getQuery,
            System.Linq.Expressions.Expression<Func<T, string>> idSelector,
            System.Linq.Expressions.Expression<Func<T, string>> ownerSelector) { }
        public void Register<T>(Func<IServiceProvider, IQueryable<T>> getQuery,
            System.Linq.Expressions.Expression<Func<T, string>> idSelector,
            System.Linq.Expressions.Expression<Func<T, string>> ownerSelector,
            System.Linq.Expressions.Expression<Func<T, string>> tenantSelector) { }
        public void Register<T, TKey>(Func<IServiceProvider, IQueryable<T>> getQuery,
            System.Linq.Expressions.Expression<Func<T, TKey>> idSelector,
            System.Linq.Expressions.Expression<Func<T, string>> ownerSelector,
            System.Linq.Expressions.Expression<Func<T, string>>? tenantSelector)
            where TKey : IParsable<TKey> { }
        public bool TryGetExecutor(Type entityType, out OwnershipCheckExecutor? executor)
        {
            executor = null;
            return false;
        }
        public bool TryGetTenantExecutor(Type entityType, out OwnershipTenantCheckExecutor? executor)
        {
            executor = null;
            return false;
        }
        public OwnershipCheckExecutor GetExecutor(Type entityType) =>
            throw new OwnershipDescriptorNotRegisteredException(entityType, tenantAware: false);
        public OwnershipTenantCheckExecutor GetTenantExecutor(Type entityType) =>
            throw new OwnershipDescriptorNotRegisteredException(entityType, tenantAware: true);
    }

    private static IQueryable<DocumentEntity> CreateQuery(List<DocumentEntity> data)
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        using (var db = new TestDbContext(options))
        {
            db.Documents.AddRange(data);
            db.SaveChanges();
        }
        var ctx = new TestDbContext(options);
        return ctx.Documents.AsQueryable();
    }

    [Fact]
    public async Task IsOwnerAsync_WhenUserOwnsResource_ReturnsTrue()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard();
        var result = await guard.IsOwnerAsync(query, "doc1", "user1", d => d.Id, d => d.OwnerId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOwnerAsync_WhenUserDoesNotOwnResource_ReturnsFalse()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard();
        var result = await guard.IsOwnerAsync(query, "doc1", "user2", d => d.Id, d => d.OwnerId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequireOwnerAsync_WhenUserOwnsResource_ReturnsSuccess()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAsync(query, "doc1", "user1", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.Success);
    }

    /// <summary>Resource truly does not exist => NotFound always (even when HideExistenceWhenForbidden=false).</summary>
    [Fact]
    public async Task RequireOwnerAsync_WhenResourceDoesNotExist_ReturnsNotFound_Always()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAsync(query, "nonexistent", "user1", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.NotFound);
    }

    /// <summary>Exists but not owned => Forbidden in default mode.</summary>
    [Fact]
    public async Task RequireOwnerAsync_WhenExistsButNotOwned_DefaultOptions_ReturnsForbidden()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAsync(query, "doc1", "user2", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.Forbidden);
    }

    /// <summary>Exists but not owned => NotFound in hide-existence mode.</summary>
    [Fact]
    public async Task RequireOwnerAsync_WhenExistsButNotOwned_HideExistence_ReturnsNotFound()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: true);
        var result = await guard.RequireOwnerAsync(query, "doc1", "user2", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.NotFound);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenOwnerAndTenantMatch_ReturnsSuccess()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAndTenantAsync(query, "doc1", "user1", "tenant1", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.Success);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenOwnerMismatch_ReturnsForbidden()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAndTenantAsync(query, "doc1", "user2", "tenant1", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.Forbidden);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenTenantMismatch_ReturnsForbidden()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAndTenantAsync(query, "doc1", "user1", "tenant2", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.Forbidden);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenMismatchAndHideExistence_ReturnsNotFound()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard(hideExistence: true);
        var result = await guard.RequireOwnerAndTenantAsync(query, "doc1", "user1", "tenant2", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.NotFound);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenResourceDoesNotExist_ReturnsNotFound()
    {
        var data = new List<DocumentEntity> { new() { Id = "doc1", OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQuery(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAndTenantAsync(query, "nonexistent", "user1", "tenant1", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.NotFound);
    }

    // --- Typed key (Guid) tests ---

    private static IQueryable<GuidDocumentEntity> CreateQueryGuid(List<GuidDocumentEntity> data)
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<GuidTestDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        using (var db = new GuidTestDbContext(options))
        {
            db.GuidDocuments.AddRange(data);
            db.SaveChanges();
        }
        var ctx = new GuidTestDbContext(options);
        return ctx.GuidDocuments.AsQueryable();
    }

    [Fact]
    public async Task RequireOwnerAsync_WhenGuid_UserOwnsResource_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var data = new List<GuidDocumentEntity> { new() { Id = id, OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQueryGuid(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAsync(query, id, "user1", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.Success);
    }

    [Fact]
    public async Task RequireOwnerAsync_WhenGuid_ResourceDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var data = new List<GuidDocumentEntity> { new() { Id = id, OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQueryGuid(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAsync(query, Guid.NewGuid(), "user1", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.NotFound);
    }

    [Fact]
    public async Task RequireOwnerAsync_WhenGuid_ExistsButNotOwned_ReturnsForbidden()
    {
        var id = Guid.NewGuid();
        var data = new List<GuidDocumentEntity> { new() { Id = id, OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQueryGuid(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAsync(query, id, "user2", d => d.Id, d => d.OwnerId);
        result.Should().Be(RequireOwnerResult.Forbidden);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenGuid_OwnerAndTenantMatch_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var data = new List<GuidDocumentEntity> { new() { Id = id, OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQueryGuid(data);
        var guard = CreateGuard();
        var result = await guard.RequireOwnerAndTenantAsync(query, id, "user1", "tenant1", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.Success);
    }

    [Fact]
    public async Task RequireOwnerAndTenantAsync_WhenGuid_TenantMismatch_ReturnsForbidden()
    {
        var id = Guid.NewGuid();
        var data = new List<GuidDocumentEntity> { new() { Id = id, OwnerId = "user1", TenantId = "tenant1" } };
        var query = CreateQueryGuid(data);
        var guard = CreateGuard(hideExistence: false);
        var result = await guard.RequireOwnerAndTenantAsync(query, id, "user1", "tenant2", d => d.Id, d => d.OwnerId, d => d.TenantId);
        result.Should().Be(RequireOwnerResult.Forbidden);
    }

    private class DocumentEntity
    {
        public string Id { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public string TenantId { get; set; } = "";
    }

    private class GuidDocumentEntity
    {
        public Guid Id { get; set; }
        public string OwnerId { get; set; } = "";
        public string TenantId { get; set; } = "";
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    }

    private class GuidTestDbContext : DbContext
    {
        public GuidTestDbContext(DbContextOptions<GuidTestDbContext> options) : base(options) { }
        public DbSet<GuidDocumentEntity> GuidDocuments => Set<GuidDocumentEntity>();
    }
}
