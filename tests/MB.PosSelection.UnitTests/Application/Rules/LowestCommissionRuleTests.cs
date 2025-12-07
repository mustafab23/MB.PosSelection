using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.UnitTests.Application.TestHelpers;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Rules
{
    public class LowestCommissionRuleTests
    {
        [Fact]
        public void Compare_Should_Order_By_CommissionRate_Ascending()
        {
            // Arrange
            var rule = new LowestCommissionRule();

            var highComm = CandidateFactory.Create("BankA", 10m, 1, commissionRate: 0.05m);
            var lowComm = CandidateFactory.Create("BankB", 10m, 1, commissionRate: 0.01m);

            var list = new List<PosCandidate> { highComm, lowComm };

            // Act
            list.Sort(rule);

            // Assert
            list[0].Ratio.PosName.Should().Be("BankB"); // Düşük komisyon önde
            list[1].Ratio.PosName.Should().Be("BankA");
        }
    }
}
