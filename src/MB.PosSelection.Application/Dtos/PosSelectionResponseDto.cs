using System.Text.Json.Serialization;

namespace MB.PosSelection.Application.Dtos
{
    public class PosSelectionResponseDto
    {
        [JsonPropertyName("pos_name")]
        public string PosName { get; set; } = string.Empty;

        [JsonPropertyName("card_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CardType { get; set; } = string.Empty;

        [JsonPropertyName("card_brand")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CardBrand { get; set; } = string.Empty;

        [JsonPropertyName("installment")]
        public int Installment { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("commission_rate")]
        public decimal CommissionRate { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("payable_total")]
        public decimal PayableTotal { get; set; }
    }
}
