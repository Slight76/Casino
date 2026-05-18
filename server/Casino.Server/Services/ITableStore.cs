namespace Casino.Server.Services;

public interface ITableStore
{
    void AddPlayer(string game, string tableId, Guid playerId);
    void RemovePlayer(string game, string tableId, Guid playerId);
    IReadOnlyList<Guid> GetPlayers(string game, string tableId);
    IReadOnlyList<string> GetTables(string game);

    /// <summary>Returns every (game, tableId) pair the player is currently seated at.</summary>
    IReadOnlyList<(string Game, string TableId)> GetTablesForPlayer(Guid playerId);
}
