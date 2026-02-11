using FluentAssertions;
using OwnershipGuard.EntityFrameworkCore;
using Xunit;

namespace OwnershipGuard.Tests;

public class QueryableExtensionsTests
{
    [Fact]
    public void WhereOwnedBy_FiltersToSingleOwner()
    {
        var items = new List<Item>
        {
            new Item { Id = 1, OwnerId = "user1", Name = "A" },
            new Item { Id = 2, OwnerId = "user2", Name = "B" },
            new Item { Id = 3, OwnerId = "user1", Name = "C" }
        };
        var query = items.AsQueryable().WhereOwnedBy("user1", i => i.OwnerId);
        var result = query.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.OwnerId == "user1");
        result.Select(i => i.Name).Should().BeEquivalentTo("A", "C");
    }

    [Fact]
    public void WhereOwnedBy_WhenNoMatch_ReturnsEmpty()
    {
        var items = new List<Item>
        {
            new Item { Id = 1, OwnerId = "user1" },
            new Item { Id = 2, OwnerId = "user2" }
        };
        var query = items.AsQueryable().WhereOwnedBy("user3", i => i.OwnerId);
        query.ToList().Should().BeEmpty();
    }

    [Fact]
    public void WhereTenant_FiltersToSingleTenant()
    {
        var items = new List<TenantItem>
        {
            new TenantItem { Id = 1, TenantId = "tenant1", Name = "A" },
            new TenantItem { Id = 2, TenantId = "tenant2", Name = "B" },
            new TenantItem { Id = 3, TenantId = "tenant1", Name = "C" }
        };
        var query = items.AsQueryable().WhereTenant("tenant1", i => i.TenantId);
        var result = query.ToList();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.TenantId == "tenant1");
        result.Select(i => i.Name).Should().BeEquivalentTo("A", "C");
    }

    [Fact]
    public void WhereTenant_WhenNoMatch_ReturnsEmpty()
    {
        var items = new List<TenantItem>
        {
            new TenantItem { Id = 1, TenantId = "tenant1" },
            new TenantItem { Id = 2, TenantId = "tenant2" }
        };
        var query = items.AsQueryable().WhereTenant("tenant3", i => i.TenantId);
        query.ToList().Should().BeEmpty();
    }

    [Fact]
    public void WhereOwnedBy_ThenWhereTenant_CombinesCorrectly()
    {
        var items = new List<TenantItem>
        {
            new TenantItem { Id = 1, OwnerId = "user1", TenantId = "tenant1" },
            new TenantItem { Id = 2, OwnerId = "user1", TenantId = "tenant2" },
            new TenantItem { Id = 3, OwnerId = "user2", TenantId = "tenant1" }
        };
        var query = items.AsQueryable()
            .WhereOwnedBy("user1", i => i.OwnerId)
            .WhereTenant("tenant1", i => i.TenantId);
        var result = query.ToList();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    private class Item
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = "";
        public string Name { get; set; } = "";
    }

    private class TenantItem
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
