namespace Casino.Server.Games.Blackjack;

public record CardDto(string Rank, string Suit, string SuitSymbol, string Display, bool IsHidden);

public record HandDto(
    IReadOnlyList<CardDto> Cards,
    int Total,
    bool IsBlackjack,
    bool IsBust,
    bool IsStood,
    bool IsDone,
    bool IsSoft,
    bool CanSplit,
    bool CanDouble,
    string? Result,
    int Bet,
    int Winnings);

public record SeatDto(
    string PlayerId,
    string PlayerName,
    int Chips,
    IReadOnlyList<HandDto> Hands,
    int CurrentHandIndex,
    int Bet,
    bool HasActed,
    bool IsCurrentSeat);

public record DealerHandDto(IReadOnlyList<CardDto> Cards, int Total);

public record RoundEventDto(string Type, string? PlayerId, string? PlayerName, string? Message);

public record TableStateDto(
    string TableId,
    string Phase,
    DealerHandDto DealerHand,
    IReadOnlyList<SeatDto> Seats,
    int CurrentSeatIndex,
    string? TurnDeadline,
    IReadOnlyList<RoundEventDto> LastEvents);

public static class TableStateMapper
{
    public static TableStateDto BuildTableState(BlackjackTable table)
    {
        bool hideHoleCard = table.Phase == GamePhase.PlayerTurn || table.Phase == GamePhase.Dealing;

        var dealerCards = new List<CardDto>();
        for (int i = 0; i < table.DealerHand.Cards.Count; i++)
        {
            var c = table.DealerHand.Cards[i];
            bool hidden = hideHoleCard && i == 1;
            dealerCards.Add(new CardDto(
                hidden ? "?" : c.RankLabel,
                hidden ? "?" : c.Suit.ToString(),
                hidden ? "?" : c.SuitSymbol,
                hidden ? "??" : c.Display,
                hidden));
        }

        int dealerTotal;
        if (hideHoleCard && table.DealerHand.Cards.Count >= 1)
        {
            // only count upcard
            dealerTotal = table.DealerHand.Cards[0].Value;
        }
        else
        {
            dealerTotal = table.DealerHand.BestTotal();
        }
        var dealerDto = new DealerHandDto(dealerCards, dealerTotal);

        var seatDtos = new List<SeatDto>();
        for (int i = 0; i < table.Seats.Count; i++)
        {
            var seat = table.Seats[i];
            var handDtos = seat.Hands.Select(h => new HandDto(
                h.Cards.Select(c => new CardDto(c.RankLabel, c.Suit.ToString(), c.SuitSymbol, c.Display, false)).ToList(),
                h.BestTotal(),
                h.IsBlackjack,
                h.IsBust,
                h.IsStood,
                h.IsDone,
                h.IsSoft,
                h.CanSplit,
                h.CanDouble,
                h.Result?.ToString(),
                h.Bet,
                h.Winnings)).ToList();

            seatDtos.Add(new SeatDto(
                seat.PlayerId.ToString(),
                seat.PlayerName,
                seat.Chips,
                handDtos,
                seat.CurrentHandIndex,
                seat.Bet,
                seat.HasActed,
                i == table.CurrentSeatIndex && table.Phase == GamePhase.PlayerTurn));
        }

        var eventDtos = table.LastEvents
            .Select(e => new RoundEventDto(e.Type.ToString(), e.PlayerId, e.PlayerName, e.Message))
            .ToList();

        return new TableStateDto(
            table.TableId,
            table.Phase.ToString(),
            dealerDto,
            seatDtos,
            table.CurrentSeatIndex,
            table.TurnDeadline?.ToString("o"),
            eventDtos);
    }
}
