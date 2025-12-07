using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;

namespace MB.PosSelection.Application.Rules
{
    public class HighestPriorityRule : IPosSelectionRule
    {
        public string RuleName => "HighestPriority";
        public int ExecutionOrder => 2;

        public int Compare(PosCandidate? x, PosCandidate? y)
        {
            if (x == null || y == null) return 0;
            // Priority yüksek olan "daha küçük" (daha önde) sayılsın diye ters çeviriyoruz (y.CompareTo(x))
            // Veya listeyi OrderByDescending yapmamak için burada mantığı kurguluyoruz.
            // Standart Sort işleminde küçük değer başa gelir. Biz Priority'si BÜYÜK olanın başa gelmesini istiyoruz.
            return y.Ratio.Priority.CompareTo(x.Ratio.Priority);
        }
    }
}
