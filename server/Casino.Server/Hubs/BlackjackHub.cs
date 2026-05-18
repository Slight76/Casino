using Casino.Server.Games.Blackjack;
using Casino.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace Casino.Server.Hubs;

public class BlackjackHub : GameHubBase
{
    private readonly IBlackjackTableStore _blackjackTables;
    private readonly BlackjackEngine _engine;
    private readonly BlackjackTimerService _timerService;

    protected override string GameName => "blackjack";

    public BlackjackHub(
        IPlayerStore playerStore,
        ITableStore tableStore,
        IBlackjackTableStore blackjackTables,
        BlackjackEngine engine,
        BlackjackTimerService timerService)
        : base(playerStore, tableStore)
    {
        _blackjackTables = blackjackTables;
        _engine = engine;
        _timerService = timerService;
    }

    public new async Task JoinTable(string tableId)
    {
        var player = RequirePlayer();
        var table = _blackjackTables.GetOrCreate(tableId);

        lock (table)
        {
            if (!table.HasSeat(player.Id))
            {
                if (table.Phase != GamePhase.Betting)
                    throw new HubException("Cannot join mid-round; please wait until the next betting phase.");
                if (table.IsFull)
                    throw new HubException("Table is full.");

                table.Seats.Add(new Seat
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    Chips = table.Rules.StartingChips,
                });
            }
        }

        TableStore.AddPlayer(GameName, tableId, player.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{GameName}:{tableId}");
        await BroadcastTableState(tableId, table);
    }

    public new async Task LeaveTable(string tableId)
    {
        var player = RequirePlayer();
        var table = _blackjackTables.Get(tableId);
        if (table is not null)
        {
            lock (table)
            {
                if (table.Phase == GamePhase.Betting || table.Phase == GamePhase.Settling)
                {
                    var seat = table.FindSeat(player.Id);
                    if (seat is not null) table.Seats.Remove(seat);
                }
            }
        }

        TableStore.RemovePlayer(GameName, tableId, player.Id);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{GameName}:{tableId}");

        if (table is not null) await BroadcastTableState(tableId, table);
    }

    public async Task PlaceBet(string tableId, int amount)
    {
        var player = RequirePlayer();
        var table = RequireTable(tableId);

        bool startRound = false;
        lock (table)
        {
            try { _engine.PlaceBet(table, player.Id, amount); }
            catch (Exception ex) { throw new HubException(ex.Message); }

            var seated = table.Seats;
            if (seated.Count > 0 && seated.All(s => s.Bet > 0))
                startRound = true;
        }

        if (startRound)
        {
            lock (table)
            {
                try { _engine.StartRound(table); }
                catch (Exception ex) { throw new HubException(ex.Message); }
            }
        }

        await BroadcastTableState(tableId, table);

        if (table.Phase == GamePhase.DealerTurn)
        {
            await _timerService.HandleDealerAndSettle(tableId, table);
        }
        else if (table.Phase == GamePhase.PlayerTurn)
        {
            _timerService.ScheduleAutoStand(tableId, table);
        }
    }

    public async Task Hit(string tableId)
    {
        var player = RequirePlayer();
        var table = RequireTable(tableId);
        lock (table)
        {
            _timerService.CancelTimer(table);
            try { _engine.Hit(table, player.Id); }
            catch (Exception ex) { throw new HubException(ex.Message); }
        }
        await BroadcastTableState(tableId, table);
        await HandlePostAction(tableId, table);
    }

    public async Task Stand(string tableId)
    {
        var player = RequirePlayer();
        var table = RequireTable(tableId);
        lock (table)
        {
            _timerService.CancelTimer(table);
            try { _engine.Stand(table, player.Id); }
            catch (Exception ex) { throw new HubException(ex.Message); }
        }
        await BroadcastTableState(tableId, table);
        await HandlePostAction(tableId, table);
    }

    public async Task Double(string tableId)
    {
        var player = RequirePlayer();
        var table = RequireTable(tableId);
        lock (table)
        {
            _timerService.CancelTimer(table);
            try { _engine.Double(table, player.Id); }
            catch (Exception ex) { throw new HubException(ex.Message); }
        }
        await BroadcastTableState(tableId, table);
        await HandlePostAction(tableId, table);
    }

    public async Task Split(string tableId)
    {
        var player = RequirePlayer();
        var table = RequireTable(tableId);
        lock (table)
        {
            _timerService.CancelTimer(table);
            try { _engine.Split(table, player.Id); }
            catch (Exception ex) { throw new HubException(ex.Message); }
        }
        await BroadcastTableState(tableId, table);
        await HandlePostAction(tableId, table);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var raw = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
            if (Guid.TryParse(raw, out var token))
            {
                var player = PlayerStore.GetByToken(token);
                if (player is not null)
                {
                    foreach (var table in _blackjackTables.GetAll())
                    {
                        bool removed = false;
                        lock (table)
                        {
                            if ((table.Phase == GamePhase.Betting || table.Phase == GamePhase.Settling)
                                && table.HasSeat(player.Id))
                            {
                                var seat = table.FindSeat(player.Id);
                                if (seat is not null)
                                {
                                    table.Seats.Remove(seat);
                                    removed = true;
                                }
                            }
                        }
                        if (removed)
                        {
                            TableStore.RemovePlayer(GameName, table.TableId, player.Id);
                            await BroadcastTableState(table.TableId, table);
                        }
                    }
                }
            }
        }
        catch { /* swallow */ }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task HandlePostAction(string tableId, BlackjackTable table)
    {
        if (table.Phase == GamePhase.DealerTurn)
        {
            await _timerService.HandleDealerAndSettle(tableId, table);
        }
        else if (table.Phase == GamePhase.PlayerTurn)
        {
            _timerService.ScheduleAutoStand(tableId, table);
        }
    }

    private BlackjackTable RequireTable(string tableId) =>
        _blackjackTables.Get(tableId) ?? throw new HubException("Table not found.");

    private async Task BroadcastTableState(string tableId, BlackjackTable table)
    {
        TableStateDto dto;
        lock (table)
        {
            dto = TableStateMapper.BuildTableState(table);
        }
        await Clients.Group($"{GameName}:{tableId}").SendAsync("TableState", dto);
    }
}
