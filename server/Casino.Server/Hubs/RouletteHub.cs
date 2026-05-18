using Casino.Server.Services;

namespace Casino.Server.Hubs;

public class RouletteHub : GameHubBase
{
    protected override string GameName => "roulette";

    public RouletteHub(IPlayerStore playerStore, ITableStore tableStore)
        : base(playerStore, tableStore) { }

    // TODO: Add roulette-specific hub methods (placeBet, spin, etc.) here
}
