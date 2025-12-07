using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MB.PosSelection.Application.Dtos
{
    /// <summary>
    /// En uygun POS'un seçilmesi için gerekli işlem bilgilerini içeren talep modeli.
    /// </summary>s
    public class PosSelectionRequestDto
    {
        /// <summary>
        /// İşlem tutarı. Örn: 100.50
        /// </summary>
        [Required(ErrorMessage = "Tutar alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// İşlemin para birimi. Desteklenenler: TRY, USD, EUR.
        /// </summary>
        [Required(ErrorMessage = "Para birimi zorunludur.")]
        [RegularExpression("TRY|USD|EUR", ErrorMessage = "Sadece TRY, USD veya EUR desteklenmektedir.")]
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Taksit sayısı. Tek çekim için 1 gönderilmelidir. (Max: 12)
        /// </summary>
        [Range(1, 12, ErrorMessage = "Taksit sayısı 1 ile 12 arasında olmalıdır.")]
        [JsonPropertyName("installment")]
        public int Installment { get; set; } = 1;

        /// <summary>
        /// Kart tipi. Örn: "Credit" veya "Debit".
        /// </summary>
        [Required(ErrorMessage = "Kart tipi zorunludur (Credit/Debit).")]
        [JsonPropertyName("card_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CardType { get; set; } = "Credit";

        /// <summary>
        /// (Opsiyonel) Kart markası/programı. Örn: "Bonus", "World", "Axess".
        /// Belirtilmezse tüm kart markaları değerlendirilir.
        /// </summary>
        [JsonPropertyName("card_brand")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CardBrand { get; set; }
    }
}
