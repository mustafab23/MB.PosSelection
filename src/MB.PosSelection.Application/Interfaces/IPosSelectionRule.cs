using MB.PosSelection.Application.Models;

namespace MB.PosSelection.Application.Interfaces
{
    public interface IPosSelectionRule : IComparer<PosCandidate>
    {
        // Kuralın çalışma sırasını belirler (Priority, Cost'tan sonra gelir gibi)
        int ExecutionOrder { get; }
        string RuleName { get; }
    }
}
