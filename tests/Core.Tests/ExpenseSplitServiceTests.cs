using Core.Expenses;

namespace Core.Tests;

public class ExpenseSplitServiceTests
{
    private readonly ExpenseSplitService _sut = new();

    [Fact]
    public void SplitEqually_EvenlyDivisibleAmount_AllSharesEqual()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        var shares = _sut.SplitEqually(30.00m, [alice, bob, carol]);

        Assert.Equal(10.00m, shares[alice]);
        Assert.Equal(10.00m, shares[bob]);
        Assert.Equal(10.00m, shares[carol]);
        Assert.Equal(30.00m, shares.Values.Sum());
    }

    [Fact]
    public void SplitEqually_UnevenAmount_SharesSumBackToOriginalWithRemainderAllocatedDeterministically()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        // €10.00 / 3 = 333.33... cents each; 1000 cents total, base 333,
        // remainder 1 cent - goes to the first participant in input order.
        var shares = _sut.SplitEqually(10.00m, [alice, bob, carol]);

        Assert.Equal(3.34m, shares[alice]);
        Assert.Equal(3.33m, shares[bob]);
        Assert.Equal(3.33m, shares[carol]);
        Assert.Equal(10.00m, shares.Values.Sum());
    }

    [Fact]
    public void SplitEqually_SingleParticipant_SharesEntireAmount()
    {
        var alice = Guid.NewGuid();

        var shares = _sut.SplitEqually(42.75m, [alice]);

        Assert.Equal(42.75m, shares[alice]);
    }

    [Fact]
    public void SplitEqually_ManyParticipantsSmallAmount_NoShareIsNegativeAndSharesSumToTotal()
    {
        var participants = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        // 1 cent split five ways: one participant gets it, the rest get 0 -
        // none negative, and it still sums back to the original amount.
        var shares = _sut.SplitEqually(0.01m, participants);

        Assert.All(shares.Values, share => Assert.True(share >= 0m));
        Assert.Equal(0.01m, shares.Values.Sum());
    }
}
