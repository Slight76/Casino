namespace Casino.Server.Games.Blackjack;

public enum Suit { Clubs, Diamonds, Hearts, Spades }

public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

public record Card(Suit Suit, Rank Rank)
{
    public int Value => Rank switch
    {
        Rank.Ace => 11,
        Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)Rank
    };

    public string Display => $"{RankLabel}{SuitSymbol}";

    public string RankLabel => Rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        _ => ((int)Rank).ToString()
    };

    public string SuitSymbol => Suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?"
    };
}
