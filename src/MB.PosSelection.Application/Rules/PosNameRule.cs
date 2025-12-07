using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;

namespace MB.PosSelection.Application.Rules
{
    public class PosNameRule : IPosSelectionRule
    {
        public string RuleName => "AlphabeticalPosName";
        public int ExecutionOrder => 4;

        public int Compare(PosCandidate? x, PosCandidate? y)
        {
            if (x == null || y == null) return 0;

            // String karşılaştırma (Alfabetik A-Z)
            return string.Compare(x.Ratio.PosName, y.Ratio.PosName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
