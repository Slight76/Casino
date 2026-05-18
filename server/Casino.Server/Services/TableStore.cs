using System.Collections.Concurrent;

namespace Casino.Server.Services;

public class TableStore : ITableStore
{
    // key: "game:tableId"  value: set of playerIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _tables = new();

    private static string Key(string game, string tableId) => $"{game}:{tableId}";

    public void AddPlayer(string game, string tableId, Guid playerId)
    {
        var set = _tables.GetOrAdd(Key(game, tableId), _ => new ConcurrentDictionary<Guid, byte>());
        set[playerId] = 0;
    }

    public void RemovePlayer(string game, string tableId, Guid playerId)
    {
        if (_tables.TryGetValue(Key(game, tableId), out var set))
            set.TryRemove(playerId, out _);
    }

    public IReadOnlyList<Guid> GetPlayers(string game, string tableId)
    {
        if (_tables.TryGetValue(Key(game, tableId), out var set))
            return set.Keys.ToList();
        return [];
    }

    public IReadOnlyList<string> GetTables(string game) =>
        _tables.Keys
               .Where(k => k.StartsWith(game + ":"))
               .Select(k => k[(game.Length + 1)..])
               .ToList();

    public IReadOnlyList<(string Game, string TableId)> GetTablesForPlayer(Guid playerId) =>
        _tables.Where(kv => kv.Value.ContainsKey(playerId))
               .Select(kv =>
               {
                   var parts = kv.Key.Split(':', 2);
                   return (parts[0], parts[1]);
               })
               .ToList();
}
