namespace Casino.Server.Games.Blackjack;

/// <summary>Configurable rule set. Defaults confirmed for v1 (issue #8).</summary>
public record BlackjackRules(
    int DeckCount = 6,
    bool DealerHitsSoft17 = true,
    decimal BlackjackPayout = 1.5m,
    bool AllowDouble = true,
    bool AllowSplit = true,
    int MaxSplits = 1,                    // one re-split allowed (max 2 hands)
    bool SplitBlackjackPays1to1 = true,  // split-21 pays even money, not 3:2
    bool SplitAcesOneCardOnly = true,    // split aces receive exactly one card each; no further action
    bool OfferInsurance = false,         // insurance not offered
    int TurnTimeoutSeconds = 30,
    int StartingChips = 1000,
    int MinBet = 10,
    int MaxBet = 500);
