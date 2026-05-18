using Casino.Server.Services;

namespace Casino.Server.Hubs;

public class PokerHub : GameHubBase
{
    protected override string GameName => "poker";

    public PokerHub(IPlayerStore playerStore, ITableStore tableStore)
        : base(playerStore, tableStore) { }

    // TODO: Add poker-specific hub methods (deal, bet, fold, raise, etc.) here
}
