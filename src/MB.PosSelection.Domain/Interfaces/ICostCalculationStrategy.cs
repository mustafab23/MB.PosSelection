using MB.PosSelection.Domain.Enums;

namespace MB.PosSelection.Domain.Interfaces
{
    public interface ICostCalculationStrategy
    {
        /// <summary>
        /// Bu stratejinin hangi para birimi için geçerli olduğunu belirtir.
        /// </summary>
        Currency SupportedCurrency { get; }

        /// <summary>
        /// Verilen tutar ve oranlara göre maliyeti hesaplar.
        /// </summary>
        /// <param name="amount">İşlem tutarı</param>
        /// <param name="commissionRate">Komisyon oranı</param>
        /// <param name="minFee">Minimum ücret</param>
        /// <returns>Hesaplanan maliyet</returns>
        decimal CalculateCost(decimal amount, decimal commissionRate, decimal minFee);
    }
}
