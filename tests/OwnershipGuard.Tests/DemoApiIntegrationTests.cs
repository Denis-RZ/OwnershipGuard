using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OwnershipGuard.DemoApi;
using Xunit;

namespace OwnershipGuard.Tests;

public class DemoApiIntegrationTests
{
    private static string Doc1IdStr => SeedIds.Doc1Id.ToString();
    private static string Doc2IdStr => SeedIds.Doc2Id.ToString();

    [Fact]
    public async Task User1_GetDoc1_Returns200()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/documents/{Doc1IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User1_GetDoc2_Returns403()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/documents/{Doc2IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User2_DeleteDoc2_Returns204()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user2");
        var response = await client.DeleteAsync($"/documents/{Doc2IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task User1_PutDoc2_Returns403()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.PutAsync($"/documents/{Doc2IdStr}",
            new StringContent(JsonSerializer.Serialize(new { Title = "Hacked", Content = "No" }), System.Text.Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoXUser_DefaultsToUser1_CanGetDoc1()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/documents/{Doc1IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>GET nonexistent guid => 404 always.</summary>
    [Fact]
    public async Task Get_NonexistentGuid_Returns404()
    {
        var nonexistent = Guid.Parse("99999999-9999-9999-9999-999999999999");
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/documents/{nonexistent}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>GET not-a-guid => 400.</summary>
    [Fact]
    public async Task Get_NotAGuid_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync("/documents/not-a-guid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task User2_WithWrongTenant_GetDoc2_Returns403()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user2");
        client.DefaultRequestHeaders.Add("X-Tenant", "tenant1");
        var response = await client.GetAsync($"/documents/{Doc2IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mvc_User1_GetDoc1_Returns200()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/mvc/documents/{Doc1IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Mvc_User1_GetDoc2_Returns403()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/mvc/documents/{Doc2IdStr}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Mvc_Get_NotAGuid_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync("/mvc/documents/not-a-guid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public class DemoApiHideExistenceFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<OwnershipGuard.OwnershipGuardOptions>(o => o.HideExistenceWhenForbidden = true);
        });
    }
}

public class DemoApiHideExistenceTests
{
    [Fact]
    public async Task User1_GetDoc2_WhenHideExistence_Returns404()
    {
        using var factory = new DemoApiHideExistenceFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/documents/{SeedIds.Doc2Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task User2_WithWrongTenant_GetDoc2_WhenHideExistence_Returns404()
    {
        using var factory = new DemoApiHideExistenceFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user2");
        client.DefaultRequestHeaders.Add("X-Tenant", "tenant1");
        var response = await client.GetAsync($"/documents/{SeedIds.Doc2Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class DemoApiMissingTenantClaimFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // FakeUserMiddleware always emits "tenant_id". Override the options to look for a different claim type.
            services.Configure<OwnershipGuard.OwnershipGuardOptions>(o => o.TenantIdClaimType = "missing_tenant");
        });
    }
}

public class DemoApiMissingTenantClaimTests
{
    [Fact]
    public async Task Get_WhenTenantClaimMissing_Returns401()
    {
        using var factory = new DemoApiMissingTenantClaimFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/documents/{SeedIds.Doc1Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Note_WhenTenantClaimMissing_IsNotRequired_Returns200()
    {
        using var factory = new DemoApiMissingTenantClaimFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/notes/{SeedIds.Note1Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Mvc_Get_Note_WhenTenantClaimMissing_IsNotRequired_Returns200()
    {
        using var factory = new DemoApiMissingTenantClaimFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", "user1");
        var response = await client.GetAsync($"/mvc/notes/{SeedIds.Note1Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
