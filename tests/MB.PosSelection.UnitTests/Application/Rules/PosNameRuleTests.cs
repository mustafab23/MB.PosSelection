using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.UnitTests.Application.TestHelpers;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Rules
{
    public class PosNameRuleTests
    {
        [Fact]
        public void Compare_Should_Order_Alphabetically()
        {
            // Arrange
            var rule = new PosNameRule();

            var ziraat = CandidateFactory.Create("Ziraat", 10m, 1);
            var akbank = CandidateFactory.Create("Akbank", 10m, 1);

            var list = new List<PosCandidate> { ziraat, akbank };

            // Act
            list.Sort(rule);

            // Assert
            list[0].Ratio.PosName.Should().Be("Akbank");
            list[1].Ratio.PosName.Should().Be("Ziraat");
        }
    }
}
