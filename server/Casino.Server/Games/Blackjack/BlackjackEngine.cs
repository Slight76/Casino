namespace Casino.Server.Games.Blackjack;

public class BlackjackEngine
{
    public List<RoundEvent> PlaceBet(BlackjackTable table, Guid playerId, int amount)
    {
        if (table.Phase != GamePhase.Betting)
            throw new InvalidOperationException("Bets can only be placed during the betting phase.");

        var seat = table.FindSeat(playerId)
            ?? throw new InvalidOperationException("Player is not seated at this table.");

        if (amount < table.Rules.MinBet || amount > table.Rules.MaxBet)
            throw new InvalidOperationException($"Bet must be between {table.Rules.MinBet} and {table.Rules.MaxBet}.");

        // refund previous bet if any
        if (seat.Bet > 0)
        {
            seat.Chips += seat.Bet;
            seat.Bet = 0;
            seat.CurrentHand.Bet = 0;
        }

        if (seat.Chips < amount)
            throw new InvalidOperationException("Not enough chips.");

        seat.Chips -= amount;
        seat.Bet = amount;
        seat.CurrentHand.Bet = amount;

        var events = new List<RoundEvent>
        {
            new(RoundEventType.BetPlaced, seat.PlayerId.ToString(), seat.PlayerName, $"Bet {amount}")
        };
        table.LastEvents = events;
        return events;
    }

    public List<RoundEvent> StartRound(BlackjackTable table)
    {
        if (table.Phase != GamePhase.Betting)
            throw new InvalidOperationException("Round can only be started from betting phase.");

        var active = table.Seats.Where(s => s.Bet > 0).ToList();
        if (active.Count == 0)
            throw new InvalidOperationException("At least one player must bet before starting a round.");

        // reset hands but keep Bet for active seats
        foreach (var seat in table.Seats)
        {
            seat.Hands = new List<Hand> { new Hand { Bet = seat.Bet } };
            seat.CurrentHandIndex = 0;
            seat.HasActed = false;
        }
        table.DealerHand = new Hand();
        table.Phase = GamePhase.Dealing;

        var events = new List<RoundEvent>
        {
            new(RoundEventType.RoundStarted, Message: "Round started"),
            new(RoundEventType.PhaseChanged, Message: GamePhase.Dealing.ToString())
        };

        // First card to each active seat
        foreach (var seat in active)
        {
            var card = table.Shoe.Draw();
            seat.CurrentHand.Add(card);
            events.Add(new RoundEvent(RoundEventType.CardDealt, seat.PlayerId.ToString(), seat.PlayerName, card.Display));
        }
        // Dealer upcard
        {
            var card = table.Shoe.Draw();
            table.DealerHand.Add(card);
            events.Add(new RoundEvent(RoundEventType.CardDealt, null, "Dealer", card.Display));
        }
        // Second card to each active seat
        foreach (var seat in active)
        {
            var card = table.Shoe.Draw();
            seat.CurrentHand.Add(card);
            events.Add(new RoundEvent(RoundEventType.CardDealt, seat.PlayerId.ToString(), seat.PlayerName, card.Display));
        }
        // Dealer hole card
        {
            var card = table.Shoe.Draw();
            table.DealerHand.Add(card);
            events.Add(new RoundEvent(RoundEventType.CardDealt, null, "Dealer", "??"));
        }

        // Check blackjacks
        foreach (var seat in active)
        {
            if (seat.CurrentHand.IsBlackjack)
            {
                events.Add(new RoundEvent(RoundEventType.PlayerBlackjack, seat.PlayerId.ToString(), seat.PlayerName, "Blackjack!"));
            }
        }

        table.Phase = GamePhase.PlayerTurn;
        events.Add(new RoundEvent(RoundEventType.PhaseChanged, Message: GamePhase.PlayerTurn.ToString()));

        // Find first seat with non-done hand
        table.CurrentSeatIndex = 0;
        SeekToFirstActionable(table, events);

        table.LastEvents = events;
        return events;
    }

