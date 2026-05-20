using Casino.Server.Games.Blackjack;

namespace Casino.Server.Services;

public interface IBlackjackTableStore
{
    BlackjackTable GetOrCreate(string tableId, BlackjackRules? rules = null);
    BlackjackTable? Get(string tableId);
    IReadOnlyList<BlackjackTable> GetAll();
    void Remove(string tableId);
}
