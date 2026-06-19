using Casino.Server.Games.Blackjack;
using Xunit;

namespace Casino.Server.Tests;

internal class FakeShoe : Shoe
{
    private readonly Queue<Card> _queue;
    public FakeShoe(params Card[] cards) : base(1) { _queue = new Queue<Card>(cards); }
    public override Card Draw() => _queue.Dequeue();
}

public class EngineTests
{
    private static Card C(Rank r, Suit s = Suit.Clubs) => new(s, r);

    private static (BlackjackEngine engine, BlackjackTable table, Seat alice, Seat bob) MakeTable(
        params Card[] dealOrder)
    {
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules(StartingChips: 1000, MinBet: 10, MaxBet: 500));
        table.Shoe = new FakeShoe(dealOrder);
        var alice = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "Alice", Chips = 1000 };
        var bob = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "Bob", Chips = 1000 };
        table.Seats.Add(alice);
        table.Seats.Add(bob);
        return (engine, table, alice, bob);
    }

    [Fact]
    public void Full_Round_Two_Players_Stand_Dealer_Plays_Settle()
    {
        // Deal order: Alice1, Bob1, DealerUp, Alice2, Bob2, DealerHole, then dealer draws
        // Alice: 10+9 = 19 (stand)
        // Bob: 10+8 = 18 (stand)
        // Dealer: 7+hole? Let's give dealer 7+9 = 16, must hit. Next draw: 10 => 26 bust.
        var (engine, table, a, b) = MakeTable(
            C(Rank.Ten), C(Rank.Ten),       // p1
            C(Rank.Seven),                  // dealer up
            C(Rank.Nine), C(Rank.Eight),    // p2
            C(Rank.Nine),                   // dealer hole
            C(Rank.Ten));                   // dealer hit -> bust

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.PlaceBet(table, b.PlayerId, 100);
        engine.StartRound(table);

        Assert.Equal(GamePhase.PlayerTurn, table.Phase);
        Assert.Equal(a.PlayerId, table.CurrentSeat!.PlayerId);

        engine.Stand(table, a.PlayerId);
        Assert.Equal(b.PlayerId, table.CurrentSeat!.PlayerId);

        engine.Stand(table, b.PlayerId);
        Assert.Equal(GamePhase.DealerTurn, table.Phase);

        engine.PlayDealer(table);
        Assert.True(table.DealerHand.IsBust);

        engine.Settle(table);
        // Both win
        Assert.Equal(HandResult.Win, a.Hands[0].Result);
        Assert.Equal(HandResult.Win, b.Hands[0].Result);
        Assert.Equal(1100, a.Chips); // bet 100 -> +100 net
        Assert.Equal(1100, b.Chips);
    }

    [Fact]
    public void Blackjack_Pays_3_To_2()
    {
        // Alice gets BJ, dealer gets 20. No Bob.
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Ace),                   // alice 1
            C(Rank.Ten),                   // dealer up
            C(Rank.King),                  // alice 2 -> BJ
            C(Rank.Ten));                  // dealer hole -> 20
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        // Player has blackjack, hand is done, should advance straight to dealer
        Assert.Equal(GamePhase.DealerTurn, table.Phase);
        engine.PlayDealer(table);
        engine.Settle(table);
        Assert.Equal(HandResult.BlackjackWin, a.Hands[0].Result);
        Assert.Equal(150, a.Hands[0].Winnings);
        Assert.Equal(1150, a.Chips); // -100 bet + 100 back + 150 payout
    }

    [Fact]
    public void Dealer_Blackjack_Vs_Player_Blackjack_Is_Push()
    {
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Ace), C(Rank.Ace),
            C(Rank.King), C(Rank.King));
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        Assert.Equal(GamePhase.DealerTurn, table.Phase);
        engine.PlayDealer(table);
        engine.Settle(table);
        Assert.Equal(HandResult.Push, a.Hands[0].Result);
        Assert.Equal(1000, a.Chips);
    }

    [Fact]
    public void Dealer_Natural_Blackjack_Beats_Split_21()
    {
        // Alice: Ten+Ten -> split. h0 draws Ace for split-21, h1 draws Two.
        // Dealer: Ace+King natural blackjack.
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Ten),   // alice 1
            C(Rank.Ace),   // dealer up
            C(Rank.Ten),   // alice 2
            C(Rank.King),  // dealer hole -> natural blackjack
            C(Rank.Ace),   // split h0: 21 (split hand, not natural)
            C(Rank.Two));  // split h1: 12
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        engine.Split(table, a.PlayerId);
        engine.Stand(table, a.PlayerId); // stand h1

        Assert.Equal(GamePhase.DealerTurn, table.Phase);
        engine.PlayDealer(table);
        engine.Settle(table);

        Assert.Equal(HandResult.Lose, a.Hands[0].Result);
        Assert.Equal(-100, a.Hands[0].Winnings);
        Assert.Equal(HandResult.Lose, a.Hands[1].Result);
        Assert.Equal(800, a.Chips);
    }

    [Fact]
    public void Double_Doubles_Bet_One_Card_Then_Stand()
    {
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Five),  // alice
            C(Rank.Six),   // dealer up
            C(Rank.Six),   // alice (11)
            C(Rank.Ten),   // dealer hole (16)
            C(Rank.Ten),   // alice double card -> 21
            C(Rank.Five)); // dealer hits to 21
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);

        engine.Double(table, a.PlayerId);
        Assert.Equal(200, a.Hands[0].Bet);
        Assert.Equal(3, a.Hands[0].Cards.Count);
        Assert.True(a.Hands[0].IsStood);
        Assert.Equal(GamePhase.DealerTurn, table.Phase);

        // Cannot hit again
        Assert.Throws<InvalidOperationException>(() => engine.Hit(table, a.PlayerId));

        engine.PlayDealer(table);
        engine.Settle(table);
        // Both 21 -> push, chips back to 1000
        Assert.Equal(HandResult.Push, a.Hands[0].Result);
        Assert.Equal(1000, a.Chips);
    }

    [Fact]
    public void Split_Creates_Two_Hands()
    {
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        // Alice: 8+8 -> split. Each hand gets one new card.
        // Deal: A1=8, dealer up=10, A2=8, dealer hole=7, splitA=3 (8+3=11), splitB=2 (8+2=10)
        // Then stand both. Dealer 17 (10+7) stands at 17.
        table.Shoe = new FakeShoe(
            C(Rank.Eight),
            C(Rank.Ten),
            C(Rank.Eight),
            C(Rank.Seven),
            C(Rank.Three),
            C(Rank.Two));
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);

        engine.Split(table, a.PlayerId);
        Assert.Equal(2, a.Hands.Count);
        Assert.Equal(0, a.CurrentHandIndex);
        Assert.Equal(100, a.Hands[0].Bet);
        Assert.Equal(100, a.Hands[1].Bet);
        Assert.Equal(800, a.Chips); // 1000 - 100 (orig) - 100 (split)

        engine.Stand(table, a.PlayerId); // hand 0
        Assert.Equal(1, a.CurrentHandIndex);
        engine.Stand(table, a.PlayerId); // hand 1
        Assert.Equal(GamePhase.DealerTurn, table.Phase);
    }

    [Fact]
    public void Split_Only_Once_Per_Round()
    {
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        // Alice: 8+8 -> split, then 8+8 again -> attempt second split throws
        table.Shoe = new FakeShoe(
            C(Rank.Eight),
            C(Rank.Ten),
            C(Rank.Eight),
            C(Rank.Seven),
            C(Rank.Eight),  // split card 1 -> hand0: 8,8
            C(Rank.Eight)); // split card 2 -> hand1: 8,8
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        engine.Split(table, a.PlayerId);
        Assert.Throws<InvalidOperationException>(() => engine.Split(table, a.PlayerId));
    }

    [Fact]
    public void Player_Bust_Loses_Dealer_Bust_Wins_Push_Returns_Bet()
    {
        // Player A: bust. Player B: 20. Dealer: 18.
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        var b = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "B", Chips = 1000 };
        table.Seats.Add(a); table.Seats.Add(b);
        // Deal: A1=10, B1=10, DU=10, A2=6, B2=10, DH=8, AHit=10 (bust 26)
        table.Shoe = new FakeShoe(
            C(Rank.Ten),
            C(Rank.Ten),
            C(Rank.Ten),
            C(Rank.Six),
            C(Rank.Ten),
            C(Rank.Eight),
            C(Rank.Ten));
        engine.PlaceBet(table, a.PlayerId, 100);
        engine.PlaceBet(table, b.PlayerId, 100);
        engine.StartRound(table);

        engine.Hit(table, a.PlayerId); // a busts -> auto advance
        Assert.Equal(b.PlayerId, table.CurrentSeat!.PlayerId);
        engine.Stand(table, b.PlayerId);

        engine.PlayDealer(table);
        engine.Settle(table);

        Assert.Equal(HandResult.Bust, a.Hands[0].Result);
        Assert.Equal(900, a.Chips); // lost 100
        // B 20 vs dealer 18 -> win
        Assert.Equal(HandResult.Win, b.Hands[0].Result);
        Assert.Equal(1100, b.Chips);

        // Push test: separate round-style: rely on equal totals
        var engine2 = new BlackjackEngine();
        var table2 = new BlackjackTable("t2", new BlackjackRules());
        var p = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "P", Chips = 1000 };
        table2.Seats.Add(p);
        // P1=10, DealerUp=10, P2=9, DealerHole=9 -> both 19 -> push
        table2.Shoe = new FakeShoe(
            C(Rank.Ten), C(Rank.Ten), C(Rank.Nine), C(Rank.Nine));
        engine2.PlaceBet(table2, p.PlayerId, 100);
        engine2.StartRound(table2);
        engine2.Stand(table2, p.PlayerId);
        engine2.PlayDealer(table2);
        engine2.Settle(table2);
        Assert.Equal(HandResult.Push, p.Hands[0].Result);
        Assert.Equal(1000, p.Chips);
    }

    [Fact]
    public void SplitAces_OneCardOnly_HandsAutoStand_And_DealerTurnAdvances()
    {
        // Alice: Ace+Ace → split. Each hand gets exactly one drawn card then auto-stands.
        // Deal: A1=Ace, DU=Ten, A2=Ace, DH=Seven, split-h0=King (Ace+King=21), split-h1=Three (Ace+Three=14)
        // Dealer: Ten+Seven=17 (no further hit needed).
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Ace),    // alice 1
            C(Rank.Ten),    // dealer up
            C(Rank.Ace),    // alice 2
            C(Rank.Seven),  // dealer hole (Ten+Seven=17)
            C(Rank.King),   // split h0: Ace+King = 21
            C(Rank.Three)); // split h1: Ace+Three = 14
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        engine.Split(table, a.PlayerId);

        // Each hand: one original ace + one drawn card = 2 cards total
        Assert.Equal(2, a.Hands.Count);
        Assert.Equal(2, a.Hands[0].Cards.Count);
        Assert.Equal(2, a.Hands[1].Cards.Count);
        // Both auto-stood after the single drawn card
        Assert.True(a.Hands[0].IsStood);
        Assert.True(a.Hands[1].IsStood);
        // Both marked as split-from-aces
        Assert.True(a.Hands[0].IsSplitAces);
        Assert.True(a.Hands[1].IsSplitAces);
        // No player action required — already at dealer turn
        Assert.Equal(GamePhase.DealerTurn, table.Phase);

        engine.PlayDealer(table);
        engine.Settle(table);

        // h0: Ace+King=21 (split, IsSplitHand=true) → 1:1 win vs dealer 17
        Assert.Equal(HandResult.Win, a.Hands[0].Result);
        Assert.Equal(100, a.Hands[0].Winnings);
        // h1: Ace+Three=14 < dealer 17 → lose
        Assert.Equal(HandResult.Lose, a.Hands[1].Result);
    }

    [Fact]
    public void SplitAces_Hit_Throws_WhenSplitAcesOneCardOnly()
    {
        // After splitting aces, hitting is forbidden. Verify the guard throws even if caller bypasses
        // the normal turn-advance logic.
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.Ace), C(Rank.Ten), C(Rank.Ace), C(Rank.Seven),
            C(Rank.King), C(Rank.Three), C(Rank.Five)); // extra card never consumed by normal play
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);
        engine.Split(table, a.PlayerId);

        // Force player-turn context for the split-ace hand (bypasses normal auto-advance)
        table.Phase = GamePhase.PlayerTurn;
        table.CurrentSeatIndex = 0;
        a.CurrentHandIndex = 0;
        a.Hands[0].IsStood = false; // un-stand to make ValidatePlayerTurn pass

        Assert.Throws<InvalidOperationException>(() => engine.Hit(table, a.PlayerId));
    }

    [Fact]
    public void SplitHand_21_Pays_1To1_Not_3To2()
    {
        // Alice splits King+Ten (both value 10). After split: h0=King+Ace=21 (IsSplitHand), h1=Ten+Six=16.
        // h0 is done (IsBlackjack=true). Turn advances to h1 immediately.
        // Alice stands h1 (16). Dealer 5+9=14, hits Three → 17.
        // h0 should pay 1:1 (Win, not BlackjackWin). h1 loses (16 < 17).
        var engine = new BlackjackEngine();
        var table = new BlackjackTable("t1", new BlackjackRules());
        table.Shoe = new FakeShoe(
            C(Rank.King),  // alice 1
            C(Rank.Five),  // dealer up
            C(Rank.Ten),   // alice 2 (same value as King → CanSplit)
            C(Rank.Nine),  // dealer hole (5+9=14)
            C(Rank.Ace),   // split h0: King+Ace = 21
            C(Rank.Six),   // split h1: Ten+Six = 16
            C(Rank.Three)); // dealer hits: 14+3=17
        var a = new Seat { PlayerId = Guid.NewGuid(), PlayerName = "A", Chips = 1000 };
        table.Seats.Add(a);

        engine.PlaceBet(table, a.PlayerId, 100);
        engine.StartRound(table);

        engine.Split(table, a.PlayerId);
        // h0 (King+Ace=21) is done → turn advanced to h1
        Assert.Equal(1, a.CurrentHandIndex);
        Assert.True(a.Hands[0].IsSplitHand);

        engine.Stand(table, a.PlayerId); // stand h1 (16)
        Assert.Equal(GamePhase.DealerTurn, table.Phase);

        engine.PlayDealer(table); // dealer 14 → hits to 17
        engine.Settle(table);

        // h0: split hand totalling 21 → pays 1:1 (Win, not BlackjackWin)
        Assert.Equal(HandResult.Win, a.Hands[0].Result);
        Assert.Equal(100, a.Hands[0].Winnings); // 1:1 not 150 (3:2)

        // h1: 16 < dealer 17 → lose
        Assert.Equal(HandResult.Lose, a.Hands[1].Result);

        // Net: started 1000, bet 100 → 900, split extra 100 → 800
        // h0 win 1:1: +200 → 1000; h1 loss: no chips change
        Assert.Equal(1000, a.Chips);
    }
}
