using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Rules
{
    public class PosSelectionRulesTests
    {
        private PosCandidate CreateCandidate(string posName, decimal price, int priority, decimal commissionRate)
        {
            var ratio = new PosRatio("b1", posName, CardType.Credit, "bonus", 1, Currency.TRY, commissionRate, 0, priority);
            return new PosCandidate(ratio, price, price + 100);
        }

        [Fact]
        public void LowestCostRule_Should_Order_By_Price_Ascending()
        {
            var rule = new LowestCostRule();
            var cheap = CreateCandidate("Cheap", 10m, 1, 0.01m);
            var expensive = CreateCandidate("Expensive", 50m, 1, 0.01m);

            var list = new List<PosCandidate> { expensive, cheap };
            list.Sort(rule);

            list[0].Ratio.PosName.Should().Be("Cheap");
            list[1].Ratio.PosName.Should().Be("Expensive");
        }

        [Fact]
        public void HighestPriorityRule_Should_Order_By_Priority_Descending()
        {
            var rule = new HighestPriorityRule();
            var highPrio = CreateCandidate("High", 10m, 10, 0.01m);
            var lowPrio = CreateCandidate("Low", 10m, 1, 0.01m);

            var list = new List<PosCandidate> { lowPrio, highPrio };
            list.Sort(rule); // Sort varsayılanı Ascending'dir, Rule bunu tersine çevirmeli (Descending)

            list[0].Ratio.PosName.Should().Be("High");
            list[1].Ratio.PosName.Should().Be("Low");
        }

        [Fact]
        public void LowestCommissionRule_Should_Order_By_Commission_Ascending()
        {
            var rule = new LowestCommissionRule();
            var lowComm = CreateCandidate("LowComm", 10m, 1, 0.01m);
            var highComm = CreateCandidate("HighComm", 10m, 1, 0.05m);

            var list = new List<PosCandidate> { highComm, lowComm };
            list.Sort(rule);

            list[0].Ratio.PosName.Should().Be("LowComm");
            list[1].Ratio.PosName.Should().Be("HighComm");
        }

        [Fact]
        public void PosNameRule_Should_Order_Alphabetically()
        {
            var rule = new PosNameRule();
            var akbank = CreateCandidate("Akbank", 10m, 1, 0.01m);
            var ziraat = CreateCandidate("Ziraat", 10m, 1, 0.01m);

            var list = new List<PosCandidate> { ziraat, akbank };
            list.Sort(rule);

            list[0].Ratio.PosName.Should().Be("Akbank");
            list[1].Ratio.PosName.Should().Be("Ziraat");
        }
    }
}
