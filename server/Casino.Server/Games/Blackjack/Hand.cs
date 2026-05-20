namespace Casino.Server.Games.Blackjack;

public enum HandResult { Win, Lose, Push, BlackjackWin, Bust }

public class Hand
{
    private readonly List<Card> _cards = new();
    public IReadOnlyList<Card> Cards => _cards;

    public bool IsStood { get; set; }
    public bool IsSplitHand { get; set; }  // true when this hand was created by splitting
    public bool IsSplitAces { get; set; }  // true when this hand was created by splitting aces
    public HandResult? Result { get; set; }
    public int Winnings { get; set; }
    public int Bet { get; set; }

    public void Add(Card card) => _cards.Add(card);

    public int BestTotal()
    {
        int total = 0;
        int aces = 0;
        foreach (var c in _cards)
        {
            total += c.Value;
            if (c.Rank == Rank.Ace) aces++;
        }
        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }
        return total;
    }

    public bool IsBlackjack => _cards.Count == 2 && BestTotal() == 21;
    public bool IsBust => BestTotal() > 21;

    public bool IsSoft
    {
        get
        {
            int total = 0;
            int aces = 0;
            foreach (var c in _cards)
            {
                total += c.Value;
                if (c.Rank == Rank.Ace) aces++;
            }
            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }
            // soft if at least one ace is still counted as 11
            return aces > 0 && total <= 21;
        }
    }

    public bool CanSplit => _cards.Count == 2 && _cards[0].Value == _cards[1].Value;
    public bool CanDouble => _cards.Count == 2;
    public bool IsDone => IsBust || IsBlackjack || IsStood;
}
