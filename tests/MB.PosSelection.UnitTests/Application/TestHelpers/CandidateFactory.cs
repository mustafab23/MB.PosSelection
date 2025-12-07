using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;

namespace MB.PosSelection.UnitTests.Application.TestHelpers
{
    public static class CandidateFactory
    {
        public static PosCandidate Create(string posName, decimal price, int priority, decimal commissionRate = 0.01m)
        {
            var ratio = new PosRatio(
                "batch-1", posName, CardType.Credit, "bonus", 1, Currency.TRY, commissionRate, 0, priority);

            // PayableTotal = Price + 100 (Varsayılan Tutar)
            return new PosCandidate(ratio, price, price + 100);
        }
    }
}
