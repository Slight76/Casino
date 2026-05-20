using Casino.Server.Games.Blackjack;
using Xunit;

namespace Casino.Server.Tests;

public class HandTests
{
    private static Card C(Rank r, Suit s = Suit.Clubs) => new(s, r);

    [Fact]
    public void BestTotal_HardTotals()
    {
        var h = new Hand();
        h.Add(C(Rank.Two));
        h.Add(C(Rank.Three));
        Assert.Equal(5, h.BestTotal());

        var h2 = new Hand();
        h2.Add(C(Rank.Ten));
        h2.Add(C(Rank.Jack));
        Assert.Equal(20, h2.BestTotal());

        var h3 = new Hand();
        h3.Add(C(Rank.Ten));
        h3.Add(C(Rank.King));
        h3.Add(C(Rank.Two));
        Assert.Equal(22, h3.BestTotal());
        Assert.True(h3.IsBust);
    }

    [Fact]
    public void BestTotal_Soft()
    {
        var soft = new Hand();
        soft.Add(C(Rank.Ace));
        soft.Add(C(Rank.Six));
        Assert.Equal(17, soft.BestTotal());
        Assert.True(soft.IsSoft);

        var hard = new Hand();
        hard.Add(C(Rank.Ace));
        hard.Add(C(Rank.Six));
        hard.Add(C(Rank.Eight));
        Assert.Equal(15, hard.BestTotal());
        Assert.False(hard.IsSoft);
    }

    [Fact]
    public void IsBlackjack_Cases()
    {
        var bj = new Hand();
        bj.Add(C(Rank.Ace));
        bj.Add(C(Rank.King));
        Assert.True(bj.IsBlackjack);

        var notBj = new Hand();
        notBj.Add(C(Rank.Ace));
        notBj.Add(C(Rank.King));
        notBj.Add(C(Rank.Two));
        Assert.False(notBj.IsBlackjack);

        var twoCard21False = new Hand();
        twoCard21False.Add(C(Rank.Nine));
        twoCard21False.Add(C(Rank.Two));
        Assert.False(twoCard21False.IsBlackjack);
    }

    [Fact]
    public void IsBust_True_On_Over_21()
    {
        var h = new Hand();
        h.Add(C(Rank.Seven));
        h.Add(C(Rank.Eight));
        h.Add(C(Rank.Nine));
        Assert.True(h.IsBust);
        Assert.Equal(24, h.BestTotal());
    }

    [Fact]
    public void CanSplit_Cases()
    {
        var pairFaces = new Hand();
        pairFaces.Add(C(Rank.King));
        pairFaces.Add(C(Rank.Jack));
        Assert.True(pairFaces.CanSplit); // both value 10

        var aces = new Hand();
        aces.Add(C(Rank.Ace));
        aces.Add(C(Rank.Ace));
        Assert.True(aces.CanSplit);

        var notPair = new Hand();
        notPair.Add(C(Rank.King));
        notPair.Add(C(Rank.Nine));
        Assert.False(notPair.CanSplit);
    }

    [Fact]
    public void CanDouble_Only_With_Two_Cards()
    {
        var two = new Hand();
        two.Add(C(Rank.Five));
        two.Add(C(Rank.Six));
        Assert.True(two.CanDouble);

        two.Add(C(Rank.Two));
        Assert.False(two.CanDouble);
    }

    [Fact]
    public void IsDone_Variations()
    {
        var live = new Hand();
        live.Add(C(Rank.Two));
        live.Add(C(Rank.Three));
        Assert.False(live.IsDone);

        live.IsStood = true;
        Assert.True(live.IsDone);

        var bust = new Hand();
        bust.Add(C(Rank.Ten));
        bust.Add(C(Rank.Ten));
        bust.Add(C(Rank.Five));
        Assert.True(bust.IsDone);

        var bj = new Hand();
        bj.Add(C(Rank.Ace));
        bj.Add(C(Rank.Queen));
        Assert.True(bj.IsDone);
    }
}
