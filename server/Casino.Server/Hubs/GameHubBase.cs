using Casino.Server.Models;
using Casino.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace Casino.Server.Hubs;

/// <summary>
/// Shared helper that game hubs inherit to resolve the calling player from the
/// access_token query-string parameter and manage table membership.
/// </summary>
public abstract class GameHubBase : Hub
{
    protected readonly IPlayerStore PlayerStore;
    protected readonly ITableStore TableStore;
    protected abstract string GameName { get; }

    protected GameHubBase(IPlayerStore playerStore, ITableStore tableStore)
    {
        PlayerStore = playerStore;
        TableStore = tableStore;
    }

    protected Player RequirePlayer()
    {
        var raw = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
        if (Guid.TryParse(raw, out var token))
        {
            var player = PlayerStore.GetByToken(token);
            if (player is not null) return player;
        }
        throw new HubException("unauthenticated");
    }

    public async Task JoinTable(string tableId)
    {
        var player = RequirePlayer();
        TableStore.AddPlayer(GameName, tableId, player.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{GameName}:{tableId}");
        await Clients.Group($"{GameName}:{tableId}")
                     .SendAsync("PlayerJoined", player.Name, tableId);
    }

    public async Task LeaveTable(string tableId)
    {
        var player = RequirePlayer();
        TableStore.RemovePlayer(GameName, tableId, player.Id);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{GameName}:{tableId}");
        await Clients.Group($"{GameName}:{tableId}")
                     .SendAsync("PlayerLeft", player.Name, tableId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Best-effort: remove player from all their tables in this game
        try
        {
            var raw = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
            if (Guid.TryParse(raw, out var token))
            {
                var player = PlayerStore.GetByToken(token);
                if (player is not null)
                {
                    foreach (var table in TableStore.GetTables(GameName))
                    {
                        TableStore.RemovePlayer(GameName, table, player.Id);
                        await Clients.Group($"{GameName}:{table}")
                                     .SendAsync("PlayerLeft", player.Name, table);
                    }
                }
            }
        }
        catch { /* swallow — disconnect must not throw */ }

        await base.OnDisconnectedAsync(exception);
    }
}
