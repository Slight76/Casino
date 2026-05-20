namespace Casino.Server.Games.Blackjack;

public enum RoundEventType
{
    BetPlaced,
    RoundStarted,
    CardDealt,
    PlayerHit,
    PlayerStood,
    PlayerDoubled,
    PlayerSplit,
    PlayerBust,
    PlayerBlackjack,
    DealerTurn,
    DealerHit,
    DealerStood,
    RoundSettled,
    TurnChanged,
    PhaseChanged,
    AutoStand
}

public record RoundEvent(RoundEventType Type, string? PlayerId = null, string? PlayerName = null, string? Message = null);