    private void SeekToFirstActionable(BlackjackTable table, List<RoundEvent> events)
    {
        // Move to first seat with bet > 0 and hand not done.
        for (int i = 0; i < table.Seats.Count; i++)
        {
            var seat = table.Seats[i];
            if (seat.Bet <= 0) continue;

            // find first undone hand
            int idx = -1;
            for (int h = 0; h < seat.Hands.Count; h++)
            {
                if (!seat.Hands[h].IsDone) { idx = h; break; }
            }
            if (idx >= 0)
            {
                table.CurrentSeatIndex = i;
                seat.CurrentHandIndex = idx;
                table.TurnDeadline = DateTime.UtcNow.AddSeconds(table.Rules.TurnTimeoutSeconds);
                events.Add(new RoundEvent(RoundEventType.TurnChanged, seat.PlayerId.ToString(), seat.PlayerName));
                return;
            }
            seat.HasActed = true;
        }
        // No actionable seats - go to dealer
        table.Phase = GamePhase.DealerTurn;
        table.TurnDeadline = null;
        events.Add(new RoundEvent(RoundEventType.PhaseChanged, Message: GamePhase.DealerTurn.ToString()));
    }

    public List<RoundEvent> Hit(BlackjackTable table, Guid playerId)
    {
        var seat = ValidatePlayerTurn(table, playerId);
        var hand = seat.CurrentHand;

        if (hand.IsSplitAces && table.Rules.SplitAcesOneCardOnly)
            throw new InvalidOperationException("Cannot hit split aces: one card only per hand.");
        var card = table.Shoe.Draw();
        hand.Add(card);

        var events = new List<RoundEvent>
        {
            new(RoundEventType.PlayerHit, seat.PlayerId.ToString(), seat.PlayerName, card.Display)
        };

        if (hand.IsBust)
        {
            events.Add(new RoundEvent(RoundEventType.PlayerBust, seat.PlayerId.ToString(), seat.PlayerName));
            AdvanceTurn(table, events);
        }
        else if (hand.BestTotal() == 21)
        {
            hand.IsStood = true;
            AdvanceTurn(table, events);
        }

        table.LastEvents = events;
        return events;
    }

    public List<RoundEvent> Stand(BlackjackTable table, Guid playerId)
    {
        var seat = ValidatePlayerTurn(table, playerId);
        seat.CurrentHand.IsStood = true;
        var events = new List<RoundEvent>
        {
            new(RoundEventType.PlayerStood, seat.PlayerId.ToString(), seat.PlayerName)
        };
        AdvanceTurn(table, events);
        table.LastEvents = events;
        return events;
    }

    public List<RoundEvent> Double(BlackjackTable table, Guid playerId)
    {
        var seat = ValidatePlayerTurn(table, playerId);
        if (!table.Rules.AllowDouble) throw new InvalidOperationException("Doubling not allowed.");
        var hand = seat.CurrentHand;
        if (!hand.CanDouble) throw new InvalidOperationException("Can only double on first two cards.");
        if (seat.Chips < hand.Bet) throw new InvalidOperationException("Not enough chips to double.");

        seat.Chips -= hand.Bet;
        hand.Bet *= 2;
        var card = table.Shoe.Draw();
        hand.Add(card);
        hand.IsStood = true;

        var events = new List<RoundEvent>
        {
            new(RoundEventType.PlayerDoubled, seat.PlayerId.ToString(), seat.PlayerName, card.Display)
        };
        if (hand.IsBust)
            events.Add(new RoundEvent(RoundEventType.PlayerBust, seat.PlayerId.ToString(), seat.PlayerName));

        AdvanceTurn(table, events);
        table.LastEvents = events;
        return events;
    }

