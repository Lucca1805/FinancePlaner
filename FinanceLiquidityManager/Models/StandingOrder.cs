using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class StandingOrder
    {
        public int OrderId { get; set; }
        public string CreditorAccountId { get; set; } = null!;
        public string Frequency { get; set; } = null!;
        public int? NumberOfPayments { get; set; }
        public DateTime FirstPaymentDateTime { get; set; }
        public DateTime? FinalPaymentDateTime { get; set; }
        public string Reference { get; set; } = null!;

        public virtual Account CreditorAccount { get; set; } = null!;
    }
}
