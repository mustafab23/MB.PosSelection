using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.UnitTests.Application.TestHelpers;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Rules
{
    public class LowestCostRuleTests
    {
        [Fact]
        public void Compare_Should_Order_Candidates_By_Price_Ascending()
        {
            // Arrange
            var rule = new LowestCostRule();

            // CandidateFactory.Create(posName, price, priority) imzasını kullanıyoruz.
            var expensive = CandidateFactory.Create("PahaliBank", price: 5.00m, priority: 1);
            var cheap = CandidateFactory.Create("UcuzBank", price: 1.50m, priority: 1);
            var medium = CandidateFactory.Create("OrtaBank", price: 3.00m, priority: 1);

            var candidates = new List<PosCandidate> { expensive, cheap, medium };

            // Act
            candidates.Sort(rule);

            // Assert
            candidates.Should().HaveCount(3);

            // Beklenen Sıralama: Küçükten Büyüğe (Price)
            // 1.50 (Ucuz) -> 3.00 (Orta) -> 5.00 (Pahalı)
            candidates[0].Ratio.PosName.Should().Be("UcuzBank");
            candidates[1].Ratio.PosName.Should().Be("OrtaBank");
            candidates[2].Ratio.PosName.Should().Be("PahaliBank");
        }
    }
}