    public List<RoundEvent> Split(BlackjackTable table, Guid playerId)
    {
        var seat = ValidatePlayerTurn(table, playerId);
        if (!table.Rules.AllowSplit) throw new InvalidOperationException("Splitting not allowed.");
        if (seat.Hands.Count > table.Rules.MaxSplits) throw new InvalidOperationException($"Only {table.Rules.MaxSplits} split(s) allowed.");
        var hand = seat.CurrentHand;
        if (!hand.CanSplit) throw new InvalidOperationException("Hand cannot be split.");
        if (seat.Chips < hand.Bet) throw new InvalidOperationException("Not enough chips to split.");

        seat.Chips -= hand.Bet;

        var c0 = hand.Cards[0];
        var c1 = hand.Cards[1];
        var bet = hand.Bet;
        bool splitFromAces = c0.Rank == Rank.Ace;

        var h0 = new Hand { Bet = bet, IsSplitHand = true, IsSplitAces = splitFromAces };
        h0.Add(c0);
        h0.Add(table.Shoe.Draw());

        var h1 = new Hand { Bet = bet, IsSplitHand = true, IsSplitAces = splitFromAces };
        h1.Add(c1);
        h1.Add(table.Shoe.Draw());

        // Split aces: each hand receives exactly one card and is immediately stood.
        if (splitFromAces && table.Rules.SplitAcesOneCardOnly)
        {
            h0.IsStood = true;
            h1.IsStood = true;
        }

        seat.Hands = new List<Hand> { h0, h1 };
        seat.CurrentHandIndex = 0;

        var events = new List<RoundEvent>
        {
            new(RoundEventType.PlayerSplit, seat.PlayerId.ToString(), seat.PlayerName)
        };

        if (h0.IsDone)
        {
            AdvanceTurn(table, events);
        }
        else
        {
            table.TurnDeadline = DateTime.UtcNow.AddSeconds(table.Rules.TurnTimeoutSeconds);
        }

        table.LastEvents = events;
        return events;
    }

    public List<RoundEvent> PlayDealer(BlackjackTable table)
    {
        if (table.Phase != GamePhase.DealerTurn)
            throw new InvalidOperationException("Not dealer's turn.");

        var events = new List<RoundEvent>
        {
            new(RoundEventType.DealerTurn, Message: "Dealer plays")
        };

        // Only play if any player has a non-bust, non-blackjack hand (or any non-bust hand)
        bool anyContestingHand = table.Seats
            .Where(s => s.Bet > 0)
            .SelectMany(s => s.Hands)
            .Any(h => !h.IsBust && !h.IsBlackjack);

        if (anyContestingHand)
        {
            while (ShouldDealerHit(table.DealerHand, table.Rules))
            {
                var card = table.Shoe.Draw();
                table.DealerHand.Add(card);
                events.Add(new RoundEvent(RoundEventType.DealerHit, null, "Dealer", card.Display));
            }
        }
        events.Add(new RoundEvent(RoundEventType.DealerStood, null, "Dealer", $"Total {table.DealerHand.BestTotal()}"));

        table.LastEvents = events;
        return events;
    }

    private static bool ShouldDealerHit(Hand dealer, BlackjackRules rules)
    {
        int total = dealer.BestTotal();
        if (total < 17) return true;
        if (total == 17 && dealer.IsSoft && rules.DealerHitsSoft17) return true;
        return false;
    }

