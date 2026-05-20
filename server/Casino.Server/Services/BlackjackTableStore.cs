using System.Collections.Concurrent;
using Casino.Server.Games.Blackjack;

namespace Casino.Server.Services;

public class BlackjackTableStore : IBlackjackTableStore
{
    private readonly ConcurrentDictionary<string, BlackjackTable> _tables = new();

    public BlackjackTable GetOrCreate(string tableId, BlackjackRules? rules = null) =>
        _tables.GetOrAdd(tableId, id => new BlackjackTable(id, rules));

    public BlackjackTable? Get(string tableId) =>
        _tables.TryGetValue(tableId, out var t) ? t : null;

    public IReadOnlyList<BlackjackTable> GetAll() => _tables.Values.ToList();

    public void Remove(string tableId) => _tables.TryRemove(tableId, out _);
}
