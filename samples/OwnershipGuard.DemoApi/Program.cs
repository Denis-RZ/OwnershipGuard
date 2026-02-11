using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OwnershipGuard;
using OwnershipGuard.DemoApi;

var builder = WebApplication.CreateBuilder(args);

var keepAliveConnection = new SqliteConnection("Data Source=:memory:");
keepAliveConnection.Open();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(keepAliveConnection));
builder.Services.AddOwnershipGuard();
builder.Services.AddControllers();

var app = builder.Build();

var registry = app.Services.GetRequiredService<IOwnershipDescriptorRegistry>();
registry.Register<Document, Guid>(
    sp => sp.GetRequiredService<AppDbContext>().Documents,
    d => d.Id,
    d => d.OwnerId,
    tenantSelector: d => d.TenantId);

registry.Register<Note, Guid>(
    sp => sp.GetRequiredService<AppDbContext>().Notes,
    n => n.Id,
    n => n.OwnerId);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Documents.Any())
    {
        db.Documents.AddRange(
            new Document { Id = SeedIds.Doc1Id, OwnerId = "user1", TenantId = "tenant1", Title = "User 1 Doc", Content = "Content 1" },
            new Document { Id = SeedIds.Doc2Id, OwnerId = "user2", TenantId = "tenant2", Title = "User 2 Doc", Content = "Content 2" });
        db.SaveChanges();
    }

    if (!db.Notes.Any())
    {
        db.Notes.AddRange(
            new Note { Id = SeedIds.Note1Id, OwnerId = "user1", Title = "User 1 Note" },
            new Note { Id = SeedIds.Note2Id, OwnerId = "user2", Title = "User 2 Note" });
        db.SaveChanges();
    }
}

app.UseMiddleware<FakeUserMiddleware>();
app.MapControllers();

var ownershipFilter = new RequireOwnershipFilter("id", typeof(Document));

app.MapGet("/documents/{id}", async (string id, AppDbContext db) =>
{
    var guid = Guid.Parse(id);
    var doc = await db.Documents.FindAsync(guid);
    return doc is null ? Results.NotFound() : Results.Ok(doc);
})
.AddEndpointFilter(ownershipFilter);

app.MapPut("/documents/{id}", async (string id, DocumentUpdate input, AppDbContext db) =>
{
    var guid = Guid.Parse(id);
    var doc = await db.Documents.FindAsync(guid);
    if (doc is null) return Results.NotFound();
    doc.Title = input.Title ?? doc.Title;
    doc.Content = input.Content ?? doc.Content;
    await db.SaveChangesAsync();
    return Results.Ok(doc);
})
.AddEndpointFilter(ownershipFilter);

app.MapDelete("/documents/{id}", async (string id, AppDbContext db) =>
{
    var guid = Guid.Parse(id);
    var doc = await db.Documents.FindAsync(guid);
    if (doc is null) return Results.NotFound();
    db.Documents.Remove(doc);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.AddEndpointFilter(ownershipFilter);

var noteOwnershipFilter = new RequireOwnershipFilter("id", typeof(Note));
app.MapGet("/notes/{id}", async (string id, AppDbContext db) =>
{
    var guid = Guid.Parse(id);
    var note = await db.Notes.FindAsync(guid);
    return note is null ? Results.NotFound() : Results.Ok(note);
})
.AddEndpointFilter(noteOwnershipFilter);

app.Run();

public partial class Program { }

public record DocumentUpdate(string? Title, string? Content);