    public List<RoundEvent> Settle(BlackjackTable table)
    {
        var events = new List<RoundEvent>();
        int dealerTotal = table.DealerHand.BestTotal();
        bool dealerBust = table.DealerHand.IsBust;
        bool dealerBlackjack = table.DealerHand.IsBlackjack;

        foreach (var seat in table.Seats)
        {
            if (seat.Bet <= 0) continue;

            foreach (var hand in seat.Hands)
            {
                int bet = hand.Bet;
                if (hand.IsBust)
                {
                    hand.Result = HandResult.Bust;
                    hand.Winnings = -bet;
                }
                else if (hand.IsBlackjack && !dealerBlackjack)
                {
                    if (hand.IsSplitHand && table.Rules.SplitBlackjackPays1to1)
                    {
                        // Split-21 is not a natural blackjack — pays even money (1:1)
                        hand.Result = HandResult.Win;
                        seat.Chips += bet * 2;
                        hand.Winnings = bet;
                    }
                    else
                    {
                        hand.Result = HandResult.BlackjackWin;
                        int payout = (int)(bet * table.Rules.BlackjackPayout);
                        seat.Chips += bet + payout;
                        hand.Winnings = payout;
                    }
                }
                else if (hand.IsBlackjack && dealerBlackjack)
                {
                    hand.Result = HandResult.Push;
                    seat.Chips += bet;
                    hand.Winnings = 0;
                }
                else if (dealerBlackjack)
                {
                    hand.Result = HandResult.Lose;
                    hand.Winnings = -bet;
                }
                else if (dealerBust)
                {
                    hand.Result = HandResult.Win;
                    seat.Chips += bet * 2;
                    hand.Winnings = bet;
                }
                else
                {
                    int t = hand.BestTotal();
                    if (t > dealerTotal)
                    {
                        hand.Result = HandResult.Win;
                        seat.Chips += bet * 2;
                        hand.Winnings = bet;
                    }
                    else if (t == dealerTotal)
                    {
                        hand.Result = HandResult.Push;
                        seat.Chips += bet;
                        hand.Winnings = 0;
                    }
                    else
                    {
                        hand.Result = HandResult.Lose;
                        hand.Winnings = -bet;
                    }
                }

                events.Add(new RoundEvent(RoundEventType.RoundSettled,
                    seat.PlayerId.ToString(),
                    seat.PlayerName,
                    $"{hand.Result}:{hand.Winnings}"));
            }
        }

        table.Phase = GamePhase.Settling;
        events.Add(new RoundEvent(RoundEventType.PhaseChanged, Message: GamePhase.Settling.ToString()));
        table.LastEvents = events;
        return events;
    }

    public void ResetForNextRound(BlackjackTable table)
    {
        foreach (var seat in table.Seats)
        {
            seat.Hands = new List<Hand> { new Hand() };
            seat.CurrentHandIndex = 0;
            seat.Bet = 0;
            seat.HasActed = false;
        }
        table.DealerHand = new Hand();
        table.Phase = GamePhase.Betting;
        table.TurnDeadline = null;
        table.CurrentSeatIndex = 0;
        table.LastEvents.Clear();
    }

    private void AdvanceTurn(BlackjackTable table, List<RoundEvent> events)
    {
        var seat = table.CurrentSeat;
        if (seat is null) return;

        // Try to find next undone hand on the same seat
        for (int h = seat.CurrentHandIndex + 1; h < seat.Hands.Count; h++)
        {
            if (!seat.Hands[h].IsDone)
            {
                seat.CurrentHandIndex = h;
                table.TurnDeadline = DateTime.UtcNow.AddSeconds(table.Rules.TurnTimeoutSeconds);
                events.Add(new RoundEvent(RoundEventType.TurnChanged, seat.PlayerId.ToString(), seat.PlayerName));
                return;
            }
        }
        seat.HasActed = true;

        // Move to next seat
        for (int i = table.CurrentSeatIndex + 1; i < table.Seats.Count; i++)
        {
            var s = table.Seats[i];
            if (s.Bet <= 0) continue;
            int idx = -1;
            for (int h = 0; h < s.Hands.Count; h++)
            {
                if (!s.Hands[h].IsDone) { idx = h; break; }
            }
            if (idx >= 0)
            {
                table.CurrentSeatIndex = i;
                s.CurrentHandIndex = idx;
                table.TurnDeadline = DateTime.UtcNow.AddSeconds(table.Rules.TurnTimeoutSeconds);
                events.Add(new RoundEvent(RoundEventType.TurnChanged, s.PlayerId.ToString(), s.PlayerName));
                return;
            }
            s.HasActed = true;
        }

        // No more seats
        table.Phase = GamePhase.DealerTurn;
        table.TurnDeadline = null;
        events.Add(new RoundEvent(RoundEventType.PhaseChanged, Message: GamePhase.DealerTurn.ToString()));
    }

    private Seat ValidatePlayerTurn(BlackjackTable table, Guid playerId)
    {
        if (table.Phase != GamePhase.PlayerTurn)
            throw new InvalidOperationException("Not the player-turn phase.");
        var seat = table.CurrentSeat;
        if (seat is null || seat.PlayerId != playerId)
            throw new InvalidOperationException("Not your turn.");
        return seat;
    }
}
