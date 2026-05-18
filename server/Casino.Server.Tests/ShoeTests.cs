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
    public void Reshuffles_At_75_Percent_Penetration()
    {
        var shoe = new Shoe(1);
        // Draw 76% of cards => 40 of 52
        for (int i = 0; i < 39; i++) shoe.Draw();
        Assert.Equal(39, shoe.DrawCount);
        // 40th draw crosses the 75% threshold (39/52 = 0.75), triggering reshuffle on next draw
        // Drawing once more — penetration before draw is 0.75, so Draw() will reshuffle then return
        shoe.Draw();
        Assert.Equal(1, shoe.DrawCount);
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
