using Casino.Server.Games.Blackjack;
using Casino.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Casino.Server.Services;

public class BlackjackTimerService
{
    private readonly IHubContext<BlackjackHub> _hub;
    private readonly IBlackjackTableStore _tableStore;
    private readonly BlackjackEngine _engine;

    public BlackjackTimerService(
        IHubContext<BlackjackHub> hub,
        IBlackjackTableStore tableStore,
        BlackjackEngine engine)
    {
        _hub = hub;
        _tableStore = tableStore;
        _engine = engine;
    }

    public void ScheduleAutoStand(string tableId, BlackjackTable table)
    {
        table.TurnTimerCts?.Cancel();
        var cts = new CancellationTokenSource();
        table.TurnTimerCts = cts;
        var seat = table.CurrentSeat;
        if (seat is null) return;
        var playerId = seat.PlayerId;
        var timeout = TimeSpan.FromSeconds(table.Rules.TurnTimeoutSeconds);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(timeout, cts.Token);
                if (cts.Token.IsCancellationRequested) return;
                await HandleAutoStand(tableId, playerId);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BlackjackTimer] {ex}");
            }
        });
    }

    public void CancelTimer(BlackjackTable table)
    {
        table.TurnTimerCts?.Cancel();
        table.TurnTimerCts = null;
    }

    private async Task HandleAutoStand(string tableId, Guid playerId)
    {
        var table = _tableStore.Get(tableId);
        if (table is null) return;

        bool wentToDealer = false;
        lock (table)
        {
            if (table.Phase != GamePhase.PlayerTurn) return;
            if (table.CurrentSeat?.PlayerId != playerId) return;
            try
            {
                _engine.Stand(table, playerId);
                table.LastEvents.Insert(0, new RoundEvent(RoundEventType.AutoStand, playerId.ToString(), table.FindSeat(playerId)?.PlayerName));
            }
            catch
            {
                return;
            }
            wentToDealer = table.Phase == GamePhase.DealerTurn;
        }

        await BroadcastState(tableId, table);

        if (wentToDealer)
        {
            await HandleDealerAndSettle(tableId, table);
        }
        else
        {
            lock (table)
            {
                if (table.Phase == GamePhase.PlayerTurn)
                    ScheduleAutoStand(tableId, table);
            }
        }
    }

    public async Task HandleDealerAndSettle(string tableId, BlackjackTable table)
    {
        lock (table)
        {
            if (table.Phase != GamePhase.DealerTurn) return;
            _engine.PlayDealer(table);
            _engine.Settle(table);
        }

        await BroadcastState(tableId, table);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                lock (table)
                {
                    if (table.Phase != GamePhase.Settling) return;
                    _engine.ResetForNextRound(table);
                }
                await BroadcastState(tableId, table);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BlackjackTimer] reset error: {ex}");
            }
        });
    }

    public Task BroadcastState(string tableId, BlackjackTable table)
    {
        TableStateDto dto;
        lock (table)
        {
            dto = TableStateMapper.BuildTableState(table);
        }
        return _hub.Clients.Group($"blackjack:{tableId}").SendAsync("TableState", dto);
    }
}
