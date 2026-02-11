using Microsoft.AspNetCore.Mvc;
using OwnershipGuard;

namespace OwnershipGuard.DemoApi;

[ApiController]
[Route("mvc/notes")]
[RequireOwnership("id", typeof(Note))]
public sealed class NotesController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotesController(AppDbContext db) => _db = db;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var guid = Guid.Parse(id);
        var note = await _db.Notes.FindAsync(guid);
        return note is null ? NotFound() : Ok(note);
    }
}
