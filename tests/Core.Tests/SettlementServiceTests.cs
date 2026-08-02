using Core.Settlements;

namespace Core.Tests;

public class SettlementServiceTests
{
    private readonly SettlementService _sut = new();

    [Fact]
    public void ComputeSettlement_TwoPersonPair_ReturnsSingleTransfer()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        // Alice paid 100 for something split equally between the two of them.
        var balances = new Dictionary<Guid, decimal>
        {
            [alice] = 50.00m,
            [bob] = -50.00m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        var transfer = Assert.Single(transfers);
        Assert.Equal(bob, transfer.FromUserId);
        Assert.Equal(alice, transfer.ToUserId);
        Assert.Equal(50.00m, transfer.Amount);
    }

    [Fact]
    public void ComputeSettlement_ThreeWayCycle_ProducesAtMostTwoTransfers()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        // Alice and Bob each owe money; Carol is owed everything.
        var balances = new Dictionary<Guid, decimal>
        {
            [alice] = -10.00m,
            [bob] = -5.00m,
            [carol] = 15.00m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        Assert.True(transfers.Count <= 2, "3 participants should never require more than n-1 = 2 transfers.");
        AssertReconcilesToBalances(balances, transfers);
    }

    [Fact]
    public void ComputeSettlement_SinglePayerForWholeGroup_EveryoneElsePaysThePayerDirectly()
    {
        var payer = Guid.NewGuid();
        var friend1 = Guid.NewGuid();
        var friend2 = Guid.NewGuid();
        var friend3 = Guid.NewGuid();

        // Payer covered a 120 expense split four ways (30 each); the other
        // three each owe the payer 30.
        var balances = new Dictionary<Guid, decimal>
        {
            [payer] = 90.00m,
            [friend1] = -30.00m,
            [friend2] = -30.00m,
            [friend3] = -30.00m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        Assert.Equal(3, transfers.Count);
        Assert.All(transfers, t => Assert.Equal(payer, t.ToUserId));
        Assert.All(transfers, t => Assert.Equal(30.00m, t.Amount));
        AssertReconcilesToBalances(balances, transfers);
    }

    [Fact]
    public void ComputeSettlement_AlreadySettled_ReturnsNoTransfers()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = 0m,
            [Guid.NewGuid()] = 0m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        Assert.Empty(transfers);
    }

    [Fact]
    public void ComputeSettlement_UnevenAmounts_HandlesFractionalCentsWithNoRoundingDrift()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        // A 0.05 expense split three ways with the remainder cent allocated
        // to the first two participants: 0.02 + 0.02 + 0.01 = 0.05.
        var balances = new Dictionary<Guid, decimal>
        {
            [alice] = 0.02m,
            [bob] = 0.02m,
            [carol] = -0.04m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        Assert.Equal(0.04m, transfers.Sum(t => t.Amount));
        AssertReconcilesToBalances(balances, transfers);
    }

    [Fact]
    public void ComputeSettlement_SixParticipantsWithMixedBalances_NeverExceedsNMinusOneTransfers()
    {
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();

        // Sums to zero: +40 +25 +5 -15 -20 -35
        var balances = new Dictionary<Guid, decimal>
        {
            [ids[0]] = 40.00m,
            [ids[1]] = 25.00m,
            [ids[2]] = 5.00m,
            [ids[3]] = -15.00m,
            [ids[4]] = -20.00m,
            [ids[5]] = -35.00m,
        };

        var transfers = _sut.ComputeSettlement(balances);

        Assert.True(transfers.Count <= ids.Length - 1);
        AssertReconcilesToBalances(balances, transfers);
    }

    /// <summary>
    /// A settlement plan is only correct if, for every participant, the net
    /// effect of their transfers (paid out subtracted, received added)
    /// reproduces their original balance exactly.
    /// </summary>
    private static void AssertReconcilesToBalances(
        IReadOnlyDictionary<Guid, decimal> balances,
        IReadOnlyList<Transfer> transfers)
    {
        foreach (var (userId, expectedBalance) in balances)
        {
            var paidOut = transfers.Where(t => t.FromUserId == userId).Sum(t => t.Amount);
            var received = transfers.Where(t => t.ToUserId == userId).Sum(t => t.Amount);
            Assert.Equal(expectedBalance, received - paidOut);
        }
    }
}
