using Microsoft.AspNetCore.Mvc;
using OwnershipGuard;

namespace OwnershipGuard.DemoApi;

[ApiController]
[Route("mvc/documents")]
[RequireOwnership("id", typeof(Document))]
public sealed class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DocumentsController(AppDbContext db) => _db = db;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var guid = Guid.Parse(id);
        var doc = await _db.Documents.FindAsync(guid);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, DocumentUpdate input)
    {
        var guid = Guid.Parse(id);
        var doc = await _db.Documents.FindAsync(guid);
        if (doc is null) return NotFound();
        doc.Title = input.Title ?? doc.Title;
        doc.Content = input.Content ?? doc.Content;
        await _db.SaveChangesAsync();
        return Ok(doc);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var guid = Guid.Parse(id);
        var doc = await _db.Documents.FindAsync(guid);
        if (doc is null) return NotFound();
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
