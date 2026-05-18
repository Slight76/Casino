namespace Casino.Server.Games.Blackjack;

public class BlackjackTable
{
    public string TableId { get; set; }
    public List<Seat> Seats { get; set; } = new();
    public Hand DealerHand { get; set; } = new();
    public Shoe Shoe { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.Betting;
    public int CurrentSeatIndex { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public BlackjackRules Rules { get; set; }
    public List<RoundEvent> LastEvents { get; set; } = new();
    public CancellationTokenSource? TurnTimerCts { get; set; }

    public BlackjackTable(string tableId, BlackjackRules? rules = null)
    {
        TableId = tableId;
        Rules = rules ?? new BlackjackRules();
        Shoe = new Shoe(Rules.DeckCount);
    }

    public Seat? FindSeat(Guid playerId) => Seats.FirstOrDefault(s => s.PlayerId == playerId);
    public bool HasSeat(Guid playerId) => Seats.Any(s => s.PlayerId == playerId);
    public bool IsFull => Seats.Count >= 7;
    public Seat? CurrentSeat => CurrentSeatIndex >= 0 && CurrentSeatIndex < Seats.Count ? Seats[CurrentSeatIndex] : null;
}
