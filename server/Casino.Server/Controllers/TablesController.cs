using Casino.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Server.Controllers;

[ApiController]
[Route("api/tables")]
public class TablesController : ControllerBase
{
    [HttpPost("blackjack")]
    public ActionResult<CreateTableResponse> CreateBlackjackTable()
    {
        var tableId = Guid.NewGuid().ToString("N")[..8];
        return Ok(new CreateTableResponse(tableId));
    }

    [HttpGet("blackjack")]
    public ActionResult<IReadOnlyList<BlackjackTableSummary>> ListBlackjackTables(
        [FromServices] IBlackjackTableStore store)
    {
        var list = store.GetAll()
            .Select(t => new BlackjackTableSummary(t.TableId, t.Seats.Count, t.Phase.ToString()))
            .ToList();
        return Ok(list);
    }
}

public record CreateTableResponse(string TableId);
public record BlackjackTableSummary(string TableId, int PlayerCount, string Phase);
