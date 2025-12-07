using MB.PosSelection.Domain.Common;
using MB.PosSelection.Domain.Enums;

namespace MB.PosSelection.Domain.Entities
{
    public class PosRatio : BaseEntity
    {
        public string BatchId { get; private set; }
        public string PosName { get; private set; }
        public CardType CardType { get; private set; }
        public string CardBrand { get; private set; }
        public int Installment { get; private set; }
        public Currency Currency { get; private set; }
        public decimal CommissionRate { get; private set; }
        public decimal MinFee { get; private set; }
        public int Priority { get; private set; }

        protected PosRatio() { }

        public PosRatio(
            string batchId,
            string posName,
            CardType cardType,
            string cardBrand,
            int installment,
            Currency currency,
            decimal commissionRate,
            decimal minFee,
            int priority)
        {
            // Temel Domain Validasyonları
            if (string.IsNullOrWhiteSpace(batchId)) throw new ArgumentNullException(nameof(batchId));
            if (string.IsNullOrWhiteSpace(posName)) throw new ArgumentNullException(nameof(posName));
            if (commissionRate < 0) throw new ArgumentException("Komisyon oranı 0'dan küçük olamaz.");
            if (installment < 1) throw new ArgumentException("Taksit sayısı en az 1 olmalıdır.");

            BatchId = batchId;
            PosName = posName;
            CardType = cardType;
            CardBrand = cardBrand.ToLowerInvariant();
            Installment = installment;
            Currency = currency;
            CommissionRate = commissionRate;
            MinFee = minFee;
            Priority = priority;
        }

        public void UpdateRate(decimal newRate, decimal newMinFee)
        {
            if (newRate < 0) throw new ArgumentException("Oran negatif olamaz.");
            CommissionRate = newRate;
            MinFee = newMinFee;
            LastModifiedAt = DateTime.UtcNow;
        }
    }
}
