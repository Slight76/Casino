using Casino.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace Casino.Server.Hubs;

public class LobbyHub : Hub
{
    private readonly ITableStore _tableStore;

    public LobbyHub(ITableStore tableStore) => _tableStore = tableStore;

    /// <summary>Returns a snapshot of all tables across all games.</summary>
    public Task<IReadOnlyList<TableInfo>> ListTables()
    {
        var games = new[] { "blackjack", "poker", "roulette" };
        var result = games
            .SelectMany(game =>
                _tableStore.GetTables(game)
                           .Select(tableId => new TableInfo(
                               game,
                               tableId,
                               _tableStore.GetPlayers(game, tableId).Count)))
            .ToList();

        return Task.FromResult<IReadOnlyList<TableInfo>>(result);
    }
}

public record TableInfo(string Game, string TableId, int PlayerCount);
