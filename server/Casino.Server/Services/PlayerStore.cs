using System.Collections.Concurrent;
using Casino.Server.Models;

namespace Casino.Server.Services;

public class PlayerStore : IPlayerStore
{
    private readonly ConcurrentDictionary<Guid, Player> _byId = new();
    private readonly ConcurrentDictionary<Guid, Player> _byToken = new();

    public Player Create(string name)
    {
        var player = new Player { Id = Guid.NewGuid(), Name = name, Token = Guid.NewGuid() };
        _byId[player.Id] = player;
        _byToken[player.Token] = player;
        return player;
    }

    public Player? GetById(Guid id) => _byId.TryGetValue(id, out var p) ? p : null;

    public Player? GetByToken(Guid token) => _byToken.TryGetValue(token, out var p) ? p : null;
}
