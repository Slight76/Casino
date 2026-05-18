namespace Casino.Server.Games.Blackjack;

/// <summary>Configurable rule set. Defaults = standard Las Vegas Strip. Confirm with product before launch.</summary>
public record BlackjackRules(
    int DeckCount = 6,
    bool DealerHitsSoft17 = true,
    decimal BlackjackPayout = 1.5m,
    bool AllowDouble = true,
    bool AllowSplit = true,
    int TurnTimeoutSeconds = 30,
    int StartingChips = 1000,
    int MinBet = 10,
    int MaxBet = 500);
