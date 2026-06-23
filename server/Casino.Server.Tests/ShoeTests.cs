using Casino.Server.Games.Blackjack;
using Xunit;

namespace Casino.Server.Tests;

public class ShoeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    public void Constructs_With_N_Decks(int n)
    {
        var shoe = new Shoe(n);
        Assert.Equal(52 * n, shoe.TotalCards);
    }

    [Fact]
    public void Draw_Does_Not_Reshuffle_At_75_Percent_Penetration()
    {
        var shoe = new Shoe(1);
        // Draw 40 of 52 cards; penetration reaches and crosses 75%, but draw source should not reset mid-round.
        for (int i = 0; i < 39; i++) shoe.Draw();
        shoe.Draw();
        Assert.Equal(40, shoe.DrawCount);
    }

    [Fact]
    public void Reshuffles_At_Round_Boundary_After_75_Percent_Penetration()
    {
        var shoe = new Shoe(1);
        for (int i = 0; i < 40; i++) shoe.Draw();

        shoe.ReshuffleIfNeededAtRoundBoundary();

        Assert.Equal(0, shoe.DrawCount);
    }

    [Fact]
    public void Reshuffles_At_Round_Boundary_When_Remaining_Cards_Are_Below_Buffer()
    {
        var shoe = new Shoe(1);
        for (int i = 0; i < 38; i++) shoe.Draw();
        Assert.Equal(14, shoe.RemainingCards);

        shoe.ReshuffleIfNeededAtRoundBoundary(minimumRemainingCards: 16);

        Assert.Equal(0, shoe.DrawCount);
        Assert.Equal(52, shoe.RemainingCards);
    }

    [Fact]
    public void Draw_Always_Returns_A_Card_Even_Beyond_Original_Size()
    {
        var shoe = new Shoe(1);
        for (int i = 0; i < 500; i++)
        {
            var c = shoe.Draw();
            Assert.NotNull(c);
        }
    }

    [Fact]
    public void Penetration_Tracks_Correctly()
    {
        var shoe = new Shoe(1);
        Assert.Equal(0.0, shoe.Penetration);
        shoe.Draw();
        Assert.Equal(1.0 / 52.0, shoe.Penetration, 5);
    }

    [Fact]
    public void Seeded_Random_Is_Deterministic()
    {
        var s1 = new Shoe(1, new Random(42));
        var s2 = new Shoe(1, new Random(42));
        for (int i = 0; i < 30; i++)
        {
            var a = s1.Draw();
            var b = s2.Draw();
            Assert.Equal(a, b);
        }
    }
}
