using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;

namespace MB.PosSelection.Application.Rules
{
    public class LowestCostRule : IPosSelectionRule
    {
        public string RuleName => "LowestCost";
        public int ExecutionOrder => 1;

        public int Compare(PosCandidate? x, PosCandidate? y)
        {
            if (x == null || y == null) return 0;
            return x.Price.CompareTo(y.Price);
        }
    }
}
