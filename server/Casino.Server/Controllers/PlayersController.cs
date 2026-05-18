using Casino.Server.Models;
using Casino.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerStore _store;

    public PlayersController(IPlayerStore store) => _store = store;

    // POST /api/players  { "name": "Alice" }
    [HttpPost]
    public ActionResult<PlayerDto> Post([FromBody] CreatePlayerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var player = _store.Create(request.Name.Trim());
        return Ok(new PlayerDto(player));
    }

    // GET /api/players/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<PlayerDto> Get(Guid id)
    {
        var player = _store.GetById(id);
        if (player is null) return NotFound();
        return Ok(new PlayerDto(player));
    }
}

public record CreatePlayerRequest(string Name);

public record PlayerDto(Guid Id, string Name, Guid Token)
{
    public PlayerDto(Player p) : this(p.Id, p.Name, p.Token) { }
}
