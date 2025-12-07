using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.UnitTests.Application.TestHelpers;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Rules
{
    public class HighestPriorityRuleTests
    {
        [Fact]
        public void Compare_Should_Order_By_Priority_Descending()
        {
            // Arrange
            var rule = new HighestPriorityRule();

            // Fiyatlar eşit, Priority farklı
            var highPriority = CandidateFactory.Create("YapiKredi", 10.0m, priority: 10);
            var lowPriority = CandidateFactory.Create("Garanti", 10.0m, priority: 5);
            var mediumPriority = CandidateFactory.Create("IsBank", 10.0m, priority: 7);

            var list = new List<PosCandidate> { lowPriority, highPriority, mediumPriority };

            // Act
            list.Sort(rule);

            // Assert
            // Beklenen: High (10) -> Medium (7) -> Low (5)
            list[0].Ratio.PosName.Should().Be("YapiKredi");
            list[1].Ratio.PosName.Should().Be("IsBank");
            list[2].Ratio.PosName.Should().Be("Garanti");
        }
    }
}
