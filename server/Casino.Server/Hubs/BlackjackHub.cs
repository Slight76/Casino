using Casino.Server.Services;

namespace Casino.Server.Hubs;

public class BlackjackHub : GameHubBase
{
    protected override string GameName => "blackjack";

    public BlackjackHub(IPlayerStore playerStore, ITableStore tableStore)
        : base(playerStore, tableStore) { }

    // TODO: Add blackjack-specific hub methods (deal, hit, stand, etc.) here
}
