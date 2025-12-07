using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;

namespace MB.PosSelection.Application.Rules
{
    public class LowestCommissionRule : IPosSelectionRule
    {
        public string RuleName => "LowestCommissionRate";
        public int ExecutionOrder => 3;

        public int Compare(PosCandidate? x, PosCandidate? y)
        {
            if (x == null || y == null) return 0;

            // Daha düşük komisyon oranı "daha iyi"dir, o yüzden standart sıralama (x -> y)
            return x.Ratio.CommissionRate.CompareTo(y.Ratio.CommissionRate);
        }
    }
}
