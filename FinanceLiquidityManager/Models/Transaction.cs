using System;
using System.Collections.Generic;

namespace FinanceLiquidityManager.Models
{
    public partial class Transaction
    {
        public string TransactionId { get; set; } = null!;
        public string AccountId { get; set; } = null!;
        public string? CreditDebitIndicator { get; set; }
        public string? Status { get; set; }
        public DateTime BookingDateTime { get; set; }
        public DateTime? ValueDateTime { get; set; }
        public decimal Amount { get; set; }
        public string AmountCurrency { get; set; } = null!;
        public string TransactionCode { get; set; } = null!;
        public string? TransactionIssuer { get; set; }
        public string? TransactionInformation { get; set; }
        public string? MerchantName { get; set; }
        public decimal? ExchangeRate { get; set; }
        public string? SourceCurrency { get; set; }
        public string? TargetCurrency { get; set; }
        public string? UnitCurrency { get; set; }
        public decimal? InstructedAmount { get; set; }
        public string? InstructedCurrency { get; set; }
        public string? BalanceCreditDebitIndicator { get; set; }
        public decimal BalanceAmount { get; set; }
        public string BalanceCurrency { get; set; } = null!;
        public decimal? ChargeAmount { get; set; }
        public string ChargeCurrency { get; set; } = null!;
        public string? SupplementaryData { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
