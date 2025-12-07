using MB.PosSelection.Application.Dtos;

namespace MB.PosSelection.Application.Models
{
    /// <summary>
    /// Bu sınıf, API Response Body'sini kirletmeden, 
    /// Controller'a metadata (Header bilgileri) taşımak için kullanılır.
    /// </summary>
    public class PosSelectionResult
    {
        public PosSelectionResponseDto Data { get; }
        public PosSelectionMeta Meta { get; }

        public PosSelectionResult(PosSelectionResponseDto data, PosSelectionMeta meta)
        {
            Data = data;
            Meta = meta;
        }
    }

    public class PosSelectionMeta
    {
        public int EvaluatedCount { get; set; }
        public int FilteredOutCount { get; set; }
        public string SelectionReason { get; set; } = string.Empty;
        public string WinnerPosName { get; set; } = string.Empty;
        public double ExecutionTimeMs { get; set; }
    }
}
