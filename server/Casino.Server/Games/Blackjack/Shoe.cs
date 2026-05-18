namespace Casino.Server.Games.Blackjack;

public class Shoe
{
    private readonly List<Card> _cards;
    private readonly Random _random;
    private int _index;
    public int DeckCount { get; }

    public Shoe(int deckCount = 6, Random? random = null)
    {
        if (deckCount < 1) throw new ArgumentOutOfRangeException(nameof(deckCount));
        DeckCount = deckCount;
        _random = random ?? Random.Shared;
        _cards = new List<Card>(deckCount * 52);
        Build();
        Shuffle();
    }

    private void Build()
    {
        _cards.Clear();
        for (int d = 0; d < DeckCount; d++)
        {
            foreach (Suit s in Enum.GetValues<Suit>())
            {
                foreach (Rank r in Enum.GetValues<Rank>())
                {
                    _cards.Add(new Card(s, r));
                }
            }
        }
        _index = 0;
    }

    private void Shuffle()
    {
        // Fisher-Yates
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _index = 0;
    }

    public int TotalCards => _cards.Count;
    public int DrawCount => _index;
    public double Penetration => (double)_index / _cards.Count;

    public virtual Card Draw()
    {
        if (Penetration >= 0.75)
        {
            Build();
            Shuffle();
        }
        if (_index >= _cards.Count)
        {
            Build();
            Shuffle();
        }
        return _cards[_index++];
    }
}
