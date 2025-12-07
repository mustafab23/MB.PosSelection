using MB.PosSelection.Domain.Common;

namespace MB.PosSelection.Domain.Entities
{
    public class PosRatioHistory : BaseEntity
    {
        public Guid OriginalPosRatioId { get; private set; }
        public DateTime ArchivedAt { get; private set; }
        public string BatchId { get; private set; }
        public string PosName { get; private set; }
        public string CardType { get; private set; }
        public string CardBrand { get; private set; }
        public int Installment { get; private set; }
        public string Currency { get; private set; }
        public decimal CommissionRate { get; private set; }
        public decimal MinFee { get; private set; }
        public int Priority { get; private set; }

        protected PosRatioHistory() { }
    }
}
