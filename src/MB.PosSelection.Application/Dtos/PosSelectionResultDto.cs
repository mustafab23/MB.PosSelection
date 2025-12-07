using System.Text.Json.Serialization;

namespace MB.PosSelection.Application.Dtos
{
    /// <summary>
    /// Kök dizinde "filters" ve "overall_min" nesnelerini barındırır.
    /// </summary>
    public class PosSelectionResultDto
    {
        [JsonPropertyName("filters")]
        public PosSelectionRequestDto Filters { get; set; }

        [JsonPropertyName("overall_min")]
        public PosSelectionResponseDto OverallMin { get; set; }

        public PosSelectionResultDto(PosSelectionRequestDto filters, PosSelectionResponseDto overallMin)
        {
            Filters = filters;
            OverallMin = overallMin;
        }
    }
}
