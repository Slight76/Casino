namespace Casino.Server.Games.Blackjack;

public class Seat
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Chips { get; set; }
    public List<Hand> Hands { get; set; } = new() { new Hand() };
    public int CurrentHandIndex { get; set; }
    public int Bet { get; set; }
    public bool HasActed { get; set; }
    public Hand CurrentHand => Hands[CurrentHandIndex];
}
